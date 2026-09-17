using OneRoof.Application.Transit;
using OneRoof.Content;
using UnityEngine;

namespace OneRoof.Presentation.Population
{
    /// <summary>
    /// Presentation component managing the Spine 2D skeletal hierarchy and slot renderers.
    /// Attaches to resident GameObjects to provide bone tracking, slot layering, and transit status plates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcSkeletalHierarchy : MonoBehaviour
    {
        // Canonical bone transforms
        public Transform Root { get; private set; }
        public Transform Hip { get; private set; }
        public Transform Spine { get; private set; }
        public Transform Neck { get; private set; }
        public Transform Head { get; private set; }

        // Renderers
        public SpriteRenderer MainRenderer { get; private set; }
        public SpriteRenderer StatusPlateRenderer { get; private set; }
        public SpriteRenderer EmoteRenderer { get; private set; }
        public Transform EmoteAnchor { get; private set; }

        public int ResidentIndex { get; private set; } = -1;
        public NpcContentRecord ContentRecord { get; private set; }
        public NpcEmoteKind CurrentEmote { get; private set; } = NpcEmoteKind.None;

        private float _timeOffset;

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void Initialize(int residentIndex)
        {
            EnsureHierarchy();
            ResidentIndex = residentIndex;
            ContentRecord = ResidentSpriteCatalog.GetRecord(residentIndex);
            _timeOffset = (residentIndex * 0.37f) % 3.0f;

            var sprite = ResidentSpriteCatalog.GetResidentSprite(residentIndex);
            if (MainRenderer != null && sprite != null)
            {
                MainRenderer.sprite = sprite;
            }

            SetTransitStatus(TransitResidentStatus.Queued);
            SetEmote(NpcEmoteKind.None);
        }

        public void EnsureHierarchy()
        {
            if (Root != null) return;

            Root = transform;

            // 1. Build Spine bone hierarchy according to NpcRigDefinition
            Hip = EnsureBone(Root, NpcRigDefinition.BoneHip, new Vector3(0f, NpcRigDefinition.HipHeight, 0f));
            Spine = EnsureBone(Hip, NpcRigDefinition.BoneSpine, new Vector3(0f, NpcRigDefinition.SpineHeight - NpcRigDefinition.HipHeight, 0f));
            Neck = EnsureBone(Spine, NpcRigDefinition.BoneNeck, new Vector3(0f, NpcRigDefinition.NeckHeight - NpcRigDefinition.SpineHeight, 0f));
            Head = EnsureBone(Neck, NpcRigDefinition.BoneHead, new Vector3(0f, NpcRigDefinition.HeadHeight - NpcRigDefinition.NeckHeight, 0f));

            // 2. Main resident character SpriteRenderer
            MainRenderer = GetComponent<SpriteRenderer>();
            if (MainRenderer == null)
            {
                MainRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            MainRenderer.sortingOrder = 15;

            // 3. Underfoot status plate shadow / disc
            var plateGo = transform.Find("StatusPlate")?.gameObject;
            if (plateGo == null)
            {
                plateGo = new GameObject("StatusPlate");
                plateGo.transform.SetParent(transform, false);
                plateGo.transform.localPosition = new Vector3(0f, 0.02f, 0.02f);
                plateGo.transform.localScale = new Vector3(0.24f, 0.05f, 1f);
            }

            StatusPlateRenderer = plateGo.GetComponent<SpriteRenderer>();
            if (StatusPlateRenderer == null)
            {
                StatusPlateRenderer = plateGo.AddComponent<SpriteRenderer>();
                StatusPlateRenderer.sprite = CreateDiscSprite();
                StatusPlateRenderer.sortingOrder = 10;
            }

            // 4. Overhead Emote Bubble Anchor & Renderer
            var emoteGo = transform.Find("EmoteBubble")?.gameObject;
            if (emoteGo == null)
            {
                emoteGo = new GameObject("EmoteBubble");
                emoteGo.transform.SetParent(Head != null ? Head : transform, false);
                emoteGo.transform.localPosition = new Vector3(0f, 0.18f, -0.05f);
                emoteGo.transform.localScale = Vector3.one;
            }

            EmoteAnchor = emoteGo.transform;
            EmoteRenderer = emoteGo.GetComponent<SpriteRenderer>();
            if (EmoteRenderer == null)
            {
                EmoteRenderer = emoteGo.AddComponent<SpriteRenderer>();
                EmoteRenderer.sortingOrder = 25;
            }
        }

        private TransitResidentStatus _currentStatus = TransitResidentStatus.Queued;

        public void SetTransitStatus(TransitResidentStatus status)
        {
            EnsureHierarchy();
            _currentStatus = status;

            switch (status)
            {
                case TransitResidentStatus.Queued:
                    // Waiting in lobby / landing: subtle warm amber status glow
                    if (MainRenderer != null) MainRenderer.color = new Color(1f, 0.95f, 0.88f);
                    if (StatusPlateRenderer != null) StatusPlateRenderer.color = new Color(1f, 0.65f, 0.25f, 0.85f);
                    break;

                case TransitResidentStatus.Riding:
                    // Riding elevator: cyan transit status glow
                    if (MainRenderer != null) MainRenderer.color = new Color(0.88f, 0.96f, 1f);
                    if (StatusPlateRenderer != null) StatusPlateRenderer.color = new Color(0.25f, 0.85f, 1f, 0.85f);
                    break;

                case TransitResidentStatus.Walking:
                    // Walking along corridor: crisp natural color with cyan locomotion plate
                    if (MainRenderer != null) MainRenderer.color = new Color(0.95f, 0.98f, 1f);
                    if (StatusPlateRenderer != null) StatusPlateRenderer.color = new Color(0.30f, 0.75f, 0.95f, 0.75f);
                    break;

                case TransitResidentStatus.InRoom:
                case TransitResidentStatus.Arrived:
                default:
                    // Settled inside room: natural full color with soft green settled shadow
                    if (MainRenderer != null) MainRenderer.color = Color.white;
                    if (StatusPlateRenderer != null) StatusPlateRenderer.color = new Color(0.28f, 0.65f, 0.40f, 0.55f);
                    break;
            }
        }

        public void SetEmote(NpcEmoteKind emote)
        {
            EnsureHierarchy();
            if (CurrentEmote == emote) return;

            CurrentEmote = emote;
            if (emote == NpcEmoteKind.None)
            {
                if (EmoteRenderer != null)
                {
                    EmoteRenderer.enabled = false;
                    EmoteRenderer.sprite = null;
                }
            }
            else
            {
                if (EmoteRenderer != null)
                {
                    EmoteRenderer.enabled = true;
                    EmoteRenderer.sprite = EmoteSpriteCatalog.GetSprite(emote, 0);
                }
            }
        }

        public void ApplyProceduralAnimation(float globalTime)
        {
            if (Spine == null || Head == null) return;

            var t = globalTime + _timeOffset;

            if (_currentStatus == TransitResidentStatus.Walking)
            {
                // Walking locomotion bob & swing
                var walkSwing = Mathf.Sin(t * 8f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, walkSwing * 3.5f);
                Head.localRotation = Quaternion.Euler(0f, 0f, -walkSwing * 1.5f);
            }
            else
            {
                // Subtle idle breathing motion
                var breatheAngle = Mathf.Sin(t * 2.1f) * 1.2f;
                Spine.localRotation = Quaternion.Euler(0f, 0f, breatheAngle);
                Head.localRotation = Quaternion.Euler(0f, 0f, -breatheAngle * 0.7f);
            }

            // Emote bubble frame animation and subtle float
            if (CurrentEmote != NpcEmoteKind.None && EmoteRenderer != null)
            {
                var frame = (int)((globalTime / 0.15f) % 3);
                var sprite = EmoteSpriteCatalog.GetSprite(CurrentEmote, frame);
                if (sprite != null)
                {
                    EmoteRenderer.sprite = sprite;
                }
                EmoteRenderer.enabled = true;

                if (EmoteAnchor != null)
                {
                    var bob = Mathf.Sin((globalTime + _timeOffset) * 4f) * 0.015f;
                    EmoteAnchor.localPosition = new Vector3(0f, 0.18f + bob, -0.05f);
                }
            }
            else if (EmoteRenderer != null && EmoteRenderer.enabled)
            {
                EmoteRenderer.enabled = false;
            }
        }

        private static Transform EnsureBone(Transform parent, string boneName, Vector3 localPos)
        {
            var child = parent.Find(boneName);
            if (child == null)
            {
                var go = new GameObject(boneName);
                child = go.transform;
                child.SetParent(parent, false);
                child.localPosition = localPos;
            }
            return child;
        }

        private static Sprite CreateDiscSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var colors = new Color[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(1f - dist);
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
