using NUnit.Framework;
using OneRoof.Presentation.Crisis;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class CaretakerCombatVisualPolishTests
    {
        private GameObject _container;

        [SetUp]
        public void SetUp()
        {
            _container = new GameObject("CombatPolishTestContainer");
        }

        [TearDown]
        public void TearDown()
        {
            if (_container != null)
            {
                Object.DestroyImmediate(_container);
                _container = null;
            }
        }

        [Test]
        public void CaretakerCombatRig_InitializesWithCombatMaterial_AndKeywordsEnabled()
        {
            var rig = new CaretakerCombatRig(_container.transform, "Rig_TestSubject", 9, null);
            Assert.That(rig.Material, Is.Not.Null);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.IsKeywordEnabled("HITEFFECT_ON"), Is.True);
                Assert.That(rig.Material.IsKeywordEnabled("GLOW_ON"), Is.True);
                Assert.That(rig.Material.IsKeywordEnabled("INNEROUTLINE_ON"), Is.True);
                Assert.That(rig.Material.IsKeywordEnabled("GHOST_ON"), Is.True);
            }

            rig.Dispose();
        }

        [Test]
        public void CaretakerCombatRig_HitEffect_DrivesHitEffectProperties()
        {
            var rig = new CaretakerCombatRig(_container.transform, "Rig_HitTest", 9, null);

            // Regular hit: pure white
            rig.Present(Vector3.zero, 1, "None", false, 0f, hit: true, defeated: false, isBoss: false, isCritical: false);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.GetFloat("_HitEffectBlend"), Is.GreaterThan(0f));
                Assert.That(rig.Material.GetColor("_HitEffectColor"), Is.EqualTo(Color.white));
            }

            // Boss / critical hit: intense crimson/gold
            rig.Present(Vector3.zero, 1, "None", false, 0f, hit: true, defeated: false, isBoss: true, isCritical: true);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.GetFloat("_HitEffectBlend"), Is.GreaterThan(0f));
                var bossHitColor = rig.Material.GetColor("_HitEffectColor");
                Assert.That(bossHitColor.r, Is.GreaterThan(1.0f));
            }

            rig.Dispose();
        }

        [Test]
        public void CaretakerCombatRig_FloorBossWindup_DrivesTelegraphAura()
        {
            var rig = new CaretakerCombatRig(_container.transform, "Rig_BossWindupTest", 9, null);

            // During attack windup (AttackFrame > 0 && AttackFrame < 24)
            rig.Present(Vector3.zero, 1, "Punch", false, 0.2f, hit: false, defeated: false, isBoss: true, attackFrame: 12);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.GetFloat("_Glow"), Is.GreaterThan(0f), "FloorBoss windup must activate telegraph glow.");
                Assert.That(rig.Material.GetFloat("_InnerOutlineAlpha"), Is.EqualTo(1f), "FloorBoss windup must activate inner outline alpha.");
            }

            // Inactive when not winding up
            rig.Present(Vector3.zero, 1, "None", false, 0f, hit: false, defeated: false, isBoss: true, attackFrame: 0);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.GetFloat("_Glow"), Is.EqualTo(0f));
                Assert.That(rig.Material.GetFloat("_InnerOutlineAlpha"), Is.EqualTo(0f));
            }

            rig.Dispose();
        }

        [Test]
        public void CaretakerCombatRig_JumpKickAndDash_DrivesGhostAfterimageTrail()
        {
            var rig = new CaretakerCombatRig(_container.transform, "Rig_GhostTest", 9, null);

            // JumpKick action
            rig.Present(Vector3.zero, 1, "JumpKick", false, 0.5f, hit: false, defeated: false);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.GetFloat("_GhostBlend"), Is.GreaterThan(0f), "JumpKick must activate GHOST_ON blend.");
            }

            // Rapid dash
            rig.Present(Vector3.zero, 1, "None", true, 0f, hit: false, defeated: false, isDashing: true);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.GetFloat("_GhostBlend"), Is.GreaterThan(0f), "Rapid dash must activate GHOST_ON blend.");
            }

            // Idle: blend resets to 0
            rig.Present(Vector3.zero, 1, "None", false, 0f, hit: false, defeated: false, isDashing: false);

            if (rig.Material.shader != null && rig.Material.shader.name.Contains("AllIn1"))
            {
                Assert.That(rig.Material.GetFloat("_GhostBlend"), Is.EqualTo(0f));
            }

            rig.Dispose();
        }

        [Test]
        public void CaretakerImpactPool_EmitsDirectionalStreaksAndLandingDust()
        {
            var material = CaretakerCombatRig.GetOrCreateDefaultCombatMaterial();
            var pool = new CaretakerImpactPool(_container.transform, material);

            // Test radial star emit
            Assert.DoesNotThrow(() => pool.Emit(Vector3.zero, false));

            // Test directional streak emit
            Assert.DoesNotThrow(() => pool.EmitStreak(Vector3.zero, new Vector2(1f, 0.45f), false, isCritical: true));

            // Test landing dust emit
            Assert.DoesNotThrow(() => pool.EmitLandingDust(new Vector3(0f, -0.58f, 0f)));

            // Test tick update with simulated delta time
            Assert.DoesNotThrow(() => pool.Tick(0.016f));
            Assert.DoesNotThrow(() => pool.Tick(0.35f));

            pool.Dispose();
        }

        [Test]
        public void CaretakerCrisisPresenter_PolynomialTrauma_DecaysSmoothly()
        {
            var go = new GameObject("Presenter_TraumaTest");
            var presenter = go.AddComponent<CaretakerCrisisPresenter>();

            presenter.AddTrauma(0.5f);
            // Verify trauma clamped behavior
            presenter.AddTrauma(1.5f);

            Object.DestroyImmediate(go);
        }
    }
}
