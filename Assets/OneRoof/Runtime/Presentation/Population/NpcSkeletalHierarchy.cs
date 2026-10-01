using System.Collections.Generic;
using OneRoof.Application.Transit;
using OneRoof.Content;
using OneRoof.Presentation.Tower;
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
        public TextMesh ActivityCaption { get; private set; }
        public VisualEffectsPresenter VisualEffects { get; private set; }

        public int ResidentIndex { get; private set; } = -1;
        public NpcContentRecord ContentRecord { get; private set; }
        public NpcEmoteKind CurrentEmote { get; private set; } = NpcEmoteKind.None;
        public NpcAnimationClip CurrentAnimation { get; private set; } = NpcAnimationClip.Idle;
        public NpcWardrobeLoadout Wardrobe { get; private set; }
        public string WardrobeVariantKey { get; private set; }
        /// <summary>Lie direction for the Sleep clip: +1 head toward +x, -1 toward -x. Set from facing.</summary>
        public int SleepDirection { get; set; } = 1;

        // Animator split: stature (uniform squash & stretch from the variant) lives apart from
        // facing (mirror direction from the presenter). They compose as (facing * stature,
        // stature, 1) so turning around never wipes body diversity and sizing up never
        // straightens a flipped resident.
        private float _stature = 1f;
        private float _facing = 1f;
        private float _avatarWidthScale = 1f;
        private float _avatarHeightScale = 1f;
        private float _poseWidthScale = 1f;
        private float _poseHeightScale = 1f;
        /// <summary>Signed facing multiplier: +1 faces +x, -1 faces -x. Preserves stature.</summary>
        public float FacingDirection => _facing;
        /// <summary>Uniform stature scale from the wardrobe variant (0.9..1.1).</summary>
        public float BodyStature => _stature;

        private float _timeOffset;
        private Vector3 _hipBasePos = new Vector3(0f, 0.28f, 0f);
        private readonly Dictionary<string, Transform> _bones = new Dictionary<string, Transform>();
        private readonly Dictionary<NpcLayerKind, SpriteRenderer> _wardrobeSlots = new Dictionary<NpcLayerKind, SpriteRenderer>();
        private readonly Dictionary<string, SpriteRenderer> _limbRenderers = new Dictionary<string, SpriteRenderer>();

        private static Sprite _torsoSprite;
        private static Sprite _headSprite;
        private static Sprite _limbSprite;
        private static Sprite _handSprite;
        private static Sprite _footSprite;
        private static Sprite _captionBackgroundSprite;
        private string _caption;
        private Transform _bubbleCompensation;
        private SpriteRenderer _captionBackground;
        private MeshRenderer _captionTextRenderer;
        private bool _bubbleVisible = true;
        private SpriteRenderer _bubbleTail;
        private SpriteRenderer _thoughtDot;
        private static Sprite _actionTailSprite;
        private static Sprite _thoughtTailSprite;
        private bool _isThought;
        public Bounds BubbleBounds
        {
            get
            {
                var bounds = _captionBackground != null && !string.IsNullOrEmpty(_caption)
                    ? _captionBackground.bounds : EmoteRenderer != null ? EmoteRenderer.bounds : new Bounds(transform.position, Vector3.zero);
                if (_bubbleTail != null && !string.IsNullOrEmpty(_caption)) bounds.Encapsulate(_bubbleTail.bounds);
                if (_isThought && _thoughtDot != null && !string.IsNullOrEmpty(_caption)) bounds.Encapsulate(_thoughtDot.bounds);
                return bounds;
            }
        }
        private Material _sharedMaterial;
        private static Material s_defaultSharedMaterial;
        private static Material s_bubbleSharedMaterial;

        public static Material GetOrCreateDefaultSharedMaterial()
        {
            if (s_defaultSharedMaterial != null) return s_defaultSharedMaterial;
            // SpriteRenderer supplies its palette through renderer tint properties.
            // The general surface shader does not consume those properties.
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                s_defaultSharedMaterial = new Material(shader) { name = "Npc_DefaultSharedMaterial", enableInstancing = true };
            }
            return s_defaultSharedMaterial;
        }

        private static Material GetOrCreateBubbleMaterial()
        {
            if (s_bubbleSharedMaterial != null) return s_bubbleSharedMaterial;
            // Tower surfaces use an opaque material; bubbles require sprite alpha and tint.
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
                s_bubbleSharedMaterial = new Material(shader) { name = "ResidentBubble_Transparent" };
            return s_bubbleSharedMaterial;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCaches()
        {
            _torsoSprite = null;
            _headSprite = null;
            _limbSprite = null;
            _handSprite = null;
            _footSprite = null;
            _captionBackgroundSprite = null;
            _actionTailSprite = null;
            _thoughtTailSprite = null;
            s_defaultSharedMaterial = null;
            s_bubbleSharedMaterial = null;
        }

        private void Awake()
        {
            EnsureHierarchy();
        }

        public void ApplySharedMaterial(Material material)
        {
            if (material == null) return;
            _sharedMaterial = material;
            if (MainRenderer != null) MainRenderer.sharedMaterial = material;
            if (StatusPlateRenderer != null) StatusPlateRenderer.sharedMaterial = material;
            foreach (var slot in _wardrobeSlots.Values)
            {
                if (slot != null) slot.sharedMaterial = material;
            }
            foreach (var limb in _limbRenderers.Values)
            {
                if (limb != null) limb.sharedMaterial = material;
            }
        }

        public void Initialize(int residentIndex, Material defaultMaterial = null)
        {
            EnsureHierarchy();
            ApplySharedMaterial(defaultMaterial ?? _sharedMaterial ?? GetOrCreateDefaultSharedMaterial());
            ResidentIndex = residentIndex;
            _facing = 1f;
            _poseWidthScale = 1f;
            _poseHeightScale = 1f;
            Root.localRotation = Quaternion.identity;
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
                // Neutral generated proxy supports picking and LOD only.
                // The visible body is always assembled from articulated rig parts.
                MainRenderer.enabled = false;
                var worldWidth = ContentRecord != null ? ContentRecord.WorldWidth : NpcRigDefinition.NominalWorldWidth;
                var worldHeight = ContentRecord != null ? ContentRecord.WorldHeight : NpcRigDefinition.NominalWorldHeight;
                _avatarWidthScale = sprite.bounds.size.x > 0f ? worldWidth / sprite.bounds.size.x : 1f;
                _avatarHeightScale = sprite.bounds.size.y > 0f ? worldHeight / sprite.bounds.size.y : 1f;
                UpdateBodyScale();
            }

            _bubbleVisible = true;
            SetTransitStatus(TransitResidentStatus.Queued);
            SetEmote(NpcEmoteKind.None);
            SetCaption(null);
        }

        public void EnsureHierarchy()
        {
            if (Root != null)
            {
                RepointWardrobeSlots();
                return;
            }

            Root = transform;
            _bones[NpcRigDefinition.BoneRoot] = Root;
            VisualEffects = GetComponent<VisualEffectsPresenter>() ?? gameObject.AddComponent<VisualEffectsPresenter>();

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
                var parentBone = NpcRigDefinition.GetParentBone(layer);
                var parent = parentBone != null && _bones.TryGetValue(parentBone, out var bone) && bone != null
                    ? bone
                    : Spine;
                var existing = FindAuthoredSlot(layer);
                Transform slotTransform;
                SpriteRenderer renderer;
                if (existing != null)
                {
                    slotTransform = existing;
                    renderer = existing.GetComponent<SpriteRenderer>();
                    if (renderer == null) renderer = existing.gameObject.AddComponent<SpriteRenderer>();
                }
                else
                {
                    var slot = new GameObject($"Wardrobe_{layer}");
                    slotTransform = slot.transform;
                    renderer = slot.AddComponent<SpriteRenderer>();
                }
                if (_sharedMaterial != null) renderer.sharedMaterial = _sharedMaterial;
                slotTransform.SetParent(parent, false);
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
            if (_sharedMaterial != null) MainRenderer.sharedMaterial = _sharedMaterial;
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
            if (_sharedMaterial != null) StatusPlateRenderer.sharedMaterial = _sharedMaterial;

            // 4. Overhead Emote Bubble Anchor & Renderer
            var emoteGo = transform.Find("EmoteBubble")?.gameObject;
            if (emoteGo == null)
            {
                emoteGo = new GameObject("EmoteBubble");
                emoteGo.transform.SetParent(transform, false);
                emoteGo.transform.localPosition = new Vector3(0f, 0.18f, -0.05f);
                emoteGo.transform.localScale = Vector3.one;
            }

            _bubbleCompensation = new GameObject("BubblePoseCompensation").transform;
            _bubbleCompensation.SetParent(transform, false);
            EmoteAnchor = emoteGo.transform;
            EmoteAnchor.SetParent(_bubbleCompensation, false);
            var oldEmoteRenderer = emoteGo.GetComponent<SpriteRenderer>();
            if (oldEmoteRenderer != null) oldEmoteRenderer.enabled = false;
            var iconGo = new GameObject("EmotionIcon");
            iconGo.transform.SetParent(EmoteAnchor, false);
            EmoteRenderer = iconGo.GetComponent<SpriteRenderer>();
            if (EmoteRenderer == null)
            {
                EmoteRenderer = iconGo.AddComponent<SpriteRenderer>();
                EmoteRenderer.sortingOrder = 25;
                EmoteRenderer.enabled = false;
            }
            else
            {
                EmoteRenderer.enabled = CurrentEmote != NpcEmoteKind.None;
            }
            EmoteRenderer.sharedMaterial = GetOrCreateBubbleMaterial();

            var captionGo = EmoteAnchor.Find("ActivityCaption")?.gameObject;
            if (captionGo == null)
            {
                captionGo = new GameObject("ActivityCaption");
                captionGo.transform.SetParent(EmoteAnchor, false);
                captionGo.transform.localPosition = new Vector3(0.09f, -0.015f, -0.02f);
            }
            ActivityCaption = captionGo.GetComponent<TextMesh>();
            if (ActivityCaption == null)
            {
                ActivityCaption = captionGo.AddComponent<TextMesh>();
                ActivityCaption.anchor = TextAnchor.MiddleCenter;
                ActivityCaption.alignment = TextAlignment.Center;
                ActivityCaption.fontSize = 48;
                ActivityCaption.color = new Color(0.08f, 0.13f, 0.19f);
                captionGo.GetComponent<MeshRenderer>().sortingOrder = 27;
            }
            // Captions sit above every visible resident; keep them compact at
            // gameplay zoom so neighbouring routines remain distinguishable.
            ActivityCaption.characterSize = 0.028f;
            ActivityCaption.fontSize = 64;
            ActivityCaption.richText = false;
            ActivityCaption.transform.localPosition = new Vector3(0.08f, 0f, -0.02f);
            _captionTextRenderer = captionGo.GetComponent<MeshRenderer>();
            var backgroundGo = EmoteAnchor.Find("CaptionBackground")?.gameObject;
            if (backgroundGo == null)
            {
                backgroundGo = new GameObject("CaptionBackground");
                backgroundGo.transform.SetParent(EmoteAnchor, false);
                backgroundGo.transform.localPosition = new Vector3(0.06f, -0.015f, 0.01f);
            }
            backgroundGo.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            backgroundGo.transform.localScale = new Vector3(0.92f, 0.816f, 1f);
            var background = backgroundGo.GetComponent<SpriteRenderer>();
            if (background == null)
            {
                background = backgroundGo.AddComponent<SpriteRenderer>();
                background.sprite = GetCaptionBackgroundSprite();
                background.color = new Color(0.91f, 0.96f, 0.98f, 0.93f);
                background.sortingOrder = 24;
            }
            background.sharedMaterial = GetOrCreateBubbleMaterial();
            _captionBackground = background;
            background.enabled = !string.IsNullOrEmpty(_caption);
            ActivityCaption.gameObject.SetActive(!string.IsNullOrEmpty(_caption));
            var tailGo = new GameObject("BubbleTail");
            tailGo.transform.SetParent(EmoteAnchor, false);
            tailGo.transform.localPosition = new Vector3(-0.10f, -0.175f, 0.012f);
            tailGo.transform.localScale = new Vector3(0.07f, 0.07f, 1f);
            _bubbleTail = tailGo.AddComponent<SpriteRenderer>();
            _bubbleTail.sprite = GetBubbleTailSprite(false);
            _bubbleTail.sharedMaterial = GetOrCreateBubbleMaterial();
            _bubbleTail.sortingOrder = 24;
            _bubbleTail.enabled = false;
            var dotGo = new GameObject("ThoughtTrailDot");
            dotGo.transform.SetParent(EmoteAnchor, false);
            dotGo.transform.localPosition = new Vector3(-0.12f, -0.245f, 0.012f);
            dotGo.transform.localScale = new Vector3(0.028f, 0.028f, 1f);
            _thoughtDot = dotGo.AddComponent<SpriteRenderer>();
            _thoughtDot.sprite = GetBubbleTailSprite(true);
            _thoughtDot.sharedMaterial = GetOrCreateBubbleMaterial();
            _thoughtDot.sortingOrder = 24;
            _thoughtDot.enabled = false;

            // Bind-pose carry for procedural locomotion: hip bob/sway offsets compose on top.
            if (Hip != null) _hipBasePos = Hip.localPosition;
        }

        private TransitResidentStatus _currentStatus = TransitResidentStatus.Queued;

        public void SetTransitStatus(TransitResidentStatus status)
        {
            EnsureHierarchy();
            _currentStatus = status;

            SetAnimationClip(status == TransitResidentStatus.Walking ? NpcAnimationClip.Walk :
                status == TransitResidentStatus.Queued ? NpcAnimationClip.QueueWait :
                status == TransitResidentStatus.Riding ? NpcAnimationClip.Ride : NpcAnimationClip.Idle,
                status == TransitResidentStatus.Walking ? "Transit: walking" :
                status == TransitResidentStatus.Queued ? "Transit: lift queue" :
                status == TransitResidentStatus.Riding ? "Transit: lift ride" : "Transit: settled");

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
                    EmoteRenderer.enabled = _bubbleVisible;
                    EmoteRenderer.sprite = EmoteSpriteCatalog.GetSprite(emote, 0);
                }
            }
        }

        public void SetBubble(ResidentBubbleCue cue)
        {
            EnsureHierarchy();
            _isThought = cue.Kind == ResidentBubbleKind.Thought;
            var color = _isThought ? new Color(0.93f, 0.92f, 1f, 0.97f) : new Color(0.93f, 0.97f, 1f, 0.97f);
            var emote = NpcEmoteKind.Ellipsis;
            switch (cue.Emotion)
            {
                case ResidentBubbleEmotion.Connected: color = new Color(0.87f, 0.98f, 0.91f, 0.97f); emote = NpcEmoteKind.ThumbsUp; break;
                case ResidentBubbleEmotion.Flirty: color = new Color(1f, 0.88f, 0.94f, 0.97f); emote = NpcEmoteKind.Heart; break;
                case ResidentBubbleEmotion.Embarrassed: color = new Color(1f, 0.93f, 0.86f, 0.97f); emote = NpcEmoteKind.Blush; break;
                case ResidentBubbleEmotion.Angry: color = new Color(1f, 0.88f, 0.85f, 0.97f); emote = NpcEmoteKind.Anger; break;
                case ResidentBubbleEmotion.Impatient: color = new Color(1f, 0.95f, 0.80f, 0.97f); emote = NpcEmoteKind.Sweat; break;
                default:
                    if (!_isThought) emote = cue.Text == "Sleeping" ? NpcEmoteKind.Sleeping : NpcEmoteKind.Target;
                    break;
            }
            _captionBackground.color = color;
            _bubbleTail.color = color;
            _thoughtDot.color = color;
            _bubbleTail.sprite = GetBubbleTailSprite(_isThought);
            _bubbleTail.transform.localRotation = Quaternion.identity;
            _bubbleTail.transform.localPosition = new Vector3(-0.10f, _isThought ? -0.195f : -0.19f, 0.012f);
            _bubbleTail.transform.localScale = _isThought ? new Vector3(0.055f, 0.055f, 1f) : new Vector3(0.10f, 0.075f, 1f);
            SetCaption(cue.Text);
            SetEmote(emote);
            SetBubbleVisible(_bubbleVisible);
        }

        public void SetCaption(string caption)
        {
            EnsureHierarchy();
            if (_caption == caption) return;
            _caption = caption;
            // A short bounded caption is safer than text escaping into neighbouring rooms.
            var display = caption ?? string.Empty;
            if (display.Length > 44) display = display.Substring(0, 41).TrimEnd() + "...";
            if (!display.Contains("\n") && display.Length > 16)
            {
                var split = display.LastIndexOf(' ', Mathf.Min(22, display.Length - 1));
                if (split > 0) display = display.Substring(0, split) + "\n" + display.Substring(split + 1);
            }
            ActivityCaption.text = display;
            ActivityCaption.transform.localScale = Vector3.one;
            // Mesh bounds include real font metrics rather than an assumed character width.
            var size = _captionTextRenderer.localBounds.size;
            var fit = Mathf.Min(1f, 0.64f / Mathf.Max(0.001f, size.x), 0.24f / Mathf.Max(0.001f, size.y));
            ActivityCaption.transform.localScale = new Vector3(fit, fit, 1f);
            EmoteRenderer.transform.localPosition = string.IsNullOrEmpty(caption)
                ? Vector3.zero : new Vector3(-0.34f, 0f, -0.01f);
            EmoteRenderer.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            SetBubbleVisible(_bubbleVisible);
            RefreshBubblePose();
        }

        public void SetBubbleVisible(bool visible)
        {
            _bubbleVisible = visible;
            if (ActivityCaption != null) ActivityCaption.gameObject.SetActive(visible && !string.IsNullOrEmpty(_caption));
            if (_captionBackground != null) _captionBackground.enabled = visible && !string.IsNullOrEmpty(_caption);
            if (_bubbleTail != null) _bubbleTail.enabled = visible && !string.IsNullOrEmpty(_caption);
            if (_thoughtDot != null) _thoughtDot.enabled = visible && _isThought && !string.IsNullOrEmpty(_caption);
            if (EmoteRenderer != null) EmoteRenderer.enabled = visible && CurrentEmote != NpcEmoteKind.None;
        }

        public void RefreshBubblePose()
        {
            if (EmoteAnchor == null || _bubbleCompensation == null) return;
            var scale = transform.localScale;
            _bubbleCompensation.localScale = new Vector3(
                1f / (Mathf.Abs(scale.x) > 0.001f ? scale.x : 1f),
                1f / (Mathf.Abs(scale.y) > 0.001f ? scale.y : 1f), 1f);
            EmoteAnchor.rotation = Quaternion.identity;
            EmoteAnchor.position = transform.position + new Vector3(0f, 0.77f, -0.05f);
        }

        private static Sprite GetBubbleTailSprite(bool thought)
        {
            if (thought)
            {
                if (_thoughtTailSprite == null) _thoughtTailSprite = CreateDiscSprite();
                return _thoughtTailSprite;
            }
            if (_actionTailSprite != null) return _actionTailSprite;
            const int size = 24;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "ResidentActionPointer" };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                pixels[y * size + x] = Mathf.Abs(x + 0.5f - size * 0.5f) <= (y + 1f) * 0.5f ? Color.white : Color.clear;
            texture.SetPixels(pixels);
            texture.Apply();
            _actionTailSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _actionTailSprite;
        }

        private static Sprite GetCaptionBackgroundSprite()
        {
            if (_captionBackgroundSprite != null) return _captionBackgroundSprite;
            const int width = 96, height = 40;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "ResidentBubbleSurface" };
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - 8f));
                var dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - 8f));
                pixels[y * width + x] = dx * dx + dy * dy <= 64f ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            _captionBackgroundSprite = Sprite.Create(texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), width);
            return _captionBackgroundSprite;
        }

        public string AnimationSelectionReason { get; private set; } = "Default idle";

        public void SetAnimationClip(NpcAnimationClip animation, string reason = "Explicit pose selection")
        {
            CurrentAnimation = animation;
            AnimationSelectionReason = reason;
        }

        /// <summary>
        /// Faces the resident toward a signed x direction without touching stature.
        /// Replaces direct <c>transform.localScale = (±1,1,1)</c> writes, which used to wipe the
        /// variant's body scale. Zero or NaN keeps the current facing.
        /// </summary>
        public void SetFacing(float direction)
        {
            if (float.IsNaN(direction) || Mathf.Approximately(direction, 0f)) return;
            EnsureHierarchy();
            _facing = direction >= 0f ? 1f : -1f;
            UpdateBodyScale();
        }

        private void UpdateBodyScale()
        {
            if (transform != null)
                transform.localScale = new Vector3(
                    _facing * _stature * _avatarWidthScale * _poseWidthScale,
                    _stature * _avatarHeightScale * _poseHeightScale,
                    1f);
        }

        private void SetWholeBodyPose(float widthScale, float heightScale, float rotationDegrees)
        {
            _poseWidthScale = widthScale;
            _poseHeightScale = heightScale;
            UpdateBodyScale();
            Root.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        }

        public void ApplyWardrobe(NpcWardrobeLoadout wardrobe)
        {
            if (wardrobe == null) throw new System.ArgumentNullException(nameof(wardrobe));
            if (wardrobe.RigId != NpcRigDefinition.RigId) throw new System.ArgumentException("Wardrobe is incompatible with the resident rig.", nameof(wardrobe));
            Wardrobe = wardrobe;
            EnsureHierarchy();
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                if (!_wardrobeSlots.TryGetValue(layer, out var renderer) || renderer == null) continue;
                var layerId = wardrobe.GetLayerId(layer);
                renderer.name = $"Wardrobe_{layer}_{layerId}";
                // The photo cutouts are not authored to these joint proportions.
                // Palette-tinted anatomy supplies the visible rig until matching
                // wardrobe slices exist.
                renderer.enabled = false;
            }
            foreach (var limb in _limbRenderers.Values)
                if (limb != null) limb.enabled = true;
        }

        public void ApplyVariantDiversity(NpcWardrobeVariant variant)
        {
            if (variant == null) return;
            WardrobeVariantKey = variant.Key;
            EnsureHierarchy();

            // Tint the canonical rig anatomy from the resident's palette.
            if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.Body), out var skin))
            {
                skin = LayerFallbackColor(NpcLayerKind.Body);
            }
            SetLimbColor("head", skin);
            SetLimbColor(NpcRigDefinition.BoneHandL, skin);
            SetLimbColor(NpcRigDefinition.BoneHandR, skin);
            SetLimbColor(NpcRigDefinition.BoneArmLowerL, skin);
            SetLimbColor(NpcRigDefinition.BoneArmLowerR, skin);
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
                SetLimbColor(NpcRigDefinition.BoneLegLowerL, lower);
                SetLimbColor(NpcRigDefinition.BoneLegLowerR, lower);
            }
            else
            {
                if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.Body), out var bareSkin))
                {
                    bareSkin = LayerFallbackColor(NpcLayerKind.Body);
                }
                SetLimbColor(NpcRigDefinition.BoneLegUpperL, bareSkin);
                SetLimbColor(NpcRigDefinition.BoneLegUpperR, bareSkin);
                SetLimbColor(NpcRigDefinition.BoneLegLowerL, bareSkin);
                SetLimbColor(NpcRigDefinition.BoneLegLowerR, bareSkin);
            }
            if (!TryParseHex(variant.GetLayerColor(NpcLayerKind.Footwear), out var footwear))
            {
                footwear = LayerFallbackColor(NpcLayerKind.Footwear);
            }
            SetLimbColor(NpcRigDefinition.BoneFootL, footwear);
            SetLimbColor(NpcRigDefinition.BoneFootR, footwear);

            // Subtle stature jitter (+/-5%) for crowd readability. Absolute scale
            // assignment keeps repeated Initialize calls idempotent; facing composes
            // multiplicatively so turning never wipes diversity.
            var s = Mathf.Clamp(variant.BodyScale, 0.9f, 1.1f);
            _stature = s;
            UpdateBodyScale();
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

        /// <summary>Time input of the last evaluated pose, exposed for animation inspection.</summary>
        public float LastAnimationSampleTime { get; private set; }
        public float AnimationPhaseOffset => _timeOffset;

        public void ApplyProceduralAnimation(float globalTime)
        {
            if (Spine == null || Head == null) return;
            LastAnimationSampleTime = globalTime;

            // Alan Becker stick figure motion dynamics:
            // 1. Rotation convention (front view, facing +x): +z swings a hanging limb forward.
            // 2. Overlapping drag & follow-through: distal extremities (forearms, hands) lag behind
            //    proximal drivers (shoulders) by trailing the velocity arc (derivative of stride).
            // 3. Dynamic squash & stretch: contact and down poses compress while passing/push-off lifts.
            // 4. Expressive secondary action: head counter-balances, elbows keep soft tension,
            //    feet articulate naturally through push-off and strike.
            var t = globalTime + _timeOffset;
            ResetLimbPose();
            if (Hip != null) Hip.localPosition = _hipBasePos;
            _poseWidthScale = 1f;
            _poseHeightScale = 1f;
            if (CurrentAnimation != NpcAnimationClip.Sleep)
            {
                Root.localRotation = Quaternion.identity;
                if (StatusPlateRenderer != null) StatusPlateRenderer.transform.localRotation = Quaternion.identity;
            }

            if (CurrentAnimation == NpcAnimationClip.Walk)
            {
                // Stride phase at 8 rad/s (~1.27 Hz stride frequency).
                var phase = t * 8f;
                var stride = Mathf.Sin(phase);
                var strideVel = Mathf.Cos(phase); // Instantaneous swing velocity for overlapping drag
                var lift = Mathf.Abs(Mathf.Cos(phase));

                // Dynamic Line of Action & Whole-body bounce:
                // Contact / down compresses slightly while passing pose lifts with an energetic forward lean.
                SetWholeBodyPose(1f, 1f + lift * 0.018f, stride * 2.2f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, stride * 3.5f);
                // Head counter-rotation with slight lag: keeps horizon steady like an animated hero
                Head.localRotation = Quaternion.Euler(0f, 0f, -stride * 1.5f - strideVel * 0.5f);
                if (Hip != null) Hip.localPosition = _hipBasePos + new Vector3(0f, lift * 0.008f, 0f);

                // Arm swing with Alan Becker whip/drag:
                // Shoulder drives swing (-20 to +20 deg).
                var shoulderL = stride * 20f;
                var shoulderR = -stride * 20f;
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, shoulderL);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, shoulderR);

                // Forearm drag: flexes more when swinging backward, relaxes forward, with velocity drag
                // Note: at stride = 1 (phase = pi/2), strideVel = 0, exactly preserving test values.
                var elbowL = 4f + Mathf.Max(0f, -stride) * 18f + Mathf.Max(0f, -strideVel) * 4f;
                var elbowR = 4f + Mathf.Max(0f, stride) * 18f + Mathf.Max(0f, strideVel) * 4f;
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, elbowL);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, elbowR);

                // Hand follow-through: counter-rotates and trails the arm motion for elastic whip
                SetBoneRotation(NpcRigDefinition.BoneHandL, -(shoulderL + elbowL) * 0.3f + strideVel * 4f);
                SetBoneRotation(NpcRigDefinition.BoneHandR, -(shoulderR + elbowR) * 0.3f - strideVel * 4f);

                // Leg kinematics:
                // Thigh drives stride, knee flexes sharply on swing-through/push-off to clear floor.
                var thighL = -stride * 17f;
                var thighR = stride * 17f;
                var shinL = -(Mathf.Max(0f, stride) * 26f + Mathf.Max(0f, -stride) * 10f);
                var shinR = -(Mathf.Max(0f, -stride) * 26f + Mathf.Max(0f, stride) * 10f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, thighL);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, thighR);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, shinL);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, shinR);

                // Feet counter-pitch against thigh+shin so soles roll smoothly from heel-strike to toe-off
                SetBoneRotation(NpcRigDefinition.BoneFootL, -(thighL + shinL) * 0.5f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, -(thighR + shinR) * 0.5f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Sleep)
            {
                // Lie flat: whole-body rotation about feet with relaxed fetal curl.
                // Chest gently expands and contracts in deep, restful sleep cadence.
                var dir = SleepDirection >= 0 ? 1f : -1f;
                var sleepBreathe = Mathf.Sin(t * 1.4f);
                SetWholeBodyPose(1f, 1f + sleepBreathe * 0.008f, -75f * dir);
                Head.localRotation = Quaternion.Euler(0f, 0f, (8f + sleepBreathe * 1.2f) * dir);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, 8f + sleepBreathe * 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, 8f + sleepBreathe * 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 14f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 14f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, 6f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, 6f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -10f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -10f);
                if (StatusPlateRenderer != null)
                {
                    StatusPlateRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, 75f * dir);
                }
            }
            else if (CurrentAnimation == NpcAnimationClip.Sit)
            {
                // Seated fold: thighs forward to horizontal (78 deg), shins vertical (-72 deg),
                // feet level (-6 deg, sum = 0). Body squashes into chair with forward torso poise.
                // Soft upper-body breathing adds subtle vitality without disturbing the seat fold.
                var sitBreathe = Mathf.Sin(t * 1.9f);
                SetWholeBodyPose(1.025f, 0.90f + sitBreathe * 0.003f, -4.5f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, -8f + sitBreathe * 0.8f);
                Head.localRotation = Quaternion.Euler(0f, 0f, 4f - sitBreathe * 0.5f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, 78f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, 78f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -72f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -72f);
                SetBoneRotation(NpcRigDefinition.BoneFootL, -6f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, -6f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, 12f + sitBreathe * 1.2f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, 12f + sitBreathe * 1.2f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 18f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 18f);
                SetBoneRotation(NpcRigDefinition.BoneHandL, -4f);
                SetBoneRotation(NpcRigDefinition.BoneHandR, -4f);
            }
            else if (CurrentAnimation == NpcAnimationClip.QueueWait)
            {
                // Alan Becker impatient wait:
                // Weight shifts rhythmically from hip to hip with cocked pelvis, counter-angled spine,
                // and restless foot tap / arm fidgeting showing vivid personality.
                var shiftPhase = t * 1.7f;
                var shift = Mathf.Sin(shiftPhase) * 2.5f;
                var shiftDip = -Mathf.Abs(Mathf.Cos(shiftPhase)) * 0.002f;
                var tapPhase = t * 5.5f;
                var footTap = Mathf.Max(0f, Mathf.Sin(tapPhase)) * 6.5f;

                SetWholeBodyPose(1f, 1f - Mathf.Abs(Mathf.Cos(shiftPhase)) * 0.008f, shift);
                Spine.localRotation = Quaternion.Euler(0f, 0f, shift);
                Head.localRotation = Quaternion.Euler(0f, 0f, -shift * 0.6f + Mathf.Sin(t * 0.9f) * 2.5f);
                if (Hip != null)
                {
                    Hip.localPosition = _hipBasePos + new Vector3(
                        Mathf.Sin(shiftPhase) * 0.008f,
                        shiftDip, 0f);
                }
                // Arms held in impatient wait posture, slightly flexing as balance shifts
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, shift * 2f + 6f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, -shift * 2f + 6f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 16f + Mathf.Sin(shiftPhase) * 4f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 16f - Mathf.Sin(shiftPhase) * 4f);
                SetBoneRotation(NpcRigDefinition.BoneHandL, 6f);
                SetBoneRotation(NpcRigDefinition.BoneHandR, 6f);
                // Restless front foot tap
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -footTap * 0.25f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, footTap);
            }
            else if (CurrentAnimation == NpcAnimationClip.Ride)
            {
                // Braced elevator transit stance:
                // Knees softly flexed to absorb shaft vibration, hands stabilizing balance,
                // subtle micro-squash reacting to vertical lift momentum.
                var rideWave = Mathf.Sin(t * 2.5f);
                var microSquash = Mathf.Sin(t * 5f) * 0.002f;
                SetWholeBodyPose(1f, 1f + rideWave * 0.004f + microSquash, rideWave * 0.7f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, rideWave * 0.7f);
                Head.localRotation = Quaternion.Euler(0f, 0f, -rideWave * 0.3f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, 3f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, 3f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -6f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -6f);
                SetBoneRotation(NpcRigDefinition.BoneFootL, 3f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, 3f);
                // Arms raised slightly in transit balance
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, 4f + rideWave * 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, 4f - rideWave * 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 14f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 14f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Run)
            {
                // Alan Becker athletic sprint / run cycle:
                // High-cadence stride (13.5 rad/s) with pronounced forward line of action,
                // vertical flight oscillation, aggressive elbow pumping, and high rear-heel kick.
                var phase = t * 13.5f;
                var stride = Mathf.Sin(phase);
                var strideVel = Mathf.Cos(phase);
                var lift = Mathf.Abs(Mathf.Cos(phase));

                var bodyLean = 8f + stride * 3.5f;
                var runLift = lift * 0.024f;
                SetWholeBodyPose(1f - lift * 0.01f, 1f + lift * 0.025f, bodyLean);
                Spine.localRotation = Quaternion.Euler(0f, 0f, 6f + stride * 5f);
                Head.localRotation = Quaternion.Euler(0f, 0f, -4f - stride * 2.5f - strideVel * 1f);
                if (Hip != null) Hip.localPosition = _hipBasePos + new Vector3(0f, runLift, 0f);

                // Aggressive 90-degree arm pump with whip drag
                var runShoulderL = stride * 42f;
                var runShoulderR = -stride * 42f;
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, runShoulderL);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, runShoulderR);
                var runElbowL = 68f + Mathf.Max(0f, -stride) * 22f - strideVel * 8f;
                var runElbowR = 68f + Mathf.Max(0f, stride) * 22f + strideVel * 8f;
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, runElbowL);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, runElbowR);
                SetBoneRotation(NpcRigDefinition.BoneHandL, -12f + strideVel * 10f);
                SetBoneRotation(NpcRigDefinition.BoneHandR, -12f - strideVel * 10f);

                // High knee drive and heel kick to glute
                var runThighL = -stride * 36f;
                var runThighR = stride * 36f;
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, runThighL);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, runThighR);
                var runShinL = -(Mathf.Max(0f, stride) * 55f + Mathf.Max(0f, -stride) * 14f);
                var runShinR = -(Mathf.Max(0f, -stride) * 55f + Mathf.Max(0f, stride) * 14f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, runShinL);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, runShinR);
                SetBoneRotation(NpcRigDefinition.BoneFootL, -(runThighL + runShinL) * 0.45f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, -(runThighR + runShinR) * 0.45f);
            }
            else if (CurrentAnimation == NpcAnimationClip.WorkDesk)
            {
                // Seated office / desk work posture:
                // Attentive forward lean, hands elevated to desk surface with rapid
                // keyboard typing bursts and head focus on monitors.
                var workBreathe = Mathf.Sin(t * 2.2f);
                SetWholeBodyPose(1.025f, 0.90f + workBreathe * 0.003f, -5.5f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, -11f + workBreathe * 0.6f);
                Head.localRotation = Quaternion.Euler(0f, 0f, 9f - workBreathe * 0.4f);

                // Level seated legs matching office chairs
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, 78f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, 78f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -72f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -72f);
                SetBoneRotation(NpcRigDefinition.BoneFootL, -6f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, -6f);

                // Forearms raised to keyboard height
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, 34f + workBreathe * 1f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, 34f + workBreathe * 1f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 52f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 52f);

                // Rhythmic typing flutter
                var burst = Mathf.Sin(t * 2.8f) > -0.25f ? 1f : 0.15f;
                var typeL = Mathf.Sin(t * 22f) * 12f * burst;
                var typeR = Mathf.Sin(t * 22f + 1.6f) * 12f * burst;
                SetBoneRotation(NpcRigDefinition.BoneHandL, -14f + typeL);
                SetBoneRotation(NpcRigDefinition.BoneHandR, -14f + typeR);
            }
            else if (CurrentAnimation == NpcAnimationClip.Crawl)
            {
                // Low-profile duct / undercity excavation inspection crawl:
                // Pelvis lowered to ground, spine arched horizontal, head raised forward,
                // and alternating hands/knees crawling gait.
                if (Hip != null) Hip.localPosition = _hipBasePos + new Vector3(0f, -0.13f, 0f);
                var crawlPhase = t * 5f;
                var crawlStride = Mathf.Sin(crawlPhase);
                var crawlBounce = Mathf.Abs(Mathf.Cos(crawlPhase));
                SetWholeBodyPose(1.08f, 0.70f + crawlBounce * 0.02f, -18f + crawlStride * 3f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, -22f + crawlStride * 4f);
                Head.localRotation = Quaternion.Euler(0f, 0f, 32f - crawlStride * 2f);

                var crawlArmL = crawlStride * 28f + 18f;
                var crawlArmR = -crawlStride * 28f + 18f;
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, crawlArmL);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, crawlArmR);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 45f + Mathf.Max(0f, -crawlStride) * 25f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 45f + Mathf.Max(0f, crawlStride) * 25f);
                SetBoneRotation(NpcRigDefinition.BoneHandL, -30f);
                SetBoneRotation(NpcRigDefinition.BoneHandR, -30f);

                var crawlThighL = -crawlStride * 32f + 55f;
                var crawlThighR = crawlStride * 32f + 55f;
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, crawlThighL);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, crawlThighR);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -80f + Mathf.Max(0f, crawlStride) * 25f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -80f + Mathf.Max(0f, -crawlStride) * 25f);
                SetBoneRotation(NpcRigDefinition.BoneFootL, 25f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, 25f);
            }
            else
            {
                // Alan Becker alert idle:
                // Organic breathing cycle with natural ease-in expansion, chest rise,
                // micro-nod of the head, unlocked ready elbows, and subtle shoulder poise.
                // When t == 0, breathe == 0 so hip restores exactly to bind height with zero offset.
                var breathe = Mathf.Sin(t * 2.1f);
                var breatheAngle = breathe * 1.2f;
                SetWholeBodyPose(1f, 1f + breathe * 0.002f, breatheAngle * 0.5f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, breatheAngle);
                Head.localRotation = Quaternion.Euler(0f, 0f, -breatheAngle * 0.7f);
                if (Hip != null) Hip.localPosition = _hipBasePos + new Vector3(0f, breathe * 0.0015f, 0f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, breathe * 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, -breathe * 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 6f + Mathf.Max(0f, breathe) * 2f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 6f + Mathf.Max(0f, breathe) * 2f);
                SetBoneRotation(NpcRigDefinition.BoneHandL, -2f);
                SetBoneRotation(NpcRigDefinition.BoneHandR, -2f);
                // Unlocked soft knees for active stick figure balance stance
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -1.5f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -1.5f);
                SetBoneRotation(NpcRigDefinition.BoneFootL, 1.5f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, 1.5f);
            }

            // Central presenter owns animation and density; the view has no Update loop.
            RefreshBubblePose();
            if (CurrentEmote != NpcEmoteKind.None && EmoteRenderer != null && _bubbleVisible)
            {
                var frame = (int)((globalTime / 0.15f) % 3);
                var sprite = EmoteSpriteCatalog.GetSprite(CurrentEmote, frame);
                if (sprite != null) EmoteRenderer.sprite = sprite;
            }
            SetBubbleVisible(_bubbleVisible);
        }

        private void RepointWardrobeSlots()
        {
            // Scene-authored residents were built when every slot hung off the spine.
            // Re-seat each slot on its spec bone so garments deform with the right joint.
            foreach (var layer in NpcRigDefinition.LayerRenderingOrder)
            {
                if (!_wardrobeSlots.TryGetValue(layer, out var renderer) || renderer == null) continue;
                var parentBone = NpcRigDefinition.GetParentBone(layer);
                if (parentBone != null && _bones.TryGetValue(parentBone, out var bone) && bone != null)
                {
                    renderer.transform.SetParent(bone, false);
                }
            }
        }

        private Transform FindAuthoredSlot(NpcLayerKind layer)
        {
            var slotName = $"Wardrobe_{layer}";
            var direct = transform.Find(slotName);
            if (direct != null) return direct;
            // Scene-authored slots may already hang off a bone from an earlier pass.
            foreach (var pair in _bones)
            {
                var boneTransform = pair.Value;
                if (boneTransform == null || boneTransform == transform) continue;
                var nested = boneTransform.Find(slotName);
                if (nested != null) return nested;
            }
            return null;
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
            if (_torsoSprite == null) _torsoSprite = CreateTorsoPartSprite(48, 64);
            if (_headSprite == null) _headSprite = CreateCirclePartSprite(48);
            if (_limbSprite == null) _limbSprite = CreateCapsulePartSprite(24, 72);
            if (_handSprite == null) _handSprite = CreateCirclePartSprite(24);
            if (_footSprite == null) _footSprite = CreateCapsulePartSprite(20, 48);

            // 1. Torso & Head
            EnsureLimbRenderer("torso", Spine, _torsoSprite, new Vector3(0f, -0.04f, 0.02f), new Vector3(0.18f, 0.22f, 1f), 13, new Color(0.78f, 0.62f, 0.48f));
            EnsureLimbRenderer("head", Head, _headSprite, new Vector3(0f, 0.035f, 0.01f), new Vector3(0.14f, 0.14f, 1f), 15, new Color(0.96f, 0.76f, 0.61f));

            // 2. Arms (Upper Arm -> Lower Arm -> Hand)
            // Rear Arm (sorting order 11, behind torso)
            EnsureLimbRenderer(NpcRigDefinition.BoneArmUpperL, _bones[NpcRigDefinition.BoneArmUpperL], _limbSprite, Vector3.zero, new Vector3(0.09f, 0.11f, 1f), 11, new Color(0.85f, 0.68f, 0.54f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmLowerL, _bones[NpcRigDefinition.BoneArmLowerL], _limbSprite, Vector3.zero, new Vector3(0.08f, 0.09f, 1f), 11, new Color(0.85f, 0.68f, 0.54f));
            EnsureLimbRenderer(NpcRigDefinition.BoneHandL, _bones[NpcRigDefinition.BoneHandL], _handSprite, new Vector3(0f, -0.012f, 0.01f), new Vector3(0.045f, 0.045f, 1f), 11, new Color(0.85f, 0.68f, 0.54f));

            // Front Arm (sorting order 16, in front of torso)
            EnsureLimbRenderer(NpcRigDefinition.BoneArmUpperR, _bones[NpcRigDefinition.BoneArmUpperR], _limbSprite, Vector3.zero, new Vector3(0.09f, 0.11f, 1f), 16, new Color(0.96f, 0.76f, 0.61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmLowerR, _bones[NpcRigDefinition.BoneArmLowerR], _limbSprite, Vector3.zero, new Vector3(0.08f, 0.09f, 1f), 16, new Color(0.96f, 0.76f, 0.61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneHandR, _bones[NpcRigDefinition.BoneHandR], _handSprite, new Vector3(0f, -0.012f, -0.01f), new Vector3(0.045f, 0.045f, 1f), 16, new Color(0.96f, 0.76f, 0.61f));

            // 3. Legs (Hip -> Upper Leg -> Lower Leg -> Foot)
            // Rear Leg (sorting order 11)
            EnsureLimbRenderer(NpcRigDefinition.BoneLegUpperL, _bones[NpcRigDefinition.BoneLegUpperL], _limbSprite, Vector3.zero, new Vector3(0.10f, 0.13f, 1f), 11, new Color(0.30f, 0.35f, 0.42f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegLowerL, _bones[NpcRigDefinition.BoneLegLowerL], _limbSprite, Vector3.zero, new Vector3(0.09f, 0.11f, 1f), 11, new Color(0.30f, 0.35f, 0.42f));
            EnsureLimbRenderer(NpcRigDefinition.BoneFootL, _bones[NpcRigDefinition.BoneFootL], _footSprite, new Vector3(0.012f, -0.012f, 0.01f), new Vector3(0.08f, 0.04f, 1f), 11, new Color(0.14f, 0.17f, 0.22f));

            // Front Leg (sorting order 14)
            EnsureLimbRenderer(NpcRigDefinition.BoneLegUpperR, _bones[NpcRigDefinition.BoneLegUpperR], _limbSprite, Vector3.zero, new Vector3(0.10f, 0.13f, 1f), 14, new Color(0.34f, 0.39f, 0.47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegLowerR, _bones[NpcRigDefinition.BoneLegLowerR], _limbSprite, Vector3.zero, new Vector3(0.09f, 0.11f, 1f), 14, new Color(0.34f, 0.39f, 0.47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneFootR, _bones[NpcRigDefinition.BoneFootR], _footSprite, new Vector3(0.012f, -0.012f, -0.01f), new Vector3(0.08f, 0.04f, 1f), 14, new Color(0.16f, 0.19f, 0.24f));
        }

        private static Sprite GetOrCreateSprite(ref Sprite sprite, int width, int height)
        {
            if (sprite == null)
            {
                sprite = CreateCapsulePartSprite(width, height);
            }

            return sprite;
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
            if (_sharedMaterial != null) renderer.sharedMaterial = _sharedMaterial;
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

        private static Sprite CreateCapsulePartSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"NpcCapsulePart_{width}x{height}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            var radius = (width - 1) * 0.5f;
            var topCenterY = height - 1 - radius;
            var bottomCenterY = radius;
            var centerX = radius;

            for (var y = 0; y < height; y++)
            {
                var cy = y > topCenterY ? topCenterY : (y < bottomCenterY ? bottomCenterY : y);
                var dy = y - cy;
                for (var x = 0; x < width; x++)
                {
                    var dx = x - centerX;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            var pivot = new Vector2(0.5f, topCenterY / (float)height);
            var effectiveLength = Mathf.Max(1f, height - width);
            return Sprite.Create(texture, new Rect(0, 0, width, height), pivot, effectiveLength);
        }

        private static Sprite CreateCirclePartSprite(int diameter)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                name = $"NpcCirclePart_{diameter}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[diameter * diameter];
            var radius = (diameter - 1) * 0.5f;
            for (var y = 0; y < diameter; y++)
            {
                var dy = y - radius;
                for (var x = 0; x < diameter; x++)
                {
                    var dx = x - radius;
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    pixels[y * diameter + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), diameter);
        }

        private static Sprite CreateTorsoPartSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"NpcTorsoPart_{width}x{height}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            var centerX = (width - 1) * 0.5f;
            for (var y = 0; y < height; y++)
            {
                var t = y / (float)height;
                var halfW = Mathf.Lerp(width * 0.32f, width * 0.48f, t);
                for (var x = 0; x < width; x++)
                {
                    var dx = Mathf.Abs(x - centerX);
                    var alpha = Mathf.Clamp01(halfW + 0.5f - dx);
                    if (y > height - 6) alpha *= Mathf.Clamp01((height - y) / 5f);
                    if (y < 4) alpha *= Mathf.Clamp01(y / 4f);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
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
