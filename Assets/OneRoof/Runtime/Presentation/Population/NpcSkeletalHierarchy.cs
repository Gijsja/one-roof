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

            // Rotation convention (front view, facing +x): +z swings a hanging limb forward.
            // Knees flex backward (heel lift, negative) and elbows flex forward (positive);
            // joints keep a soft constant bend so limbs never lock poker-straight.
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
                // Two-bone gait: contralateral arm/leg swing, knee fold on push-off and
                // swing-through, partial foot flattening, pelvis bob at 2x stride frequency.
                var phase = t * 8f;
                var stride = Mathf.Sin(phase);
                var lift = Mathf.Abs(Mathf.Cos(phase));
                // A restrained whole-body sway and bounce reinforces the
                // joint motion without overpowering the small silhouette.
                SetWholeBodyPose(1f, 1f + lift * 0.018f, stride * 2.2f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, stride * 3.5f);
                Head.localRotation = Quaternion.Euler(0f, 0f, -stride * 1.5f);
                if (Hip != null) Hip.localPosition = _hipBasePos + new Vector3(0f, lift * 0.008f, 0f);

                var shoulderL = stride * 20f;
                var shoulderR = -stride * 20f;
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, shoulderL);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, shoulderR);
                var elbowL = 4f + Mathf.Max(0f, -stride) * 18f;
                var elbowR = 4f + Mathf.Max(0f, stride) * 18f;
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, elbowL);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, elbowR);
                SetBoneRotation(NpcRigDefinition.BoneHandL, -(shoulderL + elbowL) * 0.3f);
                SetBoneRotation(NpcRigDefinition.BoneHandR, -(shoulderR + elbowR) * 0.3f);

                var thighL = -stride * 17f;
                var thighR = stride * 17f;
                var shinL = -(Mathf.Max(0f, stride) * 26f + Mathf.Max(0f, -stride) * 10f);
                var shinR = -(Mathf.Max(0f, -stride) * 26f + Mathf.Max(0f, stride) * 10f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, thighL);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, thighR);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, shinL);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, shinR);
                SetBoneRotation(NpcRigDefinition.BoneFootL, -(thighL + shinL) * 0.5f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, -(thighR + shinR) * 0.5f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Sleep)
            {
                // Lie flat: rotate the whole body about the feet so the head
                // rests toward the facing direction instead of tipping the
                // spine rigidly sideways mid-air. The underfoot plate is
                // counter-rotated so it stays a floor shadow. Limbs relax into
                // a slight fetal fold so the body never reads as a stiff plank.
                var dir = SleepDirection >= 0 ? 1f : -1f;
                SetWholeBodyPose(1f, 1f, -75f * dir);
                Head.localRotation = Quaternion.Euler(0f, 0f, 8f * dir);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, 8f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, 8f);
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
                // A compact squash and forward lean makes the articulated
                // pose read as seated at gameplay scale.
                SetWholeBodyPose(1.025f, 0.90f, -4.5f);
                // Seated fold: thighs forward to horizontal, shins drop to vertical,
                // feet flatten (78 - 72 - 6 = 0), hands rest forward onto the lap.
                Spine.localRotation = Quaternion.Euler(0f, 0f, -8f);
                Head.localRotation = Quaternion.Euler(0f, 0f, 4f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperL, 78f);
                SetBoneRotation(NpcRigDefinition.BoneLegUpperR, 78f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -72f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -72f);
                SetBoneRotation(NpcRigDefinition.BoneFootL, -6f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, -6f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, 12f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, 12f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 18f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 18f);
            }
            else if (CurrentAnimation == NpcAnimationClip.QueueWait)
            {
                // Impatient weight shift: pelvis sways foot-to-foot with a small transfer
                // dip mid-shift while the chest counter-rolls above it.
                var shiftPhase = t * 1.7f;
                var shift = Mathf.Sin(shiftPhase) * 2.5f;
                SetWholeBodyPose(1f, 1f - Mathf.Abs(Mathf.Cos(shiftPhase)) * 0.008f, shift);
                Spine.localRotation = Quaternion.Euler(0f, 0f, shift);
                Head.localRotation = Quaternion.Euler(0f, 0f, -shift * .5f);
                if (Hip != null)
                {
                    Hip.localPosition = _hipBasePos + new Vector3(
                        Mathf.Sin(shiftPhase) * 0.008f,
                        -Mathf.Abs(Mathf.Cos(shiftPhase)) * 0.002f, 0f);
                }
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, shift * 2f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, -shift * 2f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 5f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 5f);
            }
            else if (CurrentAnimation == NpcAnimationClip.Ride)
            {
                // Braced stance: soft knees, hands half-raised as if on the car rail.
                SetWholeBodyPose(1f, 1f + Mathf.Sin(t * 2.5f) * 0.004f, Mathf.Sin(t * 2.5f) * 0.7f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.5f) * .7f);
                Head.localRotation = Quaternion.identity;
                SetBoneRotation(NpcRigDefinition.BoneLegLowerL, -6f);
                SetBoneRotation(NpcRigDefinition.BoneLegLowerR, -6f);
                SetBoneRotation(NpcRigDefinition.BoneFootL, 3f);
                SetBoneRotation(NpcRigDefinition.BoneFootR, 3f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 8f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 8f);
            }
            else
            {
                // Idle breathing: chest rise, head counter-nod, unlocked elbows,
                // faint shoulder drift so the silhouette never freezes.
                var breathe = Mathf.Sin(t * 2.1f);
                var breatheAngle = breathe * 1.2f;
                SetWholeBodyPose(1f, 1f + breathe * 0.002f, breatheAngle * 0.5f);
                Spine.localRotation = Quaternion.Euler(0f, 0f, breatheAngle);
                Head.localRotation = Quaternion.Euler(0f, 0f, -breatheAngle * 0.7f);
                if (Hip != null) Hip.localPosition = _hipBasePos + new Vector3(0f, breathe * 0.0015f, 0f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperL, breathe * 1f);
                SetBoneRotation(NpcRigDefinition.BoneArmUpperR, -breathe * 1f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerL, 4f);
                SetBoneRotation(NpcRigDefinition.BoneArmLowerR, 4f);
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
            // Procedural fallback body draws strictly under the wardrobe slots (16+), so a
            // palette swatch or photo part always covers the box beneath its own layer.
            EnsureLimbRenderer("torso", Spine, GetOrCreateSprite(ref _torsoSprite, 10, 14), new Vector3(0f, -.065f, .02f), new Vector3(.18f, .25f, 1f), 13, new Color(.78f, .62f, .48f));
            EnsureLimbRenderer("head", Head, GetOrCreateSprite(ref _headSprite, 10, 10), new Vector3(0f, .025f, .01f), new Vector3(.14f, .14f, 1f), 14, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmUpperL, _bones[NpcRigDefinition.BoneArmUpperL], GetOrCreateSprite(ref _limbSprite, 4, 12), new Vector3(-.018f, -.055f, .03f), new Vector3(.052f, .12f, 1f), 12, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmUpperR, _bones[NpcRigDefinition.BoneArmUpperR], _limbSprite, new Vector3(.018f, -.055f, .03f), new Vector3(.052f, .12f, 1f), 12, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmLowerL, _bones[NpcRigDefinition.BoneArmLowerL], _limbSprite, new Vector3(0f, -.055f, .03f), new Vector3(.042f, .11f, 1f), 12, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneArmLowerR, _bones[NpcRigDefinition.BoneArmLowerR], _limbSprite, new Vector3(0f, -.055f, .03f), new Vector3(.042f, .11f, 1f), 12, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegUpperL, _bones[NpcRigDefinition.BoneLegUpperL], _limbSprite, new Vector3(0f, -.075f, .03f), new Vector3(.065f, .17f, 1f), 12, new Color(.34f, .39f, .47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegUpperR, _bones[NpcRigDefinition.BoneLegUpperR], _limbSprite, new Vector3(0f, -.075f, .03f), new Vector3(.065f, .17f, 1f), 12, new Color(.34f, .39f, .47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegLowerL, _bones[NpcRigDefinition.BoneLegLowerL], _limbSprite, new Vector3(0f, -.065f, .03f), new Vector3(.052f, .13f, 1f), 12, new Color(.34f, .39f, .47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneLegLowerR, _bones[NpcRigDefinition.BoneLegLowerR], _limbSprite, new Vector3(0f, -.065f, .03f), new Vector3(.052f, .13f, 1f), 12, new Color(.34f, .39f, .47f));
            EnsureLimbRenderer(NpcRigDefinition.BoneHandL, _bones[NpcRigDefinition.BoneHandL], GetOrCreateSprite(ref _handSprite, 5, 5), new Vector3(0f, -.018f, .03f), new Vector3(.045f, .045f, 1f), 12, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneHandR, _bones[NpcRigDefinition.BoneHandR], _handSprite, new Vector3(0f, -.018f, .03f), new Vector3(.045f, .045f, 1f), 12, new Color(.96f, .76f, .61f));
            EnsureLimbRenderer(NpcRigDefinition.BoneFootL, _bones[NpcRigDefinition.BoneFootL], GetOrCreateSprite(ref _footSprite, 8, 3), new Vector3(-.012f, -.015f, .03f), new Vector3(.08f, .034f, 1f), 12, new Color(.16f, .19f, .24f));
            EnsureLimbRenderer(NpcRigDefinition.BoneFootR, _bones[NpcRigDefinition.BoneFootR], _footSprite, new Vector3(.012f, -.015f, .03f), new Vector3(.08f, .034f, 1f), 12, new Color(.16f, .19f, .24f));
        }

        private static Sprite GetOrCreateSprite(ref Sprite sprite, int width, int height)
        {
            if (sprite == null)
            {
                sprite = CreatePixelPartSprite(width, height);
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

        private static Sprite CreatePixelPartSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"NpcPrototypePart_{width}x{height}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            var radiusX = Mathf.Min(0.45f, (height * 0.42f) / width);
            var radiusY = Mathf.Min(0.45f, (width * 0.42f) / height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = Mathf.Abs((x + 0.5f - width * 0.5f) / (width * 0.5f));
                    var ny = Mathf.Abs((y + 0.5f - height * 0.5f) / (height * 0.5f));
                    var cornerX = Mathf.Max(0f, nx - (1f - radiusX)) / radiusX;
                    var cornerY = Mathf.Max(0f, ny - (1f - radiusY)) / radiusY;
                    var inside = cornerX * cornerX + cornerY * cornerY <= 1f;
                    pixels[y * width + x] = inside ? Color.white : Color.clear;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, 1f), height);
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
