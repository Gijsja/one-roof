using OneRoof.Content;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Crisis
{
    /// <summary>Articulated combat poses on the same wardrobe and bone rig used by Tower residents.</summary>
    internal sealed class CaretakerCombatRig
    {
        private readonly NpcSkeletalHierarchy _rig;
        private readonly Transform _root;
        private readonly SpriteRenderer[] _renderers;
        private readonly Color[] _colors;
        private readonly Transform _upperArm, _forearm, _rearArm, _rearForearm, _thigh, _shin;
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

            _upperArm = Bone(NpcRigDefinition.BoneArmUpperR);
            _forearm = Bone(NpcRigDefinition.BoneArmLowerR);
            _rearArm = Bone(NpcRigDefinition.BoneArmUpperL);
            _rearForearm = Bone(NpcRigDefinition.BoneArmLowerL);
            _thigh = Bone(NpcRigDefinition.BoneLegUpperR);
            _shin = Bone(NpcRigDefinition.BoneLegLowerR);
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
            _rig.SetAnimationClip(moving ? NpcAnimationClip.Walk : NpcAnimationClip.Idle);
            _rig.SetFacing(facing >= 0 ? 1 : -1);
            _rig.ApplyProceduralAnimation(Time.unscaledTime);
            // Keep the same anatomy while bringing the fighting stance into readable guard.
            var duck = crouching || action == "Duck";
            var punch = action == "Punch";
            var kick = action == "Kick" || action == "JumpKick";
            var impact = Mathf.Sin(Mathf.Clamp01(phase) * Mathf.PI);
            _root.localScale = new Vector3((facing >= 0 ? 1 : -1) * 1.35f, duck ? .92f : 1.35f, 1);
            Rotate(_rearArm, 42);
            Rotate(_rearForearm, 110);
            Rotate(_upperArm, punch ? Mathf.Lerp(48, 102, impact) : 48);
            Rotate(_forearm, punch ? Mathf.Lerp(95, -10, impact) : 95);
            if (kick)
            {
                Rotate(_thigh, Mathf.Lerp(12, 100, impact));
                Rotate(_shin, Mathf.Lerp(-38, -8, impact));
                Rotate(_rig.Spine, -12 * impact);
            }

            if (duck)
            {
                if (!kick)
                {
                    Rotate(_thigh, 58);
                    Rotate(_shin, -85);
                }

                Rotate(_rig.Spine, -12);
            }

            if (defeated)
                _root.rotation = Quaternion.Euler(0, 0, -75 * facing);
            else
                _root.rotation = Quaternion.identity;
            for (var i = 0; i < _renderers.Length; i++)
                _renderers[i].color = hit ? Color.Lerp(_colors[i], Color.white, .8f) : _colors[i];
        }
    }
}
