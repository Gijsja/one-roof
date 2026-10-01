using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Crisis
{
    /// <summary>
    /// Articulated combat poses and 2D ragdoll controller on the same wardrobe and bone rig used by Tower residents.
    /// Manages connected joint segments (upper arm, lower arm, hand; hip, upper leg, lower leg, foot; spine, head).
    /// </summary>
    internal sealed class CaretakerCombatRig
    {
        private readonly NpcSkeletalHierarchy _rig;
        private readonly Transform _root;
        private readonly SpriteRenderer[] _renderers;
        private readonly Color[] _colors;

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

        public Transform Transform => _root;
        public GameObject GameObject => _root.gameObject;

        public CaretakerCombatRig(Transform parent, string name, int wardrobeIndex, Material material)
        {
            var go = new GameObject(name);
            _root = go.transform;
            _root.SetParent(parent, false);
            _rig = go.AddComponent<NpcSkeletalHierarchy>();
            _rig.Initialize(wardrobeIndex, NpcSkeletalHierarchy.GetOrCreateDefaultSharedMaterial());
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

        public void Present(Vector3 position, int facing, string action, bool moving, float phase, bool hit, bool defeated, bool crouching = false)
        {
            _root.position = position;

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

                for (var i = 0; i < _renderers.Length; i++)
                    _renderers[i].color = hit ? Color.Lerp(_colors[i], Color.white, 0.8f) : _colors[i];
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

            for (var i = 0; i < _renderers.Length; i++)
                _renderers[i].color = hit ? Color.Lerp(_colors[i], Color.white, 0.8f) : _colors[i];
        }
    }
}
