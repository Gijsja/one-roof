using System;
using System.Collections.Generic;
using OneRoof.Application.Tower;
using OneRoof.Domain.Time;
using OneRoof.Domain.Topology;
using OneRoof.Domain.Weather;
using UnityEngine;
using UnityEngine.Rendering;
using UnityApplication = UnityEngine.Application;

namespace OneRoof.Presentation.Tower
{
    using WeatherCondition = OneRoof.Domain.Weather.WeatherCondition;


    /// <summary>
    /// Exterior roof eave / gutter point where rainwater collects and drips into the outside space.
    /// </summary>
    public readonly struct RoofEave : IEquatable<RoofEave>
    {
        public readonly Vector3 WorldPosition;
        public readonly int Floor;
        public readonly bool IsWestSide;
        public readonly float Overhang;

        public RoofEave(Vector3 worldPosition, int floor, bool isWestSide, float overhang = 0.20f)
        {
            WorldPosition = worldPosition;
            Floor = floor;
            IsWestSide = isWestSide;
            Overhang = overhang;
        }

        public bool Equals(RoofEave other) =>
            WorldPosition.Equals(other.WorldPosition) && Floor == other.Floor && IsWestSide == other.IsWestSide;

        public override bool Equals(object obj) => obj is RoofEave other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(WorldPosition, Floor, IsWestSide);
    }

    /// <summary>
    /// Presentation component managing retro pixel rain, mathematical exterior envelope occlusion,
    /// and roof water runoff / dripping VFX for One Roof's outside world stage.
    /// Operates with 0 steady-state GC allocations, 1 draw call, and strict headless Unity safety.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PixelRainPresenter : MonoBehaviour
    {
        private const int MaxRainDrops = 384;
        private const int MaxRoofDrips = 64;
        private const int MaxSplashes = 64;
        private const int MaxSnowFlakes = 128;
        private const int MaxFogPuffs = 32;
        private const int TotalQuads = MaxRainDrops + MaxRoofDrips + MaxSplashes + MaxSnowFlakes + MaxFogPuffs;
        private const int TotalVertices = TotalQuads * 4;
        private const int TotalIndices = TotalQuads * 6;

        private struct RainDrop
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Length;
            public float Width;
            public bool Active;
        }

        private struct RoofDrip
        {
            public Vector2 Position;
            public float VelocityY;
            public float ImpactY;
            public float Length;
            public bool Active;
        }

        private struct SplashParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Life;
            public float MaxLife;
            public float Size;
            public bool Active;
        }

        private struct SnowFlake
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float SwayPhase;
            public bool Active;
        }

        private struct FogPuff
        {
            public Vector2 Position;
            public float Opacity;
            public float Size;
            public float DriftX;
            public bool Active;
        }

        private readonly RainDrop[] _rainDrops = new RainDrop[MaxRainDrops];
        private readonly RoofDrip[] _roofDrips = new RoofDrip[MaxRoofDrips];
        private readonly SplashParticle[] _splashes = new SplashParticle[MaxSplashes];
        private readonly SnowFlake[] _snowFlakes = new SnowFlake[MaxSnowFlakes];
        private readonly FogPuff[] _fogPuffs = new FogPuff[MaxFogPuffs];
        private readonly List<RoofEave> _roofEaves = new List<RoofEave>();
        private readonly float[] _eaveDripTimers = new float[MaxRoofDrips];

        private Vector2[] _floorBounds = Array.Empty<Vector2>();
        private int _floorCount;

        // Preallocated procedural mesh buffers (0 GC in steady state)
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private Color32[] _colors;
        private int[] _triangles;

        private Mesh _proceduralMesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _rainMaterial;

        private Camera _camera;
        private AudioSource _rainSource;
        private AudioSource _dripSource;
        private AudioClip _rainAmbientClip;
        private AudioClip _dripFoleyClip;

        private WeatherCondition _condition = WeatherCondition.Clear;
        private float _rainIntensity = 0f;
        private float _targetIntensity = 0f;
        private float _windSpeed = -0.6f;
        private float _nightFactor = 0f;
        private uint _rngState = 123456789;

        public WeatherCondition Condition => _condition;
        public float TargetIntensity => _targetIntensity;
        public float RainIntensity => _rainIntensity;
        public float WindSpeed
        {
            get => _windSpeed;
            set => _windSpeed = value;
        }
        public int ActiveDropCount { get; private set; }
        public int ActiveDripCount { get; private set; }
        public int ActiveSplashCount { get; private set; }
        public int ActiveSnowFlakeCount { get; private set; }
        public int ActiveFogPuffCount { get; private set; }
        public int RoofEaveCount => _roofEaves.Count;
        public IReadOnlyList<RoofEave> RoofEaves => _roofEaves;
        public AudioSource RainAudioSource => _rainSource;
        public AudioSource DripAudioSource => _dripSource;
        public MeshRenderer RainMeshRenderer => _meshRenderer;

        public void Initialize(Camera camera, Material material = null)
        {
            _camera = camera;
            EnsureMeshBuffers();
            EnsureRenderers(material);
            EnsureAudio();
        }

        public void Initialize(Material material)
        {
            Initialize(Camera.main, material);
        }

        public void SyncTopology(TowerTopologyProjection topology) => SyncEnvelope(topology);

        public void SyncEnvelope(TowerTopologyProjection topology)
        {
            _floorCount = topology != null ? Mathf.Max(1, topology.FloorCount) : TowerStructurePresenter.InitialFloorCount;
            if (_floorBounds.Length < _floorCount)
            {
                _floorBounds = new Vector2[Mathf.Max(32, _floorCount)];
            }

            for (var f = 0; f < _floorCount; f++)
            {
                float minX = -14f, maxX = 16f;
                if (topology != null && topology.TryGetFloorSlab(f, out var slab))
                {
                    minX = slab.MinX;
                    maxX = slab.MaxX;
                }
                var worldLeft = -2.4f + minX * 0.5f;
                var worldRight = -2.4f + (maxX + 1) * 0.5f;
                _floorBounds[f] = new Vector2(worldLeft, worldRight);
            }

            RebuildRoofEaves();
        }

        public void SetWeather(WeatherSample sample) => SetWeather(sample.Condition, sample.Intensity, sample.WindSpeed);

        public void SetWeather(WeatherCondition condition, float intensity = -1f, float windSpeed = float.NaN)
        {
            _condition = condition;
            _targetIntensity = intensity >= 0f ? Mathf.Clamp01(intensity) : condition switch
            {
                WeatherCondition.Clear => 0f,
                WeatherCondition.Drizzle => 0.25f,
                WeatherCondition.Rain => 0.65f,
                WeatherCondition.Storm => 1.0f,
                WeatherCondition.Fog => 0.40f,
                WeatherCondition.Snow => 0.50f,
                _ => 0f
            };

            if (!float.IsNaN(windSpeed))
            {
                _windSpeed = windSpeed;
            }
            else if (_condition == WeatherCondition.Storm)
            {
                _windSpeed = -2.2f;
            }
            else if (_condition == WeatherCondition.Drizzle)
            {
                _windSpeed = -0.3f;
            }
            else if (_condition == WeatherCondition.Rain)
            {
                _windSpeed = -0.9f;
            }
            else if (_condition == WeatherCondition.Snow)
            {
                _windSpeed = (RandomValue() - 0.5f) * 0.3f; // gentle random snow drift
            }
            else if (_condition == WeatherCondition.Fog)
            {
                _windSpeed = 0f;
            }
        }

        public void UpdateWeather(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            // Smoothly ramp rain intensity
            _rainIntensity = Mathf.MoveTowards(_rainIntensity, _targetIntensity, deltaTime * 0.5f);

            UpdateAudioVolume(deltaTime);

            if (_rainIntensity <= 0.001f && ActiveDropCount == 0 && ActiveDripCount == 0 && ActiveSplashCount == 0
                && ActiveSnowFlakeCount == 0 && ActiveFogPuffCount == 0)
            {
                if (_meshRenderer != null && _meshRenderer.enabled) _meshRenderer.enabled = false;
                return;
            }

            if (_meshRenderer != null && !_meshRenderer.enabled) _meshRenderer.enabled = true;

            UpdateRainDrops(deltaTime);
            UpdateRoofDrips(deltaTime);
            UpdateSplashes(deltaTime);
            if (_condition == WeatherCondition.Snow) UpdateSnowFlakes(deltaTime);
            else { DeactivateSnowFlakes(); }
            if (_condition == WeatherCondition.Fog) UpdateFogPuffs(deltaTime);
            else { DeactivateFogPuffs(); }
            RebuildMeshGeometry();
        }

        public void UpdateDayNight(DayPhase phase)
        {
            var hour = phase.Hour + phase.Minute / 60f;
            _nightFactor = hour < 6f ? Mathf.Clamp01((7f - hour) / 2f) :
                hour >= 18f ? Mathf.Clamp01((hour - 18f) / 2f) : 0f;
        }

        public void UpdateLighting(DayPhase phase) => UpdateDayNight(phase);

        /// <summary>
        /// Exact O(1) mathematical check: is the given 2D world position inside any cutaway interior room?
        /// </summary>
        public bool IsInsideBuilding(float x, float y)
        {
            if (_floorCount <= 0) return false;
            var groundY = TowerStructurePresenter.FloorY(0) - 0.74f;
            if (y < groundY) return false;

            var floor = Mathf.FloorToInt((y - groundY) / TowerStructurePresenter.DefaultFloorHeight);
            if (floor < 0 || floor >= _floorCount) return false;

            var bounds = _floorBounds[floor];
            return x >= bounds.x && x <= bounds.y;
        }

        /// <summary>
        /// Returns the exterior roof elevation at coordinate x.
        /// </summary>
        public float GetRoofY(float x)
        {
            var groundY = TowerStructurePresenter.FloorY(0) - 0.70f;
            if (_floorCount <= 0) return groundY;

            for (var f = _floorCount - 1; f >= 0; f--)
            {
                var b = _floorBounds[f];
                if (x >= b.x && x <= b.y)
                {
                    return TowerStructurePresenter.FloorY(f) + 0.88f;
                }
            }
            return groundY;
        }

        public void Clear()
        {
            for (var i = 0; i < MaxRainDrops; i++) _rainDrops[i].Active = false;
            for (var i = 0; i < MaxRoofDrips; i++) _roofDrips[i].Active = false;
            for (var i = 0; i < MaxSplashes; i++) _splashes[i].Active = false;
            for (var i = 0; i < MaxSnowFlakes; i++) _snowFlakes[i].Active = false;
            for (var i = 0; i < MaxFogPuffs; i++) _fogPuffs[i].Active = false;
            ActiveDropCount = 0;
            ActiveDripCount = 0;
            ActiveSplashCount = 0;
            ActiveSnowFlakeCount = 0;
            ActiveFogPuffCount = 0;
            _roofEaves.Clear();

            if (_proceduralMesh != null)
            {
                _proceduralMesh.Clear();
                DestroyUnityObject(_proceduralMesh);
                _proceduralMesh = null;
            }

            if (_meshRenderer != null) DestroyUnityObject(_meshRenderer.gameObject);
            _meshRenderer = null;
            _meshFilter = null;

            if (_rainMaterial != null) DestroyUnityObject(_rainMaterial);
            _rainMaterial = null;

            if (_rainSource != null) DestroyUnityObject(_rainSource.gameObject);
            _rainSource = null;
            if (_dripSource != null) DestroyUnityObject(_dripSource.gameObject);
            _dripSource = null;

            if (_rainAmbientClip != null) DestroyUnityObject(_rainAmbientClip);
            _rainAmbientClip = null;
            if (_dripFoleyClip != null) DestroyUnityObject(_dripFoleyClip);
            _dripFoleyClip = null;
        }

        private void OnDestroy() => Clear();
        private void Update() => UpdateWeather(UnityApplication.isPlaying ? Time.deltaTime : 0.016f);

        private void RebuildRoofEaves()
        {
            _roofEaves.Clear();
            if (_floorCount <= 0) return;

            var topFloor = _floorCount - 1;
            var topBounds = _floorBounds[topFloor];
            var topRoofY = TowerStructurePresenter.FloorY(topFloor) + 0.88f;

            // 1. Primary Top Roof Overhangs (West and East)
            _roofEaves.Add(new RoofEave(new Vector3(topBounds.x - 0.20f, topRoofY, -0.45f), topFloor, true, 0.20f));
            _roofEaves.Add(new RoofEave(new Vector3(topBounds.y + 0.20f, topRoofY, -0.45f), topFloor, false, 0.20f));

            // Intermediate gutters if the roof span is wide
            var topWidth = topBounds.y - topBounds.x;
            if (topWidth > 6.0f)
            {
                var midLeft = topBounds.x + topWidth * 0.33f;
                var midRight = topBounds.x + topWidth * 0.67f;
                _roofEaves.Add(new RoofEave(new Vector3(midLeft, topRoofY, -0.45f), topFloor, true, 0f));
                _roofEaves.Add(new RoofEave(new Vector3(midRight, topRoofY, -0.45f), topFloor, false, 0f));
            }

            // 2. Setback / Terrace Eaves (floors wider than the floor above)
            for (var f = 0; f < topFloor; f++)
            {
                var current = _floorBounds[f];
                var above = _floorBounds[f + 1];
                var terraceY = TowerStructurePresenter.FloorY(f) + 0.88f;

                if (current.x < above.x - 0.2f)
                {
                    // West setback step
                    _roofEaves.Add(new RoofEave(new Vector3(current.x - 0.15f, terraceY, -0.45f), f, true, 0.15f));
                }

                if (current.y > above.y + 0.2f)
                {
                    // East setback step
                    _roofEaves.Add(new RoofEave(new Vector3(current.y + 0.15f, terraceY, -0.45f), f, false, 0.15f));
                }
            }
        }

        private void UpdateRainDrops(float deltaTime)
        {
            // Snow and Fog do not spawn rain drops
            if (_condition == WeatherCondition.Snow || _condition == WeatherCondition.Fog)
            {
                for (var i = 0; i < MaxRainDrops; i++) _rainDrops[i].Active = false;
                ActiveDropCount = 0;
                return;
            }

            var targetDrops = Mathf.RoundToInt(_rainIntensity * MaxRainDrops);
            var streetY = TowerStructurePresenter.FloorY(0) - 0.70f;
            var camPos = _camera != null ? _camera.transform.position : new Vector3(0f, 3.5f, -10f);
            var ortho = _camera != null ? _camera.orthographicSize : 6.8f;
            var aspect = _camera != null ? _camera.aspect : 1.777f;
            var viewLeft = camPos.x - ortho * aspect - 2f;
            var viewRight = camPos.x + ortho * aspect + 2f;
            var spawnY = camPos.y + ortho + 1.5f;
            var active = 0;

            for (var i = 0; i < MaxRainDrops; i++)
            {
                ref var drop = ref _rainDrops[i];

                if (!drop.Active)
                {
                    if (active < targetDrops && RandomValue() < 0.25f)
                    {
                        // Spawn in Outside Stage
                        var spawnX = Mathf.Lerp(viewLeft, viewRight, RandomValue());
                        drop.Position = new Vector2(spawnX, spawnY + RandomValue() * 3f);
                        var speed = Mathf.Lerp(11.5f, 15.5f, RandomValue());
                        drop.Velocity = new Vector2(_windSpeed * speed * 0.12f, -speed);
                        drop.Length = Mathf.Lerp(0.20f, 0.32f, RandomValue());
                        drop.Width = 0.035f;
                        drop.Active = true;
                        active++;
                    }
                    continue;
                }

                // Advance
                drop.Position += drop.Velocity * deltaTime;

                var dropX = drop.Position.x;
                var dropY = drop.Position.y;

                // 1. Street impact (East outside)
                if (dropY <= streetY)
                {
                    drop.Active = false;
                    SpawnSplash(new Vector2(dropX, streetY), 0.045f);
                    continue;
                }

                // 2. Interior building occlusion: if drop touches roof or enters building envelope, splash and terminate!
                if (IsInsideBuilding(dropX, dropY))
                {
                    drop.Active = false;
                    var roofY = GetRoofY(dropX);
                    SpawnSplash(new Vector2(dropX, roofY), 0.035f);
                    continue;
                }

                // 3. Below camera view culling
                if (dropY < camPos.y - ortho - 1.5f)
                {
                    drop.Active = false;
                    continue;
                }

                active++;
            }

            ActiveDropCount = active;
        }

        private void UpdateRoofDrips(float deltaTime)
        {
            if (_condition == WeatherCondition.Snow || _condition == WeatherCondition.Fog)
            {
                ActiveDripCount = 0;
                return;
            }

            var eaveCount = _roofEaves.Count;
            if (eaveCount == 0 || _rainIntensity <= 0.05f)
            {
                ActiveDripCount = 0;
                return;
            }

            var streetY = TowerStructurePresenter.FloorY(0) - 0.70f;
            var dripInterval = Mathf.Lerp(0.65f, 0.12f, _rainIntensity);
            var active = 0;

            // 1. Emitter update
            for (var e = 0; e < eaveCount && e < MaxRoofDrips; e++)
            {
                _eaveDripTimers[e] -= deltaTime;
                if (_eaveDripTimers[e] <= 0f)
                {
                    _eaveDripTimers[e] = dripInterval * (0.8f + RandomValue() * 0.4f);
                    SpawnRoofDrip(_roofEaves[e], streetY);
                }
            }

            // 2. Particle simulation
            for (var i = 0; i < MaxRoofDrips; i++)
            {
                ref var drip = ref _roofDrips[i];
                if (!drip.Active) continue;

                drip.VelocityY -= 14.0f * deltaTime; // gravity
                drip.Position.y += drip.VelocityY * deltaTime;
                drip.Position.x += _windSpeed * deltaTime * 0.15f; // subtle wind drift
                drip.Length = Mathf.Clamp(Mathf.Abs(drip.VelocityY) * 0.015f, 0.04f, 0.16f);

                if (drip.Position.y <= drip.ImpactY)
                {
                    drip.Active = false;
                    SpawnSplash(new Vector2(drip.Position.x, drip.ImpactY), 0.04f);
                    PlayDripFoley(drip.Position);
                    continue;
                }

                // If drift blew drip into the interior wall, splash on wall and terminate
                if (IsInsideBuilding(drip.Position.x, drip.Position.y))
                {
                    drip.Active = false;
                    SpawnSplash(new Vector2(drip.Position.x, drip.Position.y), 0.03f);
                    continue;
                }

                active++;
            }

            ActiveDripCount = active;
        }

        private void SpawnRoofDrip(RoofEave eave, float streetY)
        {
            for (var i = 0; i < MaxRoofDrips; i++)
            {
                if (_roofDrips[i].Active) continue;

                ref var drip = ref _roofDrips[i];
                drip.Position = new Vector2(eave.WorldPosition.x, eave.WorldPosition.y - 0.02f);
                drip.VelocityY = -0.5f;
                drip.Length = 0.04f;

                // Calculate impact floor level or street below this eave
                var impactY = streetY;
                if (eave.Floor > 0)
                {
                    for (var f = eave.Floor - 1; f >= 0; f--)
                    {
                        var b = _floorBounds[f];
                        if (drip.Position.x >= b.x && drip.Position.x <= b.y)
                        {
                            impactY = TowerStructurePresenter.FloorY(f) + 0.88f;
                            break;
                        }
                    }
                }

                drip.ImpactY = impactY;
                drip.Active = true;
                break;
            }
        }

        private void UpdateSplashes(float deltaTime)
        {
            var active = 0;
            for (var i = 0; i < MaxSplashes; i++)
            {
                ref var splash = ref _splashes[i];
                if (!splash.Active) continue;

                splash.Life += deltaTime;
                if (splash.Life >= splash.MaxLife)
                {
                    splash.Active = false;
                    continue;
                }

                splash.Position += splash.Velocity * deltaTime;
                splash.Velocity.y -= 9.8f * deltaTime;
                active++;
            }
            ActiveSplashCount = active;
        }

        private void SpawnSplash(Vector2 position, float size)
        {
            for (var i = 0; i < MaxSplashes; i++)
            {
                if (_splashes[i].Active) continue;

                ref var splash = ref _splashes[i];
                splash.Position = position + new Vector2((RandomValue() - 0.5f) * 0.05f, 0.01f);
                var angle = Mathf.Lerp(30f, 150f, RandomValue()) * Mathf.Deg2Rad;
                var speed = Mathf.Lerp(1.2f, 2.5f, RandomValue());
                splash.Velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);
                splash.Life = 0f;
                splash.MaxLife = Mathf.Lerp(0.08f, 0.14f, RandomValue());
                splash.Size = size;
                splash.Active = true;
                break;
            }
        }

        private void RebuildMeshGeometry()
        {
            if (_proceduralMesh == null) return;

            var vertexIndex = 0;
            var triangleIndex = 0;

            // Palette derivation from Day/Night
            var dayRain = new Color32(180, 220, 245, 175);
            var nightRain = new Color32(85, 115, 155, 130);
            var baseRainColor = Color32.Lerp(dayRain, nightRain, _nightFactor);

            var streetApronX = -2.4f;
            if (_floorCount > 0 && _floorBounds.Length > 0) streetApronX = _floorBounds[0].y;
            var streetY = TowerStructurePresenter.FloorY(0) - 0.70f;

            // 1. Pack Rain Drops
            for (var i = 0; i < MaxRainDrops; i++)
            {
                ref var drop = ref _rainDrops[i];
                if (!drop.Active) continue;

                var dropColor = baseRainColor;
                // Golden street lamp glow near ground on East side
                if (drop.Position.x >= streetApronX && drop.Position.y <= streetY + 1.8f)
                {
                    var streetWarmth = Mathf.Clamp01(1f - (drop.Position.y - streetY) / 1.8f) * (0.4f + 0.6f * _nightFactor);
                    dropColor = Color32.Lerp(dropColor, new Color32(255, 205, 115, 210), streetWarmth);
                }

                var dir = drop.Velocity.normalized;
                var normal = new Vector2(-dir.y, dir.x);
                var halfWidth = drop.Width * 0.5f;

                var p0 = drop.Position - normal * halfWidth;
                var p1 = drop.Position + normal * halfWidth;
                var p2 = drop.Position - dir * drop.Length + normal * halfWidth;
                var p3 = drop.Position - dir * drop.Length - normal * halfWidth;

                const float z = -0.45f;
                _vertices[vertexIndex + 0] = new Vector3(p0.x, p0.y, z);
                _vertices[vertexIndex + 1] = new Vector3(p1.x, p1.y, z);
                _vertices[vertexIndex + 2] = new Vector3(p2.x, p2.y, z);
                _vertices[vertexIndex + 3] = new Vector3(p3.x, p3.y, z);

                _colors[vertexIndex + 0] = dropColor;
                _colors[vertexIndex + 1] = dropColor;
                _colors[vertexIndex + 2] = new Color32(dropColor.r, dropColor.g, dropColor.b, (byte)(dropColor.a * 0.25f));
                _colors[vertexIndex + 3] = new Color32(dropColor.r, dropColor.g, dropColor.b, (byte)(dropColor.a * 0.25f));

                _triangles[triangleIndex + 0] = vertexIndex + 0;
                _triangles[triangleIndex + 1] = vertexIndex + 2;
                _triangles[triangleIndex + 2] = vertexIndex + 1;
                _triangles[triangleIndex + 3] = vertexIndex + 0;
                _triangles[triangleIndex + 4] = vertexIndex + 3;
                _triangles[triangleIndex + 5] = vertexIndex + 2;

                vertexIndex += 4;
                triangleIndex += 6;
            }

            // 2. Pack Roof Drips
            var dripColor = Color32.Lerp(new Color32(210, 240, 255, 230), new Color32(110, 150, 200, 190), _nightFactor);
            for (var i = 0; i < MaxRoofDrips; i++)
            {
                ref var drip = ref _roofDrips[i];
                if (!drip.Active) continue;

                var w = 0.04f;
                var p0 = new Vector3(drip.Position.x - w * 0.5f, drip.Position.y, -0.45f);
                var p1 = new Vector3(drip.Position.x + w * 0.5f, drip.Position.y, -0.45f);
                var p2 = new Vector3(drip.Position.x + w * 0.5f, drip.Position.y + drip.Length, -0.45f);
                var p3 = new Vector3(drip.Position.x - w * 0.5f, drip.Position.y + drip.Length, -0.45f);

                _vertices[vertexIndex + 0] = p0;
                _vertices[vertexIndex + 1] = p1;
                _vertices[vertexIndex + 2] = p2;
                _vertices[vertexIndex + 3] = p3;

                _colors[vertexIndex + 0] = dripColor;
                _colors[vertexIndex + 1] = dripColor;
                _colors[vertexIndex + 2] = new Color32(dripColor.r, dripColor.g, dripColor.b, (byte)(dripColor.a * 0.4f));
                _colors[vertexIndex + 3] = new Color32(dripColor.r, dripColor.g, dripColor.b, (byte)(dripColor.a * 0.4f));

                _triangles[triangleIndex + 0] = vertexIndex + 0;
                _triangles[triangleIndex + 1] = vertexIndex + 2;
                _triangles[triangleIndex + 2] = vertexIndex + 1;
                _triangles[triangleIndex + 3] = vertexIndex + 0;
                _triangles[triangleIndex + 4] = vertexIndex + 3;
                _triangles[triangleIndex + 5] = vertexIndex + 2;

                vertexIndex += 4;
                triangleIndex += 6;
            }

            // 3. Pack Splashes
            for (var i = 0; i < MaxSplashes; i++)
            {
                ref var splash = ref _splashes[i];
                if (!splash.Active) continue;

                var lifeProgress = splash.Life / splash.MaxLife;
                var alpha = (byte)(Mathf.Clamp01(1f - lifeProgress) * 220);
                var splashColor = new Color32(baseRainColor.r, baseRainColor.g, baseRainColor.b, alpha);

                var s = splash.Size * (0.8f + 0.4f * lifeProgress);
                var p0 = new Vector3(splash.Position.x - s * 0.5f, splash.Position.y - s * 0.5f, -0.45f);
                var p1 = new Vector3(splash.Position.x + s * 0.5f, splash.Position.y - s * 0.5f, -0.45f);
                var p2 = new Vector3(splash.Position.x + s * 0.5f, splash.Position.y + s * 0.5f, -0.45f);
                var p3 = new Vector3(splash.Position.x - s * 0.5f, splash.Position.y + s * 0.5f, -0.45f);

                _vertices[vertexIndex + 0] = p0;
                _vertices[vertexIndex + 1] = p1;
                _vertices[vertexIndex + 2] = p2;
                _vertices[vertexIndex + 3] = p3;

                _colors[vertexIndex + 0] = splashColor;
                _colors[vertexIndex + 1] = splashColor;
                _colors[vertexIndex + 2] = splashColor;
                _colors[vertexIndex + 3] = splashColor;

                _triangles[triangleIndex + 0] = vertexIndex + 0;
                _triangles[triangleIndex + 1] = vertexIndex + 2;
                _triangles[triangleIndex + 2] = vertexIndex + 1;
                _triangles[triangleIndex + 3] = vertexIndex + 0;
                _triangles[triangleIndex + 4] = vertexIndex + 3;
                _triangles[triangleIndex + 5] = vertexIndex + 2;

                vertexIndex += 4;
                triangleIndex += 6;
            }

            // 4. Pack Snow Flakes
            var snowColor = Color32.Lerp(new Color32(230, 240, 255, 210), new Color32(180, 200, 230, 150), _nightFactor);
            for (var i = 0; i < MaxSnowFlakes; i++)
            {
                ref var flake = ref _snowFlakes[i];
                if (!flake.Active) continue;

                var s = flake.Size;
                var p0 = new Vector3(flake.Position.x - s * 0.5f, flake.Position.y - s * 0.5f, -0.45f);
                var p1 = new Vector3(flake.Position.x + s * 0.5f, flake.Position.y - s * 0.5f, -0.45f);
                var p2 = new Vector3(flake.Position.x + s * 0.5f, flake.Position.y + s * 0.5f, -0.45f);
                var p3 = new Vector3(flake.Position.x - s * 0.5f, flake.Position.y + s * 0.5f, -0.45f);

                _vertices[vertexIndex + 0] = p0;
                _vertices[vertexIndex + 1] = p1;
                _vertices[vertexIndex + 2] = p2;
                _vertices[vertexIndex + 3] = p3;
                _colors[vertexIndex + 0] = snowColor;
                _colors[vertexIndex + 1] = snowColor;
                _colors[vertexIndex + 2] = snowColor;
                _colors[vertexIndex + 3] = snowColor;

                _triangles[triangleIndex + 0] = vertexIndex + 0;
                _triangles[triangleIndex + 1] = vertexIndex + 2;
                _triangles[triangleIndex + 2] = vertexIndex + 1;
                _triangles[triangleIndex + 3] = vertexIndex + 0;
                _triangles[triangleIndex + 4] = vertexIndex + 3;
                _triangles[triangleIndex + 5] = vertexIndex + 2;

                vertexIndex += 4;
                triangleIndex += 6;
            }

            // 5. Pack Fog Puffs
            for (var i = 0; i < MaxFogPuffs; i++)
            {
                ref var puff = ref _fogPuffs[i];
                if (!puff.Active) continue;

                var alpha = (byte)(puff.Opacity * 255f);
                var fogColor = new Color32(210, 220, 230, alpha);
                var s = puff.Size;
                var p0 = new Vector3(puff.Position.x - s, puff.Position.y - s * 0.4f, -0.44f);
                var p1 = new Vector3(puff.Position.x + s, puff.Position.y - s * 0.4f, -0.44f);
                var p2 = new Vector3(puff.Position.x + s, puff.Position.y + s * 0.4f, -0.44f);
                var p3 = new Vector3(puff.Position.x - s, puff.Position.y + s * 0.4f, -0.44f);

                _vertices[vertexIndex + 0] = p0;
                _vertices[vertexIndex + 1] = p1;
                _vertices[vertexIndex + 2] = p2;
                _vertices[vertexIndex + 3] = p3;
                _colors[vertexIndex + 0] = fogColor;
                _colors[vertexIndex + 1] = fogColor;
                _colors[vertexIndex + 2] = new Color32(fogColor.r, fogColor.g, fogColor.b, (byte)(alpha * 0.5f));
                _colors[vertexIndex + 3] = new Color32(fogColor.r, fogColor.g, fogColor.b, (byte)(alpha * 0.5f));

                _triangles[triangleIndex + 0] = vertexIndex + 0;
                _triangles[triangleIndex + 1] = vertexIndex + 2;
                _triangles[triangleIndex + 2] = vertexIndex + 1;
                _triangles[triangleIndex + 3] = vertexIndex + 0;
                _triangles[triangleIndex + 4] = vertexIndex + 3;
                _triangles[triangleIndex + 5] = vertexIndex + 2;

                vertexIndex += 4;
                triangleIndex += 6;
            }

            // Zero remaining vertices to prevent ghost quads
            for (var v = vertexIndex; v < TotalVertices; v++)
            {
                _vertices[v] = Vector3.zero;
                _colors[v] = Color.clear;
            }
            // Degenerate unused triangle slots (all 3 indices to same vertex = zero-area, not rendered)
            var degenerateVertex = vertexIndex > 0 ? vertexIndex - 1 : 0;
            for (var t = triangleIndex; t < TotalIndices; t++)
            {
                _triangles[t] = degenerateVertex;
            }

            _proceduralMesh.vertices = _vertices;
            _proceduralMesh.colors32 = _colors;
            _proceduralMesh.triangles = _triangles;
            if (vertexIndex > 0) _proceduralMesh.RecalculateBounds();
        }

        private void EnsureMeshBuffers()
        {
            if (_vertices != null) return;

            _vertices = new Vector3[TotalVertices];
            _uvs = new Vector2[TotalVertices];
            _colors = new Color32[TotalVertices];
            _triangles = new int[TotalIndices];

            for (var i = 0; i < TotalQuads; i++)
            {
                var v = i * 4;
                _uvs[v + 0] = new Vector2(0f, 0f);
                _uvs[v + 1] = new Vector2(1f, 0f);
                _uvs[v + 2] = new Vector2(1f, 1f);
                _uvs[v + 3] = new Vector2(0f, 1f);
            }
        }

        private void EnsureRenderers(Material authoredMaterial)
        {
            if (_meshRenderer != null) return;

            var go = new GameObject("Pixel Rain Mesh Buffer");
            go.transform.SetParent(transform, false);

            _meshFilter = go.AddComponent<MeshFilter>();
            _meshRenderer = go.AddComponent<MeshRenderer>();

            _proceduralMesh = new Mesh { name = "Pixel Rain Procedural Mesh" };
            _proceduralMesh.MarkDynamic();
            _proceduralMesh.vertices = _vertices;
            _proceduralMesh.uv = _uvs;
            _proceduralMesh.colors32 = _colors;
            _proceduralMesh.triangles = _triangles;
            _meshFilter.sharedMesh = _proceduralMesh;

            if (authoredMaterial != null)
            {
                _rainMaterial = authoredMaterial;
            }
            else
            {
                var shader = Shader.Find("OneRoof/Unlit")
                    ?? Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                    ?? Shader.Find("Sprites/Default");

                if (shader != null)
                {
                    _rainMaterial = new Material(shader) { name = "Pixel Rain Material" };
                    _rainMaterial.SetOverrideTag("RenderType", "Transparent");
                    if (_rainMaterial.HasProperty("_Surface")) _rainMaterial.SetFloat("_Surface", 1f);
                    if (_rainMaterial.HasProperty("_Blend")) _rainMaterial.SetFloat("_Blend", 0f);
                    _rainMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    _rainMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    _rainMaterial.SetInt("_ZWrite", 0);
                    _rainMaterial.renderQueue = (int)RenderQueue.Transparent;
                }
            }

            _meshRenderer.sharedMaterial = _rainMaterial;
            _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
        }

        private void EnsureAudio()
        {
            if (_rainSource != null) return;

            _rainSource = CreateSpatialSource("Rain Ambient Sound", 0f, 25f);
            _rainAmbientClip = CreateRainNoiseClip("rain-ambient-loop", 2.0f);
            _rainSource.clip = _rainAmbientClip;
            _rainSource.loop = true;

            _dripSource = CreateSpatialSource("Roof Drip Foley", 0f, 12f);
            _dripFoleyClip = CreateDripPlinkClip("roof-drip-plink", 1450f, 0.05f);
            _dripSource.clip = _dripFoleyClip;
            _dripSource.loop = false;

            if (UnityApplication.isPlaying && _rainSource != null && !_rainSource.isPlaying)
            {
                _rainSource.Play();
            }
        }

        private void UpdateSnowFlakes(float deltaTime)
        {
            var targetFlakes = Mathf.RoundToInt(_rainIntensity * MaxSnowFlakes);
            var camPos = _camera != null ? _camera.transform.position : new Vector3(0f, 3.5f, -10f);
            var ortho = _camera != null ? _camera.orthographicSize : 6.8f;
            var aspect = _camera != null ? _camera.aspect : 1.777f;
            var viewLeft = camPos.x - ortho * aspect - 1f;
            var viewRight = camPos.x + ortho * aspect + 1f;
            var spawnY = camPos.y + ortho + 0.5f;
            var streetY = TowerStructurePresenter.FloorY(0) - 0.70f;
            var active = 0;
            var time = UnityApplication.isPlaying ? UnityEngine.Time.time : 0f;

            for (var i = 0; i < MaxSnowFlakes; i++)
            {
                ref var flake = ref _snowFlakes[i];
                if (!flake.Active)
                {
                    if (active < targetFlakes && RandomValue() < 0.12f)
                    {
                        flake.Position = new Vector2(
                            Mathf.Lerp(viewLeft, viewRight, RandomValue()),
                            spawnY + RandomValue() * 2f);
                        var speed = Mathf.Lerp(1.2f, 2.4f, RandomValue());
                        flake.Velocity = new Vector2(_windSpeed * 0.25f, -speed);
                        flake.Size = Mathf.Lerp(0.04f, 0.09f, RandomValue());
                        flake.SwayPhase = RandomValue() * Mathf.PI * 2f;
                        flake.Active = true;
                        active++;
                    }
                    continue;
                }

                // Gentle sway
                var sway = Mathf.Sin(time * 1.8f + flake.SwayPhase) * 0.3f;
                flake.Position += (flake.Velocity + new Vector2(sway, 0f)) * deltaTime;

                if (flake.Position.y <= streetY || IsInsideBuilding(flake.Position.x, flake.Position.y))
                {
                    flake.Active = false;
                    continue;
                }
                active++;
            }
            ActiveSnowFlakeCount = active;
        }

        private void DeactivateSnowFlakes()
        {
            for (var i = 0; i < MaxSnowFlakes; i++) _snowFlakes[i].Active = false;
            ActiveSnowFlakeCount = 0;
        }

        private void UpdateFogPuffs(float deltaTime)
        {
            var targetPuffs = Mathf.RoundToInt(_rainIntensity * MaxFogPuffs);
            var camPos = _camera != null ? _camera.transform.position : new Vector3(0f, 3.5f, -10f);
            var ortho = _camera != null ? _camera.orthographicSize : 6.8f;
            var aspect = _camera != null ? _camera.aspect : 1.777f;
            var viewLeft = camPos.x - ortho * aspect;
            var viewRight = camPos.x + ortho * aspect;
            var active = 0;

            for (var i = 0; i < MaxFogPuffs; i++)
            {
                ref var puff = ref _fogPuffs[i];
                if (!puff.Active)
                {
                    if (active < targetPuffs && RandomValue() < 0.05f)
                    {
                        puff.Position = new Vector2(
                            Mathf.Lerp(viewLeft, viewRight, RandomValue()),
                            camPos.y + Mathf.Lerp(-ortho, ortho, RandomValue()));
                        puff.Opacity = Mathf.Lerp(0.05f, 0.15f, RandomValue());
                        puff.Size = Mathf.Lerp(2.5f, 5.5f, RandomValue());
                        puff.DriftX = (_windSpeed * 0.1f) + (RandomValue() - 0.5f) * 0.1f;
                        puff.Active = true;
                        active++;
                    }
                    continue;
                }

                puff.Position.x += puff.DriftX * deltaTime;
                puff.Opacity = Mathf.MoveTowards(puff.Opacity, _rainIntensity * 0.2f, deltaTime * 0.02f);

                if (puff.Position.x < viewLeft - puff.Size || puff.Position.x > viewRight + puff.Size)
                    puff.Active = false;
                else
                    active++;
            }
            ActiveFogPuffCount = active;
        }

        private void DeactivateFogPuffs()
        {
            for (var i = 0; i < MaxFogPuffs; i++) _fogPuffs[i].Active = false;
            ActiveFogPuffCount = 0;
        }

        private void UpdateAudioVolume(float deltaTime)
        {
            if (_rainSource == null) return;
            var targetVolume = _rainIntensity * (_condition == WeatherCondition.Storm ? 0.45f : 0.22f);
            _rainSource.volume = Mathf.MoveTowards(_rainSource.volume, targetVolume, deltaTime * 0.4f);

            if (_roofEaves.Count > 0 && _dripSource != null)
            {
                _dripSource.transform.position = _roofEaves[0].WorldPosition;
            }
        }

        private void PlayDripFoley(Vector2 position)
        {
            if (!UnityApplication.isPlaying || _dripSource == null || _dripFoleyClip == null) return;
            if (RandomValue() < 0.20f) // Subsample foley hits to prevent cacophony
            {
                _dripSource.transform.position = new Vector3(position.x, position.y, -0.45f);
                _dripSource.pitch = Mathf.Lerp(0.9f, 1.15f, RandomValue());
                _dripSource.PlayOneShot(_dripFoleyClip, Mathf.Lerp(0.12f, 0.28f, _rainIntensity));
            }
        }

        private AudioSource CreateSpatialSource(string sourceName, float volume, float maxDistance)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0.5f; // Semi-spatialized
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1.0f;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            source.playOnAwake = false;
            source.volume = volume;
            return source;
        }

        private static AudioClip CreateRainNoiseClip(string clipName, float durationSeconds)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
            var samples = new float[sampleCount];
            var lastSample = 0f;

            // Low-pass filtered noise to simulate gentle rain hiss/patter
            for (var i = 0; i < sampleCount; i++)
            {
                var white = UnityEngine.Random.value * 2f - 1f;
                lastSample = (lastSample * 0.88f) + (white * 0.12f);
                samples[i] = lastSample * 0.18f;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontSave;
            return clip;
        }

        private static AudioClip CreateDripPlinkClip(string clipName, float frequency, float durationSeconds)
        {
            const int sampleRate = 22050;
            var sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
            var samples = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / sampleCount;
                var envelope = Mathf.Clamp01(1f - t) * Mathf.Clamp01(1f - t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * 0.35f;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontSave;
            return clip;
        }

        private float RandomValue()
        {
            _rngState = (_rngState * 1664525u + 1013904223u);
            return (_rngState & 0x00FFFFFF) / 16777215f;
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null) return;
            if (UnityApplication.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
