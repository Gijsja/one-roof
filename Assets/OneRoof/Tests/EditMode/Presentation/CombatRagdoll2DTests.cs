using NUnit.Framework;
using OneRoof.Content;
using OneRoof.Presentation.Crisis;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class CombatRagdoll2DTests
    {
        private GameObject _root;
        private NpcSkeletalHierarchy _rig;
        private CombatRagdoll2D _ragdoll;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RagdollTestSubject");
            _rig = _root.AddComponent<NpcSkeletalHierarchy>();
            _rig.EnsureHierarchy();
            _ragdoll = new CombatRagdoll2D(_rig.Bones);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        [Test]
        public void Ragdoll_InitiallyInactive()
        {
            Assert.That(_ragdoll.IsActive, Is.False);
        }

        [Test]
        public void Ragdoll_Activate_EnablesSimulationAndUpdatesBonePositions()
        {
            _ragdoll.Activate(new Vector2(-2.5f, 3f), 15f, 1, 0f);
            Assert.That(_ragdoll.IsActive, Is.True);

            // Step physics multiple frames
            for (var i = 0; i < 30; i++)
            {
                _ragdoll.Step(1f / 60f, 0f);
            }

            // Verify Hip position moved from launch impulse
            var hip = _rig.Bones[NpcRigDefinition.BoneHip];
            Assert.That(hip.position.x, Is.Not.EqualTo(0f));
            Assert.That(hip.position.y, Is.GreaterThanOrEqualTo(0f), "Floor constraint must keep hip above ground baseline.");
        }

        [Test]
        public void Ragdoll_FloorCollision_KeepsAllBonesAboveGround()
        {
            const float groundY = -1.5f;
            _ragdoll.Activate(new Vector2(0f, -5f), 0f, 1, groundY);

            // Let ragdoll fall to the ground
            for (var i = 0; i < 60; i++)
            {
                _ragdoll.Step(1f / 60f, groundY);
            }

            foreach (var pair in _rig.Bones)
            {
                if (pair.Value != null && pair.Key != NpcRigDefinition.BoneRoot)
                {
                    Assert.That(pair.Value.position.y, Is.GreaterThanOrEqualTo(groundY - 0.15f),
                        $"Bone {pair.Key} should rest on or above ground.");
                }
            }
        }

        [Test]
        public void Ragdoll_Deactivate_RestoresOriginalLocalPose()
        {
            var initialHipLocal = _rig.Bones[NpcRigDefinition.BoneHip].localPosition;

            _ragdoll.Activate(new Vector2(3f, 4f), 20f, 1, 0f);
            for (var i = 0; i < 10; i++)
                _ragdoll.Step(1f / 60f, 0f);

            _ragdoll.Deactivate();
            Assert.That(_ragdoll.IsActive, Is.False);

            var restoredHipLocal = _rig.Bones[NpcRigDefinition.BoneHip].localPosition;
            Assert.That(Vector3.Distance(initialHipLocal, restoredHipLocal), Is.LessThan(0.001f));
        }

        [Test]
        public void BaseAnatomy_CreatesSeamlessSegmentRenderers()
        {
            // Verify all limb renderers exist and have SpriteRenderers
            Assert.That(_rig.LimbRenderers.ContainsKey("torso"), Is.True);
            Assert.That(_rig.LimbRenderers.ContainsKey("head"), Is.True);
            Assert.That(_rig.LimbRenderers.ContainsKey(NpcRigDefinition.BoneArmUpperR), Is.True);
            Assert.That(_rig.LimbRenderers.ContainsKey(NpcRigDefinition.BoneArmLowerR), Is.True);
            Assert.That(_rig.LimbRenderers.ContainsKey(NpcRigDefinition.BoneHandR), Is.True);
            Assert.That(_rig.LimbRenderers.ContainsKey(NpcRigDefinition.BoneLegUpperR), Is.True);
            Assert.That(_rig.LimbRenderers.ContainsKey(NpcRigDefinition.BoneLegLowerR), Is.True);
            Assert.That(_rig.LimbRenderers.ContainsKey(NpcRigDefinition.BoneFootR), Is.True);

            // Verify SpriteRenderers are enabled with assigned sprites
            foreach (var pair in _rig.LimbRenderers)
            {
                Assert.That(pair.Value, Is.Not.Null);
                Assert.That(pair.Value.sprite, Is.Not.Null, $"Sprite for {pair.Key} should not be null.");
            }
        }
    }
}
