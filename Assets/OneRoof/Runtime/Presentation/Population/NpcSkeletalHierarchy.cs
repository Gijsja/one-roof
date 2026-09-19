using System.Collections.Generic;
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
        public IReadOnlyDictionary<string, Transform> Bones => _bones;
        public IReadOnlyDictionary<NpcLayerKind, SpriteRenderer> WardrobeSlots => _wardrobeSlots;

        // Renderers
        public SpriteRenderer MainRenderer { get; private set; }
        public SpriteRenderer StatusPlateRenderer { get; private set; }
        public SpriteRenderer EmoteRenderer { get; private set; }
        public Transform EmoteAnchor { get; private set; }

        public int ResidentIndex { get; private set; } = -1;
        public NpcContentRecord ContentRecord { get; private set; }
        public NpcEmoteKind CurrentEmote { get; private set; } = NpcEmoteKind.None;
        public NpcAnimationClip CurrentAnimation { get; private set; } = NpcAnimationClip.Idle;
        public NpcWardrobeLoadout Wardrobe { get; private set; }

        private float _timeOffset;
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        private readonly Dictionary<NpcLayerKind, SpriteRenderer> _wardrobeSlots = new Dictionary<NpcLayerKind, SpriteRenderer>();

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void Initialize(int residentIndex)
        {
            EnsureHierarchy();
            ResidentIndex = residentIndex;
            ContentRecord = ResidentSpriteCatalog.GetRecord(residentIndex);
            ApplyWardrobe(NpcWardrobeLoadout.FromRecord(ContentRecord));
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
            _bones[NpcRigDefinition.BoneRoot] = Root;

            // 1. Build Spine bone hierarchy according to NpcRigDefinition
            Hip = EnsureBone(Root, NpcRigDefinition.BoneHip, new Vector3(0f, NpcRigDefinition.HipHeight, 0f));
            Spine = EnsureBone(Hip, NpcRigDefinition.BoneSpine, new Vector3(0f, NpcRigDefinition.SpineHeight - NpcRigDefinition.HipHeight, 0f));
            Neck = EnsureBone(Spine, NpcRigDefinition.BoneNeck, new Vector3(0f, NpcRigDefinition.NeckHeight - NpcRigDefinition.SpineHeight, 0f));
            Head = EnsureBone(Neck, NpcRigDefinition.BoneHead, new Vector3(0f, NpcRigDefinition.HeadHeight - NpcRigDefinition.NeckHeight, 0f));
            var armUpperL = EnsureBone(Spine, NpcRigDefinition.BoneArmUpperL, new Vector3(-0.08f, 0.04f, 0f));
            var armLowerL = EnsureBone(armUpperL, NpcRigDefinition.BoneArmLowerL, new Vector3(0f, -0.11f, 0f));
            EnsureBone(armLowerL, NpcRigDefinition.BoneHandL, new Vector3(0f, -0.09f, 0f));
            var armUpperR = EnsureBone(Spine, NpcRigDefinition.BoneArmUpperR, new Vector3(0.08f, 0.04f, 0f));
            var armLowerR = EnsureBone(armUpperR, NpcRigDefinition.BoneArmLowerR, new Vector3(0f, -0.11f, 0f));
            EnsureBone(armLowerR, NpcRigDefinition.BoneHandR, new Vector3(0f, -0.09f, 0f));
            var legUpperL = EnsureBone(Hip, NpcRigDefinition.BoneLegUpperL, new Vector3(-0.05f, -0.03f, 0f));
            var legLowerL = EnsureBone(legUpperL, NpcRigDefinition.BoneLegLowerL, new Vector3(0f, -0.13f, 0f));
            EnsureBone(legLowerL, NpcRigDefinition.BoneFootL, new Vector3(0f, -0.11f, 0f));
            var legUpperR = EnsureBone(Hip, NpcRigDefinition.BoneLegUpperR, new Vector3(0.05f, -0.03f, 0f));
            var legLowerR = EnsureBone(legUpperR, NpcRigDefinition.BoneLegLowerR, new Vector3(0f, -0.13f, 0f));
            EnsureBone(legLowerR, NpcRigDefinition.BoneFootR, new Vector3(0f, -0.11f, 0f));

            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                var slot = new GameObject($"Wardrobe_{layer}");
                slot.transform.SetParent(layer == NpcLayerKind.Hair || layer == NpcLayerKind.Face ? Head : Spine, false);
                var renderer = slot.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 16 + (int)layer;
                renderer.enabled = true;
                _wardrobeSlots[layer] = renderer;
            }

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
                EmoteRenderer.enabled = false;
            }
            else
            {
                EmoteRenderer.enabled = CurrentEmote != NpcEmoteKind.None;
            }
        }

        private TransitResidentStatus _currentStatus = TransitResidentStatus.Queued;

        public void SetTransitStatus(TransitResidentStatus status)
        {
            EnsureHierarchy();
            _currentStatus = status;

            SetAnimationClip(status == TransitResidentStatus.Walking ? NpcAnimationClip.Walk :
                status == TransitResidentStatus.Queued ? NpcAnimationClip.QueueWait :
                status == TransitResidentStatus.Riding ? NpcAnimationClip.Ride : NpcAnimationClip.Idle);

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

        public void SetAnimationClip(NpcAnimationClip animation) => CurrentAnimation = animation;

        public void ApplyWardrobe(NpcWardrobeLoadout wardrobe)
        {
            if (wardrobe == null) throw new System.ArgumentNullException(nameof(wardrobe));
            if (wardrobe.RigId != NpcRigDefinition.RigId) throw new System.ArgumentException("Wardrobe is incompatible with the resident rig.", nameof(wardrobe));
            Wardrobe = wardrobe;
            EnsureHierarchy();
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                if (_wardrobeSlots.TryGetValue(layer, out var renderer))
                {
                    var layerId = wardrobe.GetLayerId(layer);
                    renderer.name = $"Wardrobe_{layer}_{layerId}";
                    renderer.sprite = CreateWardrobeSwatch(layerId);
                    renderer.color = Color.HSVToRGB((Mathf.Abs(layerId.GetHashCode()) % 360) / 360f, .52f, .92f);
                    ConfigureWardrobeSlot(layer, renderer.transform);
                }
            }
        }

        public void ApplyProceduralAnimation(float globalTime)
        {
            if (Spine == null || Head == null) return;

            var t = globalTime + _timeOffset;

            if (CurrentAnimation == NpcAnimationClip.Walk)
            {
                // Walking locomotion bob & swing
                var walkSwing = Mathf.Sin(t * 8f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, walkSwing * 3.5f);
                Head.localRotation = Quaternion.Euler(0f, 0f, -walkSwing * 1.5f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Sleep)
            {
                Spine.localRotation = Quaternion.Euler(0f, 0f, 78f);
                Head.localRotation = Quaternion.Euler(0f, 0f, -16f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Sit)
            {
                Spine.localRotation = Quaternion.Euler(0f, 0f, -8f);
                Head.localRotation = Quaternion.Euler(0f, 0f, 4f);
            }
            else if (CurrentAnimation == NpcAnimationClip.QueueWait)
            {
                var shift = Mathf.Sin(t * 1.7f) * 2.5f;
                Spine.localRotation = Quaternion.Euler(0f, 0f, shift);
                Head.localRotation = Quaternion.Euler(0f, 0f, -shift * .5f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Ride)
            {
                Spine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.5f) * .7f);
                Head.localRotation = Quaternion.identity;
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

        private Transform EnsureBone(Transform parent, string boneName, Vector3 localPos)
        {
            var child = parent.Find(boneName);
            if (child == null)
            {
                var go = new GameObject(boneName);
                child = go.transform;
                child.SetParent(parent, false);
                child.localPosition = localPos;
            }
            _bones[boneName] = child;
            return child;
        }

        private static void ConfigureWardrobeSlot(NpcLayerKind layer, Transform slot)
        {
            switch (layer)
            {
                case NpcLayerKind.Hair: slot.localPosition = new Vector3(0f, .045f, -.01f); slot.localScale = new Vector3(.16f, .12f, 1f); break;
                case NpcLayerKind.Face: slot.localPosition = new Vector3(0f, -.005f, -.02f); slot.localScale = new Vector3(.11f, .07f, 1f); break;
                case NpcLayerKind.LowerClothing: slot.localPosition = new Vector3(0f, -.12f, -.03f); slot.localScale = new Vector3(.17f, .16f, 1f); break;
                case NpcLayerKind.Footwear: slot.localPosition = new Vector3(0f, -.245f, -.04f); slot.localScale = new Vector3(.18f, .045f, 1f); break;
                case NpcLayerKind.CarriedProp: slot.localPosition = new Vector3(.13f, -.05f, -.05f); slot.localScale = new Vector3(.07f, .13f, 1f); break;
                default: slot.localPosition = new Vector3(0f, -.035f, -.03f); slot.localScale = new Vector3(.20f, .20f, 1f); break;
            }
        }

        private static Sprite CreateWardrobeSwatch(string layerId)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = $"WardrobeSwatch_{layerId}", filterMode = FilterMode.Point };
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f), 2f);
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
