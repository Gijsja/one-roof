using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Crisis
{
    /// <summary>
    /// Articulated combat poses and 2D ragdoll controller on the same wardrobe and bone rig used by Tower residents.
    /// Manages connected joint segments (upper arm, lower arm, hand; hip, upper leg, lower leg, foot; spine, head).
    /// </summary>
    public sealed class CaretakerCombatRig
    {
        private readonly NpcSkeletalHierarchy _rig;
        private readonly Transform _root;
        private readonly SpriteRenderer[] _renderers;
        private readonly Color[] _colors;
        private readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();
        private Material _material;
        private readonly bool _isAllIn1;

        // AllIn1 shader property IDs (cached statically to eliminate string hashing overhead)
        private static readonly int HitEffectColorId = Shader.PropertyToID("_HitEffectColor");
        private static readonly int HitEffectBlendId = Shader.PropertyToID("_HitEffectBlend");
        private static readonly int HitEffectGlowId = Shader.PropertyToID("_HitEffectGlow");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowId = Shader.PropertyToID("_Glow");
        private static readonly int GlowGlobalId = Shader.PropertyToID("_GlowGlobal");
        private static readonly int InnerOutlineColorId = Shader.PropertyToID("_InnerOutlineColor");
        private static readonly int InnerOutlineThicknessId = Shader.PropertyToID("_InnerOutlineThickness");
        private static readonly int InnerOutlineAlphaId = Shader.PropertyToID("_InnerOutlineAlpha");
        private static readonly int InnerOutlineGlowId = Shader.PropertyToID("_InnerOutlineGlow");
        private static readonly int GhostBlendId = Shader.PropertyToID("_GhostBlend");
        private static readonly int GhostColorBoostId = Shader.PropertyToID("_GhostColorBoost");
        private static readonly int GhostTransparencyId = Shader.PropertyToID("_GhostTransparency");

        private static Material s_sharedCombatTemplate;

        // Front limb segments
        private readonly Transform _upperArm, _forearm, _hand;
        private readonly Transform _thigh, _shin, _foot;

        // Rear limb segments
        private readonly Transform _rearArm, _rearForearm, _rearHand;
        private readonly Transform _rearThigh, _rearShin, _rearFoot;

        // Core segments
        private readonly Transform _hip, _spine, _neck, _head;

        private readonly CombatRagdoll2D _ragdoll;
        private bool _wasDefeated;
        private float _hitFlashTimer;
        private Color _activeHitColor = Color.white;

        public Transform Transform => _root;
        public GameObject GameObject => _root.gameObject;
        public Material Material => _material;

        public static Material GetOrCreateDefaultCombatMaterial()
        {
            if (s_sharedCombatTemplate != null) return s_sharedCombatTemplate;
            var shader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShaderSRPBatch")
                ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                s_sharedCombatTemplate = new Material(shader) { name = "Caretaker_CombatSharedTemplate", enableInstancing = true };
                if (s_sharedCombatTemplate.shader != null && s_sharedCombatTemplate.shader.name.Contains("AllIn1"))
                {
                    s_sharedCombatTemplate.EnableKeyword("HITEFFECT_ON");
                    s_sharedCombatTemplate.EnableKeyword("GLOW_ON");
                    s_sharedCombatTemplate.EnableKeyword("INNEROUTLINE_ON");
                    s_sharedCombatTemplate.EnableKeyword("GHOST_ON");
                }
            }
            return s_sharedCombatTemplate;
        }

        public CaretakerCombatRig(Transform parent, string name, int wardrobeIndex, Material material)
        {
            var go = new GameObject(name);
            _root = go.transform;
            _root.SetParent(parent, false);

            var baseMat = material != null ? material : GetOrCreateDefaultCombatMaterial();
            _isAllIn1 = baseMat != null && baseMat.shader != null && baseMat.shader.name.Contains("AllIn1");
            _material = baseMat != null ? new Material(baseMat) { name = name + "_CombatMat" } : null;

            if (_material != null && _isAllIn1)
            {
                _material.EnableKeyword("HITEFFECT_ON");
                _material.EnableKeyword("GLOW_ON");
                _material.EnableKeyword("INNEROUTLINE_ON");
                _material.EnableKeyword("GHOST_ON");
                _material.SetFloat(HitEffectBlendId, 0f);
                _material.SetFloat(GlowId, 0f);
                _material.SetFloat(GlowGlobalId, 1f);
                _material.SetFloat(InnerOutlineAlphaId, 0f);
                _material.SetFloat(GhostBlendId, 0f);
            }

            _rig = go.AddComponent<NpcSkeletalHierarchy>();
            _rig.Initialize(wardrobeIndex, _material ?? NpcSkeletalHierarchy.GetOrCreateDefaultSharedMaterial());
            _rig.SetBubbleVisible(false);

            // Combat feedback is centrally pooled; keep the resident construction effect loop dormant.
            if (_rig.VisualEffects != null) _rig.VisualEffects.enabled = false;
            _rig.StatusPlateRenderer.enabled = false;

            _renderers = go.GetComponentsInChildren<SpriteRenderer>();
            _colors = new Color[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
            {
                _colors[i] = _renderers[i].color;
                _renderers[i].sortingOrder += 350;
            }

            // Bind front limb segments
            _upperArm = Bone(NpcRigDefinition.BoneArmUpperR);
            _forearm = Bone(NpcRigDefinition.BoneArmLowerR);
            _hand = Bone(NpcRigDefinition.BoneHandR);
            _thigh = Bone(NpcRigDefinition.BoneLegUpperR);
            _shin = Bone(NpcRigDefinition.BoneLegLowerR);
            _foot = Bone(NpcRigDefinition.BoneFootR);

            // Bind rear limb segments
            _rearArm = Bone(NpcRigDefinition.BoneArmUpperL);
            _rearForearm = Bone(NpcRigDefinition.BoneArmLowerL);
            _rearHand = Bone(NpcRigDefinition.BoneHandL);
            _rearThigh = Bone(NpcRigDefinition.BoneLegUpperL);
            _rearShin = Bone(NpcRigDefinition.BoneLegLowerL);
            _rearFoot = Bone(NpcRigDefinition.BoneFootL);

            // Bind core segments
            _hip = Bone(NpcRigDefinition.BoneHip);
            _spine = Bone(NpcRigDefinition.BoneSpine);
            _neck = Bone(NpcRigDefinition.BoneNeck);
            _head = Bone(NpcRigDefinition.BoneHead);

            // Initialize articulated 2D ragdoll controller
            _ragdoll = new CombatRagdoll2D(_rig.Bones);
        }

        private Transform Bone(string name) => _rig.Bones.TryGetValue(name, out var bone) ? bone : null;

        private static void Rotate(Transform bone, float degrees)
        {
            if (bone != null)
                bone.localRotation = Quaternion.Euler(0, 0, degrees);
        }

        public void Present(
            Vector3 position,
            int facing,
            string action,
            bool moving,
            float phase,
            bool hit,
            bool defeated,
            bool crouching = false,
            bool isBoss = false,
            int attackFrame = 0,
            bool isCritical = false,
            bool isDashing = false)
        {
            _root.position = position;

            // Trigger hit flash duration & color
            if (hit)
            {
                _hitFlashTimer = 0.16f;
                // Pure white for regular hits, intense crimson/gold for critical or boss hits
                _activeHitColor = (isCritical || isBoss)
                    ? new Color(1.5f, 0.28f, 0.14f, 1f) // intense crimson / gold
                    : Color.white;
            }

            _hitFlashTimer = Mathf.Max(0f, _hitFlashTimer - Time.unscaledDeltaTime);
            var hitBlend = _hitFlashTimer > 0f ? Mathf.Clamp01(_hitFlashTimer / 0.16f) : (hit ? 1f : 0f);

            // Handle Ragdoll Physics on Defeat
            if (defeated)
            {
                if (!_wasDefeated)
                {
                    _ragdoll.Activate(new Vector2(-facing * 2.8f, 3.2f), -facing * 22f, facing, position.y);
                    _wasDefeated = true;
                }

                _ragdoll.Step(Time.unscaledDeltaTime, position.y);
                _root.rotation = Quaternion.identity;

                if (_isAllIn1 && _material != null)
                {
                    _material.SetFloat(HitEffectBlendId, hitBlend);
                    _material.SetColor(HitEffectColorId, _activeHitColor);
                    _material.SetFloat(GlowId, 0f);
                    _material.SetFloat(InnerOutlineAlphaId, 0f);
                    _material.SetFloat(GhostBlendId, 0f);
                }
                else
                {
                    for (var i = 0; i < _renderers.Length; i++)
                        _renderers[i].color = hitBlend > 0.05f ? Color.Lerp(_colors[i], _activeHitColor, 0.8f * hitBlend) : _colors[i];
                }
                return;
            }

            if (_wasDefeated)
            {
                _ragdoll.Deactivate();
                _wasDefeated = false;
            }

            _root.rotation = Quaternion.identity;
            _rig.SetAnimationClip(moving ? NpcAnimationClip.Walk : NpcAnimationClip.Idle);
            _rig.SetFacing(facing >= 0 ? 1 : -1);
            _rig.ApplyProceduralAnimation(Time.unscaledTime);

            var duck = crouching || action == "Duck";
            var punch = action == "Punch";
            var kick = action == "Kick" || action == "JumpKick";
            var impact = Mathf.Sin(Mathf.Clamp01(phase) * Mathf.PI);

            _root.localScale = new Vector3((facing >= 0 ? 1 : -1) * 1.35f, duck ? 0.92f : 1.35f, 1);

            // Default Guard Poses for connected limbs
            Rotate(_rearArm, 42);
            Rotate(_rearForearm, 110);
            Rotate(_rearHand, 15);
            Rotate(_upperArm, 48);
            Rotate(_forearm, 95);
            Rotate(_hand, 10);
            Rotate(_foot, 5);
            Rotate(_rearFoot, 0);

            // Punch Action: Front arm drives forward into straight punch, rear arm pulls back to rib guard
            if (punch)
            {
                Rotate(_upperArm, Mathf.Lerp(48, 102, impact));
                Rotate(_forearm, Mathf.Lerp(95, -10, impact));
                Rotate(_hand, Mathf.Lerp(10, -5, impact));

                Rotate(_rearArm, Mathf.Lerp(42, 28, impact));
                Rotate(_rearForearm, Mathf.Lerp(110, 125, impact));
                Rotate(_rearHand, 20);

                Rotate(_spine, 8 * impact);
            }

            // Kick Action: Front leg delivers high side kick, rear leg braces with weight, arms counterbalance
            if (kick)
            {
                // Front leg
                Rotate(_thigh, Mathf.Lerp(12, 102, impact));
                Rotate(_shin, Mathf.Lerp(-38, -6, impact));
                Rotate(_foot, Mathf.Lerp(5, 32, impact));

                // Rear bracing leg
                Rotate(_rearThigh, Mathf.Lerp(-10, -28, impact));
                Rotate(_rearShin, Mathf.Lerp(15, 34, impact));
                Rotate(_rearFoot, Mathf.Lerp(0, -8, impact));

                // Torso & arms counterbalance
                Rotate(_spine, -16 * impact);
                Rotate(_upperArm, Mathf.Lerp(48, 25, impact));
                Rotate(_forearm, Mathf.Lerp(95, 115, impact));
                Rotate(_rearArm, Mathf.Lerp(42, 85, impact));
                Rotate(_rearForearm, Mathf.Lerp(110, 45, impact));
            }

            // Duck Action: Knees compress, torso tucks, both hands cover upper head
            if (duck)
            {
                if (!kick)
                {
                    Rotate(_thigh, 62);
                    Rotate(_shin, -90);
                    Rotate(_foot, 28);

                    Rotate(_rearThigh, 55);
                    Rotate(_rearShin, -85);
                    Rotate(_rearFoot, 24);
                }

                Rotate(_spine, -18);
                Rotate(_upperArm, 75);
                Rotate(_forearm, 105);
                Rotate(_hand, 20);

                Rotate(_rearArm, 65);
                Rotate(_rearForearm, 115);
                Rotate(_rearHand, 20);
            }

            // AllIn1 Shader / Combat Material visual polish
            if (_isAllIn1 && _material != null)
            {
                // 1. Hit effect
                _material.SetFloat(HitEffectBlendId, hitBlend);
                _material.SetColor(HitEffectColorId, _activeHitColor);

                // 2. FloorBoss attack windup telegraph aura: GLOW_ON & INNEROUTLINE_ON
                var bossTelegraph = isBoss && attackFrame > 0 && attackFrame < 24;
                if (bossTelegraph)
                {
                    var pulse = 18f + 8f * Mathf.Sin(Time.unscaledTime * 28f);
                    _material.SetFloat(GlowGlobalId, 1f);
                    _material.SetFloat(GlowId, pulse);
                    _material.SetColor(GlowColorId, new Color(1.5f, 0.75f, 0.2f, 1f));

                    _material.SetFloat(InnerOutlineAlphaId, 1f);
                    _material.SetFloat(InnerOutlineThicknessId, 1.6f);
                    _material.SetFloat(InnerOutlineGlowId, 4.5f);
                    _material.SetColor(InnerOutlineColorId, new Color(1.4f, 0.2f, 0.15f, 1f));
                }
                else
                {
                    _material.SetFloat(GlowId, 0f);
                    _material.SetFloat(InnerOutlineAlphaId, 0f);
                }

                // 3. Jump-kicks and rapid dashes: GHOST_ON afterimage trail
                var ghostTrail = action == "JumpKick" || isDashing;
                if (ghostTrail)
                {
                    _material.SetFloat(GhostBlendId, 0.75f);
                    _material.SetFloat(GhostColorBoostId, 1.4f);
                    _material.SetFloat(GhostTransparencyId, 0.15f);
                }
                else
                {
                    _material.SetFloat(GhostBlendId, 0f);
                }
            }
            else
            {
                // Fallback unlit rendering
                for (var i = 0; i < _renderers.Length; i++)
                    _renderers[i].color = hitBlend > 0.05f ? Color.Lerp(_colors[i], _activeHitColor, 0.8f * hitBlend) : _colors[i];
            }
        }

        public void Dispose()
        {
            if (_material != null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Object.Destroy(_material);
                }
                else
                {
                    Object.DestroyImmediate(_material);
                }
                _material = null;
            }
        }
    }
}
