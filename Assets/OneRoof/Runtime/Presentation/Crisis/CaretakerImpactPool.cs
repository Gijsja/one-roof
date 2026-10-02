using UnityEngine;

namespace OneRoof.Presentation.Crisis
{
    /// <summary>Bounded, reusable procedural impact marks, directional attack streaks, and landing dust puffs.</summary>
    public sealed class CaretakerImpactPool
    {
        private const int PoolSize = 8;

        // Radial impact stars
        private readonly SpriteRenderer[] _marks = new SpriteRenderer[PoolSize];
        private readonly float[] _age = new float[PoolSize];
        private readonly Texture2D _texture;
        private readonly Sprite _sprite;
        private int _next;

        // Directional impact streaks
        private readonly SpriteRenderer[] _streaks = new SpriteRenderer[PoolSize];
        private readonly float[] _streakAge = new float[PoolSize];
        private readonly Texture2D _streakTexture;
        private readonly Sprite _streakSprite;
        private int _nextStreak;

        // Landing dust puffs
        private readonly SpriteRenderer[] _dustPuffs = new SpriteRenderer[PoolSize];
        private readonly float[] _dustAge = new float[PoolSize];
        private readonly Vector3[] _dustOrigins = new Vector3[PoolSize];
        private readonly Texture2D _dustTexture;
        private readonly Sprite _dustSprite;
        private int _nextDust;

        public CaretakerImpactPool(Transform parent, Material material)
        {
            // 1. Procedural 4-point impact star texture
            _texture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                name = "Caretaker_ImpactStar",
                filterMode = FilterMode.Bilinear
            };
            var starPixels = new Color[32 * 32];
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    var d = new Vector2(x - 15.5f, y - 15.5f) / 16;
                    var angle = Mathf.Atan2(d.y, d.x);
                    var edge = .25f + .65f * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4)), 12);
                    starPixels[y * 32 + x] = new Color(1, 1, 1, Mathf.Clamp01((edge - d.magnitude) * 14));
                }
            _texture.SetPixels(starPixels);
            _texture.Apply(false, true);
            _sprite = Sprite.Create(_texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 32);

            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("ImpactStar " + i);
                go.transform.SetParent(parent, false);
                var mark = go.AddComponent<SpriteRenderer>();
                mark.sprite = _sprite;
                mark.sharedMaterial = material;
                mark.sortingOrder = 900;
                mark.enabled = false;
                _marks[i] = mark;
                _age[i] = 1;
            }

            // 2. Procedural directional impact streak texture (elongated tapered slash along X)
            _streakTexture = new Texture2D(48, 16, TextureFormat.RGBA32, false)
            {
                name = "Caretaker_ImpactStreak",
                filterMode = FilterMode.Bilinear
            };
            var streakPixels = new Color[48 * 16];
            for (var y = 0; y < 16; y++)
                for (var x = 0; x < 48; x++)
                {
                    var nx = (x - 23.5f) / 24f; // -1 to 1
                    var ny = (y - 7.5f) / 8f;   // -1 to 1
                    var taper = 1f - Mathf.Abs(nx);
                    var thickness = Mathf.Clamp01(1f - Mathf.Abs(ny) / Mathf.Max(0.01f, taper * 0.9f));
                    var alpha = Mathf.Clamp01(taper * thickness * 2.2f);
                    streakPixels[y * 48 + x] = new Color(1, 1, 1, alpha);
                }
            _streakTexture.SetPixels(streakPixels);
            _streakTexture.Apply(false, true);
            _streakSprite = Sprite.Create(_streakTexture, new Rect(0, 0, 48, 16), new Vector2(0.5f, 0.5f), 32);

            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("ImpactStreak " + i);
                go.transform.SetParent(parent, false);
                var streak = go.AddComponent<SpriteRenderer>();
                streak.sprite = _streakSprite;
                streak.sharedMaterial = material;
                streak.sortingOrder = 902;
                streak.enabled = false;
                _streaks[i] = streak;
                _streakAge[i] = 1;
            }

            // 3. Procedural soft landing dust puff texture
            _dustTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
            {
                name = "Caretaker_LandingDust",
                filterMode = FilterMode.Bilinear
            };
            var dustPixels = new Color[32 * 32];
            for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    var nx = (x - 15.5f) / 16f;
                    var ny = (y - 15.5f) / 16f;
                    var dist = Mathf.Sqrt(nx * nx + ny * ny);
                    var alpha = Mathf.Clamp01(1f - dist);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    dustPixels[y * 32 + x] = new Color(1, 1, 1, alpha);
                }
            _dustTexture.SetPixels(dustPixels);
            _dustTexture.Apply(false, true);
            _dustSprite = Sprite.Create(_dustTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);

            for (var i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("LandingDust " + i);
                go.transform.SetParent(parent, false);
                var dust = go.AddComponent<SpriteRenderer>();
                dust.sprite = _dustSprite;
                dust.sharedMaterial = material;
                dust.sortingOrder = 898;
                dust.enabled = false;
                _dustPuffs[i] = dust;
                _dustAge[i] = 1;
            }
        }

        public void Emit(Vector3 position, bool caretakerHit)
        {
            var i = _next++ % _marks.Length;
            _marks[i].transform.position = position;
            _marks[i].transform.rotation = Quaternion.Euler(0, 0, i * 23);
            _marks[i].color = caretakerHit ? new Color(1, .27f, .2f) : new Color(1, .84f, .35f);
            _marks[i].enabled = true;
            _age[i] = 0;
        }

        public void EmitStreak(Vector3 position, Vector2 attackVector, bool caretakerHit, bool isCritical = false)
        {
            var i = _nextStreak++ % _streaks.Length;
            var angle = Mathf.Atan2(attackVector.y, attackVector.x) * Mathf.Rad2Deg;

            _streaks[i].transform.position = position;
            _streaks[i].transform.rotation = Quaternion.Euler(0, 0, angle);
            _streaks[i].transform.localScale = new Vector3(0.5f, 0.25f, 1f);

            if (isCritical)
                _streaks[i].color = new Color(1f, 0.92f, 0.45f, 1f);
            else if (caretakerHit)
                _streaks[i].color = new Color(1f, 0.32f, 0.22f, 0.95f);
            else
                _streaks[i].color = new Color(1f, 0.85f, 0.38f, 0.95f);

            _streaks[i].enabled = true;
            _streakAge[i] = 0;
        }

        public void EmitLandingDust(Vector3 position)
        {
            var i = _nextDust++ % _dustPuffs.Length;
            _dustOrigins[i] = position;
            _dustPuffs[i].transform.position = position;
            _dustPuffs[i].transform.localScale = new Vector3(0.3f, 0.15f, 1f);
            _dustPuffs[i].color = new Color(0.88f, 0.85f, 0.80f, 0.65f);
            _dustPuffs[i].enabled = true;
            _dustAge[i] = 0;
        }

        public void Tick(float delta)
        {
            // 1. Radial impact stars
            for (var i = 0; i < _marks.Length; i++)
            {
                if (!_marks[i].enabled)
                    continue;
                _age[i] += delta;
                var t = _age[i] / .22f;
                if (t >= 1)
                {
                    _marks[i].enabled = false;
                    continue;
                }

                _marks[i].transform.localScale = Vector3.one * Mathf.Lerp(.28f, .72f, t);
                var color = _marks[i].color;
                color.a = 1 - t;
                _marks[i].color = color;
            }

            // 2. Directional impact streaks (stretch & thin out along attack vector)
            for (var i = 0; i < _streaks.Length; i++)
            {
                if (!_streaks[i].enabled)
                    continue;
                _streakAge[i] += delta;
                var t = _streakAge[i] / .20f;
                if (t >= 1)
                {
                    _streaks[i].enabled = false;
                    continue;
                }

                _streaks[i].transform.localScale = new Vector3(Mathf.Lerp(0.5f, 1.35f, t), Mathf.Lerp(0.30f, 0.05f, t), 1f);
                var color = _streaks[i].color;
                color.a = (1 - t) * (1 - t);
                _streaks[i].color = color;
            }

            // 3. Soft landing dust puffs (expand horizontally and rise slightly)
            for (var i = 0; i < _dustPuffs.Length; i++)
            {
                if (!_dustPuffs[i].enabled)
                    continue;
                _dustAge[i] += delta;
                var t = _dustAge[i] / .30f;
                if (t >= 1)
                {
                    _dustPuffs[i].enabled = false;
                    continue;
                }

                _dustPuffs[i].transform.position = _dustOrigins[i] + new Vector3(0, Mathf.Lerp(0f, 0.07f, t), 0f);
                _dustPuffs[i].transform.localScale = new Vector3(Mathf.Lerp(0.3f, 0.95f, t), Mathf.Lerp(0.14f, 0.36f, t), 1f);
                var color = _dustPuffs[i].color;
                color.a = 0.65f * (1 - t);
                _dustPuffs[i].color = color;
            }
        }

        public void Dispose()
        {
            void SafeDestroy(Object obj)
            {
                if (obj == null) return;
                if (UnityEngine.Application.isPlaying) Object.Destroy(obj);
                else Object.DestroyImmediate(obj);
            }

            SafeDestroy(_sprite);
            SafeDestroy(_texture);
            SafeDestroy(_streakSprite);
            SafeDestroy(_streakTexture);
            SafeDestroy(_dustSprite);
            SafeDestroy(_dustTexture);
        }
    }
}
