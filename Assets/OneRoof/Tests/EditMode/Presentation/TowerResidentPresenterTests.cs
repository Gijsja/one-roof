using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Domain.Population;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class TowerResidentPresenterTests
    {
        private GameObject _holder;
        private TowerResidentPresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_Resident_Holder");
            _presenter = new TowerResidentPresenter();
            _presenter.Initialize(_holder.transform);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Clear();
            if (_holder != null) Object.DestroyImmediate(_holder);
        }

        [Test]
        public void EnsureResidentViews_CreatesRequestedViewCount()
        {
            _presenter.EnsureResidentViews(10);

            Assert.That(_presenter.ResidentCount, Is.EqualTo(10));
            Assert.That(_presenter.ResidentViews.Count, Is.EqualTo(10));
            Assert.That(_presenter.ResidentSkeletons.Count, Is.EqualTo(10));
        }

        [Test]
        public void UpdateResidentPositions_WithSimulationProjection_PositionsResidents()
        {
            var session = new TowerSimulationSession();
            var snapshot = session.Projection();
            var topology = session.TopologyProjection();

            _presenter.EnsureResidentViews(snapshot.Residents.Count);
            _presenter.UpdateResidentPositions(snapshot, topology, 0f);

            Assert.That(_presenter.ResidentCount, Is.EqualTo(snapshot.Residents.Count));
            for (var i = 0; i < _presenter.ResidentCount; i++)
            {
                var view = _presenter.ResidentViews[i];
                Assert.That(view, Is.Not.Null);
                Assert.That(view.transform.position.z, Is.Not.EqualTo(0f)); // Positioned with depth offset
            }
        }

        [Test]
        public void Clear_DestroysAllObjectsAndResetsCount()
        {
            _presenter.EnsureResidentViews(15);
            Assert.That(_presenter.ResidentCount, Is.EqualTo(15));

            _presenter.Clear();

            Assert.That(_presenter.ResidentCount, Is.EqualTo(0));
            Assert.That(_holder.transform.childCount, Is.EqualTo(0));
        }

        [Test]
        public void TryGetResidentView_And_TryGetResidentAt_FindsPositionedResident()
        {
            _presenter.EnsureResidentViews(3);

            var viewFound = _presenter.TryGetResidentView(1, out var bounds, out var sprite, out var tr);
            Assert.That(viewFound, Is.True);
            Assert.That(tr, Is.Not.Null);

            tr.position = new Vector3(3.5f, 1.2f, -0.2f);
            var hitFound = _presenter.TryGetResidentAt(new Vector2(3.5f, 1.55f), 0.45f, out var hitIdx, out var hitBounds, out _, out _);
            Assert.That(hitFound, Is.True);
            Assert.That(hitIdx, Is.EqualTo(1));
            Assert.That(hitBounds.size.x, Is.GreaterThan(0f));
        }

        [Test]
        public void Initialize_ExistingResidentDisablesLegacyCompositeSprite()
        {
            var resident = new GameObject("Resident View 1");
            resident.transform.SetParent(_holder.transform, false);
            var skeletal = resident.AddComponent<OneRoof.Presentation.Population.NpcSkeletalHierarchy>();
            skeletal.EnsureHierarchy();
            skeletal.MainRenderer.enabled = true;

            _presenter.Initialize(_holder.transform);

            Assert.That(skeletal.MainRenderer.enabled, Is.False);
        }

        [Test]
        public void RidingResident_WithoutCarPresenter_StandsInsideShaftAtLargestBank()
        {
            var session = new TowerSimulationSession();
            var residents = new List<TransitResidentProjection>
            {
                new TransitResidentProjection(7, 0, TransitResidentStatus.Riding, 2, 0, null, ActivityKind.Idle, 0, 0)
            };
            var elevators = new List<ElevatorProjection>
            {
                new ElevatorProjection(501, 0, 0, 10, new List<int>()),
                new ElevatorProjection(502, 1, 0, 10, new List<int>()),
                new ElevatorProjection(503, 2, 1, 10, new List<int> { 7 })
            };
            var snapshot = new TowerProjection(10L, 0, 0, 0f, residents, elevators);

            _presenter.EnsureResidentViews(1);
            _presenter.UpdateResidentPositions(snapshot, session.TopologyProjection(), 0f);

            ElevatorBankPresenter.CalculateCarLayout(2, 3, out var layoutX, out var carWidth);
            var expectedX = layoutX + ((0 % 2) - 0.5f) * Mathf.Min(0.12f, carWidth * 0.3f);
            var pos = _presenter.ResidentViews[0].transform.position;
            Assert.That(pos.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(pos.x, Is.GreaterThan(-2.4f).And.LessThan(-1.4f), "Rider must stay inside the shaft cavity.");
            Assert.That(pos.y, Is.EqualTo(TowerStructurePresenter.FloorY(2) - 0.25f).Within(0.001f));
        }

        [Test]
        public void RidingResident_WithCarPresenter_TracksRenderedCarMidTravel()
        {
            var session = new TowerSimulationSession();
            var carHolder = new GameObject("Test_Car_Holder");
            var elevatorPresenter = new ElevatorBankPresenter();
            elevatorPresenter.Initialize(carHolder.transform, null, null);
            elevatorPresenter.EnsureElevatorViews(1);
            elevatorPresenter.ElevatorViews[0].transform.position = new Vector3(-1.9f, 9.9f, 0f);

            var residents = new List<TransitResidentProjection>
            {
                new TransitResidentProjection(3, 0, TransitResidentStatus.Riding, 2, 0, null, ActivityKind.Idle, 0, 0)
            };
            var elevators = new List<ElevatorProjection>
            {
                new ElevatorProjection(501, 2, 1, 10, new List<int> { 3 })
            };
            var snapshot = new TowerProjection(10L, 0, 0, 0f, residents, elevators);

            try
            {
                _presenter.EnsureResidentViews(1);
                _presenter.UpdateResidentPositions(snapshot, session.TopologyProjection(), 0f, null, elevatorPresenter);

                var pos = _presenter.ResidentViews[0].transform.position;
                Assert.That(pos.y, Is.EqualTo(9.65f).Within(0.001f), "Rider feet must sit on the rendered car floor mid-travel.");
                Assert.That(pos.x, Is.EqualTo(-1.96f).Within(0.001f));
            }
            finally
            {
                elevatorPresenter.Clear();
                Object.DestroyImmediate(carHolder);
            }
        }

        [Test]
        public void QueuedResidents_CrowdedLanding_StaysInsideShaftReserve()
        {
            // Regression: the single-file queue marched 12 slots deep into the
            // neighbouring apartment (with red agitation auras). The landing
            // formation must stay inside the shaft reserve [-2.4, -1.4].
            var session = new TowerSimulationSession();
            var residents = new List<TransitResidentProjection>();
            for (var i = 0; i < 14; i++)
            {
                residents.Add(new TransitResidentProjection(100 + i, 0, TransitResidentStatus.Queued, 1, 0, null, ActivityKind.Idle, 0, 25));
            }
            var snapshot = new TowerProjection(10L, 14, 0, 25f, residents, new List<ElevatorProjection>());

            _presenter.EnsureResidentViews(residents.Count);
            _presenter.UpdateResidentPositions(snapshot, session.TopologyProjection(), 0f);

            for (var i = 0; i < residents.Count; i++)
            {
                var pos = _presenter.ResidentViews[i].transform.position;
                Assert.That(pos.x, Is.GreaterThan(-2.5f).And.LessThan(-1.3f), $"Queued resident {i} must stay inside the shaft reserve.");
            }
        }

        [Test]
        public void FacingComposesWithStatureInsteadOfWipingIt()
        {
            var go = new GameObject("Test_Facing");
            try
            {
                var skeletal = go.AddComponent<OneRoof.Presentation.Population.NpcSkeletalHierarchy>();
                skeletal.Initialize(0); // firefighter stature 1.03
                var stature = skeletal.BodyStature;
                Assert.That(stature, Is.InRange(0.9f, 1.1f));

                skeletal.SetFacing(-1f);
                Assert.That(skeletal.FacingDirection, Is.EqualTo(-1f));
                Assert.That(go.transform.localScale.x, Is.EqualTo(-stature).Within(0.0001f));
                Assert.That(go.transform.localScale.y, Is.EqualTo(stature).Within(0.0001f));

                skeletal.SetFacing(1f);
                Assert.That(go.transform.localScale.x, Is.EqualTo(stature).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void QueuedUpdate_PreservesVariantStature()
        {
            var session = new TowerSimulationSession();
            var residents = new List<TransitResidentProjection>
            {
                new TransitResidentProjection(100, 0, TransitResidentStatus.Queued, 1, 0, null, ActivityKind.Idle, 0, 25)
            };
            var snapshot = new TowerProjection(10L, 1, 0, 25f, residents, new List<ElevatorProjection>());

            _presenter.EnsureResidentViews(1);
            var skeletal = _presenter.ResidentSkeletons[0];
            var stature = skeletal.BodyStature;
            _presenter.UpdateResidentPositions(snapshot, session.TopologyProjection(), 0f);

            var scale = _presenter.ResidentViews[0].transform.localScale;
            Assert.That(scale.x, Is.EqualTo(stature).Within(0.0001f), "Queue heading reset must keep stature.");
            Assert.That(scale.y, Is.EqualTo(stature).Within(0.0001f));
        }

        [Test]
        public void WalkingResident_FacesTravelDirection()
        {
            var session = new TowerSimulationSession();
            _presenter.EnsureResidentViews(1);

            var right = new TowerProjection(10L, 0, 0, 0f,
                new List<TransitResidentProjection>
                {
                    new TransitResidentProjection(100, 0, TransitResidentStatus.Walking, 0, 4f, null, ActivityKind.Idle, 0, 0)
                },
                new List<ElevatorProjection>());
            _presenter.UpdateResidentPositions(right, session.TopologyProjection(), 0f);
            _presenter.UpdateResidentPositions(right, session.TopologyProjection(), 0.1f);
            var further = new TowerProjection(11L, 0, 0, 0f,
                new List<TransitResidentProjection>
                {
                    new TransitResidentProjection(100, 0, TransitResidentStatus.Walking, 0, 6f, null, ActivityKind.Idle, 0, 0)
                },
                new List<ElevatorProjection>());
            _presenter.UpdateResidentPositions(further, session.TopologyProjection(), 0.2f);
            Assert.That(_presenter.ResidentSkeletons[0].FacingDirection, Is.EqualTo(1f));

            var back = new TowerProjection(12L, 0, 0, 0f,
                new List<TransitResidentProjection>
                {
                    new TransitResidentProjection(100, 0, TransitResidentStatus.Walking, 0, 1f, null, ActivityKind.Idle, 0, 0)
                },
                new List<ElevatorProjection>());
            _presenter.UpdateResidentPositions(back, session.TopologyProjection(), 0.3f);
            Assert.That(_presenter.ResidentSkeletons[0].FacingDirection, Is.EqualTo(-1f));
            var scale = _presenter.ResidentViews[0].transform.localScale;
            Assert.That(Mathf.Abs(scale.x), Is.EqualTo(_presenter.ResidentSkeletons[0].BodyStature).Within(0.0001f),
                "Travel facing must preserve stature magnitude.");
        }

        [Test]
        public void SleepingResident_LiesFlatTowardFacingInsteadOfTippingSideways()
        {
            var go = new GameObject("Test_Sleeper");
            go.transform.SetParent(_holder.transform, false);
            var skeletal = go.AddComponent<OneRoof.Presentation.Population.NpcSkeletalHierarchy>();
            skeletal.EnsureHierarchy();
            Assert.That(skeletal.Root, Is.Not.Null, "Skeletal hierarchy must build its bone root.");
            skeletal.SleepDirection = -1;
            skeletal.SetAnimationClip(OneRoof.Content.NpcAnimationClip.Sleep);

            skeletal.ApplyProceduralAnimation(1f);

            var z = skeletal.Root.localRotation.eulerAngles.z;
            Assert.That(z, Is.EqualTo(75f).Within(0.5f), "Sleeper must lie flat toward its facing direction.");
        }

        [Test]
        public void WardrobeFallbackColors_ArePlausibleGarmentsNeverBlankWhite()
        {
            foreach (OneRoof.Content.NpcLayerKind layer in System.Enum.GetValues(typeof(OneRoof.Content.NpcLayerKind)))
            {
                var color = OneRoof.Presentation.Population.NpcSkeletalHierarchy.LayerFallbackColor(layer);
                Assert.That(color.a, Is.EqualTo(1f));
                Assert.That(color.grayscale, Is.LessThan(0.97f), $"{layer} fallback must not read as blank white.");
            }
        }
    }
}
