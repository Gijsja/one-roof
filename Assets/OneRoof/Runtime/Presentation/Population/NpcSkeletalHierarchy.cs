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
        public IReadOnlyDictionary<string, SpriteRenderer> LimbRenderers => _limbRenderers;

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
        public string WardrobeVariantKey { get; private set; }
        /// <summary>Lie direction for the Sleep clip: +1 head toward +x, -1 toward -x. Set from facing.</summary>
        public int SleepDirection { get; set; } = 1;

        private float _timeOffset;
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        private readonly Dictionary<NpcLayerKind, SpriteRenderer> _wardrobeSlots = new Dictionary<NpcLayerKind, SpriteRenderer>();
        private readonly Dictionary<string, SpriteRenderer> _limbRenderers = new Dictionary<string, SpriteRenderer>();

        private static Sprite _torsoSprite;
        private static Sprite _headSprite;
        private static Sprite _limbSprite;
        private static Sprite _handSprite;
        private static Sprite _footSprite;

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void Initialize(int residentIndex)
        {
            EnsureHierarchy();
            ResidentIndex = residentIndex;
            ContentRecord = ResidentSpriteCatalog.GetRecord(residentIndex);
            var variant = NpcWardrobeVariantCatalog.GetVariant(residentIndex);
            WardrobeVariantKey = variant != null ? variant.Key : null;
            ApplyWardrobe(NpcWardrobeLoadout.FromRecord(ContentRecord));
            ApplyVariantDiversity(variant);
            _timeOffset = (residentIndex * 0.37f) % 3.0f;

            var sprite = ResidentSpriteCatalog.GetResidentSprite(residentIndex);
            if (MainRenderer != null && sprite != null)
            {
                MainRenderer.sprite = sprite;
                MainRenderer.enabled = false;
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

            // A compact, modular silhouette based on the proportions and separate-part
            // assembly of the supplied profession sheets. The source composites are not
            // flattened into runtime content; every part stays rig-compatible.
            EnsureBaseAnatomy();

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
            var variant = CurrentVariant();
            var facePart = variant != null ? WardrobePartCatalog.GetPart(variant, NpcLayerKind.Face) : null;
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                if (!_wardrobeSlots.TryGetValue(layer, out var renderer) || renderer == null) continue;
                var layerId = wardrobe.GetLayerId(layer);
                renderer.name = $"Wardrobe_{layer}_{layerId}";
                var part = variant != null ? WardrobePartCatalog.GetPart(variant, layer) : null;
                if (part != null)
                {
                    // Sliced transparent part from the modular sheets: true color, auto-fit.
                    renderer.enabled = true;
                    renderer.sprite = part;
                    renderer.color = Color.white;
                    WardrobePartCatalog.FitSlot(renderer.transform, part, layer);
                }
                else if (layer == NpcLayerKind.Hair && facePart != null)
                {
                    // Photo head already carries hair; hide the palette swatch.
                    renderer.enabled = false;
                }
                else
                {
                    renderer.enabled = true;
                    renderer.sprite = CreateWardrobeSwatch(layerId);
                    renderer.color = ResolveWardrobeColor(layer, layerId);
                    ConfigureWardrobeSlot(layer, renderer.transform);
                }
            }
            // Photo parts fully cover the procedural boxes beneath them; hide those so no
            // box edges peek around the art. Missing parts keep the procedural body.
            SetLimbVisible("head", facePart == null);
            var upperPart = variant != null ? WardrobePartCatalog.GetPart(variant, NpcLayerKind.UpperClothing) : null;
            SetLimbVisible("torso", upperPart == null);
        }

        public void ApplyVariantDiversity(NpcWardrobeVariant variant)
        {
            if (variant == null) return;
            WardrobeVariantKey = variant.Key;
            EnsureHierarchy();

            // Re-resolve wardrobe slot colors through the variant palette so the
            // 12 profession columns from the modular sheets read at a glance.
            // Layers carrying a sliced photo part keep true-white color.
            if (Wardrobe != null)
            {
                foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
                {
                    if (_wardrobeSlots.TryGetValue(layer, out var renderer) && renderer != null)
                    {
                        if (WardrobePartCatalog.GetPart(variant, layer) == null)
                        {
                            renderer.color = ResolveWardrobeColor(layer, Wardrobe.GetLayerId(layer));
                        }
                    }
                }
            }

            // Tint the modular base anatomy: skin for head/hands, profession
            // upper/lower/footwear for torso/legs/feet. Keeps rig proportions
            // identical; only presentation colors vary.
            if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.Body), out var skin))
            {
                skin = LayerFallbackColor(NpcLayerKind.Body);
            }
            SetLimbColor("head", skin);
            SetLimbColor(NpcRigDefinition.BoneHandL, skin);
            SetLimbColor(NpcRigDefinition.BoneHandR, skin);
            if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.UpperClothing), out var upper))
            {
                upper = LayerFallbackColor(NpcLayerKind.UpperClothing);
            }
            SetLimbColor("torso", upper);
            if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.LowerClothing), out var lower))
            {
                lower = LayerFallbackColor(NpcLayerKind.LowerClothing);
            }
            // Shorts variants (bare legs) keep skin-toned legs under the photo shorts.
            if (!variant.BareLegs)
            {
                SetLimbColor(NpcRigDefinition.BoneLegUpperL, lower);
                SetLimbColor(NpcRigDefinition.BoneLegUpperR, lower);
            }
            else
            {
                if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.Body), out var bareSkin))
                {
                    bareSkin = LayerFallbackColor(NpcLayerKind.Body);
                }
                SetLimbColor(NpcRigDefinition.BoneLegUpperL, bareSkin);
                SetLimbColor(NpcRigDefinition.BoneLegUpperR, bareSkin);
            }
            if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.Footwear), out var footwear))
            {
                footwear = LayerFallbackColor(NpcLayerKind.Footwear);
            }
            SetLimbColor(NpcRigDefinition.BoneFootL, footwear);
            SetLimbColor(NpcRigDefinition.BoneFootR, footwear);

            // Subtle stature jitter (+/-5%) for crowd readability. Absolute scale
            // assignment keeps repeated Initialize calls idempotent.
            var s = Mathf.Clamp(variant.BodyScale, 0.9f, 1.1f);
            transform.localScale = new Vector3(s, s, 1f);
        }

        private Color ResolveWardrobeColor(NpcLayerKind layer, string layerId)
        {
            var variant = CurrentVariant();
            if (variant != null && TryParseHex(variant.GetLayerColor(layer), out var palette))
            {
                return palette;
            }
            // A missing palette entry must degrade to a plausible garment,
            // never the near-white parse-failure tint that read as floating boxes.
            return LayerFallbackColor(layer);
        }

        /// <summary>Plausible per-layer garment defaults when no palette entry exists.</summary>
        public static Color LayerFallbackColor(NpcLayerKind layer)
        {
            switch (layer)
            {
                case NpcLayerKind.Face: return new Color(.96f, .76f, .61f);
                case NpcLayerKind.Body: return new Color(.96f, .76f, .61f);
                case NpcLayerKind.Hair: return new Color(.25f, .18f, .14f);
                case NpcLayerKind.UpperClothing: return new Color(.35f, .45f, .60f);
                case NpcLayerKind.LowerClothing: return new Color(.30f, .34f, .42f);
                case NpcLayerKind.Footwear: return new Color(.18f, .20f, .26f);
                case NpcLayerKind.CarriedProp: return new Color(.55f, .42f, .28f);
                case NpcLayerKind.Accessory: return new Color(.75f, .65f, .45f);
                default: return new Color(.45f, .50f, .58f);
            }
        }

        private NpcWardrobeVariant CurrentVariant()
        {
            if (!string.IsNullOrEmpty(WardrobeVariantKey))
            {
                var keyed = NpcWardrobeVariantCatalog.GetByKey(WardrobeVariantKey);
                if (keyed != null) return keyed;
            }
            return ResidentIndex >= 0 ? NpcWardrobeVariantCatalog.GetVariant(ResidentIndex) : null;
        }

        private void SetLimbVisible(string key, bool visible)
        {
            if (_limbRenderers.TryGetValue(key, out var renderer) && renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        private void SetLimbColor(string key, Color color)
        {
            if (_limbRenderers.TryGetValue(key, out var renderer) && renderer != null)
            {
                renderer.color = color;
            }
        }

        private static bool TryParseHex(string hex, out Color color)
        {
            if (!string.IsNullOrWhiteSpace(hex) && ColorUtility.TryParseHtmlString(hex, out color))
            {
                return true;
            }
            color = Color.white;
            return false;
        }

        public void ApplyProceduralAnimation(float globalTime)
        {
            if (Spine == null || Head == null) return;

            var t = globalTime + _timeOffset;
            ResetLimbPose();
            if (CurrentAnimation != NpcAnimationClip.Sleep)
            {
                Root.localRotation = Quaternion.identity;
                if (StatusPlateRenderer != null) StatusPlateRenderer.transform.localRotation = Quaternion.identity;
            }

            if (CurrentAnimation == NpcAnimationClip.Walk)
            {
                // Walking locomotion bob & swing
                var walkSwing = Mathf.Sin(t * 8f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, walkSwing * 3.5f);
                Head.localRotation = Quaternion.Euler(0f, 0f, -walkSwing * 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, walkSwing * 20f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, -walkSwing * 20f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, -walkSwing * 17f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, walkSwing * 17f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Sleep)
            {
                // Lie flat: rotate the whole body about the feet so the head
                // rests toward the facing direction instead of tipping the
                // spine rigidly sideways mid-air. The underfoot plate is
                // counter-rotated so it stays a floor shadow.
                var dir = SleepDirection >= 0 ? 1f : -1f;
                Root.localRotation = Quaternion.Euler(0f, 0f, -75f * dir);
                Head.localRotation = Quaternion.Euler(0f, 0f, 8f * dir);
                if (StatusPlateRenderer != null)
                {
                    StatusPlateRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 75f * dir);
                }
            }
            else if (CurrentAnimation == NpcAnimationClip.Sit)
            {
                Spine.localRotation = Quaternion.Euler(0f, 0f, -8f);
                Head.localRotation = Quaternion.Euler(0f, 0f, 4f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, 58f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, 58f);
            }
            else if (CurrentAnimation == NpcAnimationClip.QueueWait)
            {
                var shift = Mathf.Sin(t * 1.7f) * 2.5f;
                Spine.localRotation = Quaternion.Euler(0f, 0f, shift);
                Head.localRotation = Quaternion.Euler(0f, 0f, -shift * .5f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, shift * 2f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, -shift * 2f);
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

        private void EnsureBaseAnatomy()
        {
            EnsureLimbRenderer("torso", Spine, _torsoSprite ?? (_torsoSprite = CreatePixelPartSprite(10, 14)), new Vector3(0f, -.065f, .02f), new Vector3(.18f, .25f, 1f), 17, new Color(.78f, .62f, .48f));
            EnsureLimbRenderer("head", Head, _headSprite ?? (_headSprite = CreatePixelPartSprite(10, 10)), new Vector3(0f, .025f, .01f), new Vector3(.14f, .14f, 1f), 19, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmUpperL, _bones[NpcRigDefinition.BoneArmUpperL], _limbSprite ?? (_limbSprite = CreatePixelPartSprite(4, 12)), new Vector3(-.018f, -.055f, .03f), new Vector3(.052f, .12f, 1f), 16, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmUpperR, _bones[NpcRigDefinition.BoneArmUpperR], _limbSprite, new Vector3(.018f, -.055f, .03f), new Vector3(.052f, .12f, 1f), 18, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegUpperL, _bones[NpcRigDefinition.BoneLegUpperL], _limbSprite, new Vector3(0f, -.075f, .03f), new Vector3(.065f, .17f, 1f), 16, new Color(.34f, .39f, .47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegUpperR, _bones[NpcRigDefinition.BoneLegUpperR], _limbSprite, new Vector3(0f, -.075f, .03f), new Vector3(.065f, .17f, 1f), 18, new Color(.34f, .39f, .47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneHandL, _bones[NpcRigDefinition.BoneHandL], _handSprite ?? (_handSprite = CreatePixelPartSprite(5, 5)), new Vector3(0f, -.018f, .03f), new Vector3(.045f, .045f, 1f), 16, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneHandR, _bones[NpcRigDefinition.BoneHandR], _handSprite, new Vector3(0f, -.018f, .03f), new Vector3(.045f, .045f, 1f), 18, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneFootL, _bones[NpcRigDefinition.BoneFootL], _footSprite ?? (_footSprite = CreatePixelPartSprite(8, 3)), new Vector3(-.012f, -.015f, .03f), new Vector3(.08f, .034f, 1f), 16, new Color(.16f, .19f, .24f));
            EnsureLimbRenderer(NpcRigDefinition.BoneFootR, _bones[NpcRigDefinition.BoneFootR], _footSprite, new Vector3(.012f, -.015f, .03f), new Vector3(.08f, .034f, 1f), 18, new Color(.16f, .19f, .24f));
        }

        private void EnsureLimbRenderer(string key, Transform parent, Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder, Color color)
        {
            if (_limbRenderers.TryGetValue(key, out var existing) && existing != null) return;
            var go = new GameObject($"Anatomy_{key}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            _limbRenderers[key] = renderer;
        }

        private void ResetLimbPose()
        {
            foreach (var pair in _bones)
            {
                if (pair.Key != NpcRigDefinition.BoneRoot && pair.Value != null) pair.Value.localRotation = Quaternion.identity;
            }
        }

        private void SetBoneRotation(string boneName, float zDegrees)
        {
            if (_bones.TryGetValue(boneName, out var bone) && bone != null) bone.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
        }

        private static Sprite CreatePixelPartSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"NpcPrototypePart_{width}x{height}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, 1f), height);
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
