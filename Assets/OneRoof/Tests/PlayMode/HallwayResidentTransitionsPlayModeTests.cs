using System;
using System.Collections;
using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Application.Transit;
using OneRoof.Presentation.Population;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class HallwayResidentTransitionsPlayModeTests : OwnedPlayModeSceneFixture
    {
        [UnityTest]
        public IEnumerator PooledHiddenRider_ReclaimsReboundView_AndExitsOnDestinationFloor()
        {
            var holder = new GameObject("Pooled hallway arrival test");
            var cameraObject = new GameObject("Pooled hallway arrival camera");
            var pool = holder.AddComponent<NpcViewPool>();
            pool.MaxCapacity = 1;
            pool.Prewarm(1);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 2f;
            camera.transform.position = new Vector3(0f, TowerStructurePresenter.FloorY(2), -10f);
            var presenter = new TowerResidentPresenter
            {
                ViewPool = pool, PresentationCamera = camera, HallwayVisible = true
            };
            try
            {
                presenter.Initialize(holder.transform);
                presenter.EnsureResidentViews(2);
                var topology = new TowerSimulationSession().TopologyProjection();
                TowerProjection Snapshot(TransitResidentStatus status, int floor, float cellX, bool includeReplacement = false) =>
                    new TowerProjection(1, 0, 0, 0f,
                        includeReplacement
                            ? new[] { new TransitResidentProjection(7, 4, status, floor, cellX),
                                new TransitResidentProjection(8, 1, TransitResidentStatus.Walking, 1, 8f) }
                            : new[] { new TransitResidentProjection(7, 4, status, floor, cellX) },
                        Array.Empty<ElevatorProjection>());

                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Queued, 0, 0f), topology, 0f);
                Assert.That(pool.TryGetView(7, out var originalView), Is.True);
                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Riding, 4, 0f), topology, 0f);
                Assert.That(pool.TryGetView(7, out _), Is.False, "Hidden riders must release their skeletal slot.");
                Assert.That(originalView.gameObject.activeSelf, Is.False);
                Assert.That(presenter.VisibleResidentCount, Is.Zero, "Hidden riders must also disappear from sprite and macro LOD.");

                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Riding, 4, 0f, true), topology, 0f);
                Assert.That(pool.TryGetView(8, out var replacement), Is.True);
                Assert.That(replacement, Is.SameAs(originalView), "Exercise actual pooled reuse while the rider is concealed.");
                yield return null;
                Assert.That(Time.deltaTime, Is.GreaterThan(0f));

                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Walking, 4, 4f), topology, 0f);
                Assert.That(pool.TryGetView(8, out _), Is.False);
                Assert.That(pool.TryGetView(7, out var arrival), Is.True);
                Assert.That(arrival, Is.SameAs(originalView));
                Assert.That(arrival.gameObject.activeSelf, Is.True);
                Assert.That(arrival.transform.position.y, Is.EqualTo(TowerStructurePresenter.FloorY(4) - .58f).Within(.001f));
                Assert.That(arrival.transform.position.x, Is.EqualTo(-2.4f + 4f * .5f + .25f).Within(.001f),
                    "Rebinding after an elevator exit must snap to the destination landing.");
                Assert.That(pool.TotalInstantiatedCount, Is.EqualTo(1));
                var arrivalX = arrival.transform.position.x;
                yield return null;
                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Walking, 4, 20f), topology, 0f);
                Assert.That(arrival.transform.position.y, Is.EqualTo(TowerStructurePresenter.FloorY(4) - .58f).Within(.001f));
                Assert.That(arrival.transform.position.x, Is.GreaterThan(arrivalX));
                Assert.That(arrival.transform.position.x, Is.LessThan(-2.4f + 20f * .5f + .25f),
                    "Walking after arrival must retain horizontal interpolation.");
            }
            finally
            {
                presenter.Clear();
                UnityEngine.Object.Destroy(holder);
                UnityEngine.Object.Destroy(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator HiddenElevatorRider_ExitsOnDestinationFloor_WithoutWalkingThroughOtherFloors()
        {
            var holder = new GameObject("Hallway arrival interpolation test");
            var presenter = new TowerResidentPresenter();
            try
            {
                presenter.Initialize(holder.transform);
                presenter.EnsureResidentViews(1);
                presenter.HallwayVisible = true;
                var topology = new TowerSimulationSession().TopologyProjection();
                TowerProjection Snapshot(TransitResidentStatus status, int floor, float cellX) =>
                    new TowerProjection(1, 0, 0, 0f,
                        new[] { new TransitResidentProjection(7, 4, status, floor, cellX) },
                        Array.Empty<ElevatorProjection>());
                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Queued, 0, 0f), topology, 0f);
                var view = presenter.ResidentViews[0].transform;
                var departureY = view.position.y;
                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Riding, 4, 0f), topology, 0f);
                Assert.That(view.gameObject.activeSelf, Is.False);
                yield return null;
                Assert.That(UnityEngine.Application.isPlaying, Is.True, "Regression must execute the production PlayMode branch.");
                Assert.That(Time.deltaTime, Is.GreaterThan(0f), "Regression requires the runtime interpolation path.");
                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Walking, 4, 4f), topology, 0f);
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(view.position.y, Is.EqualTo(TowerStructurePresenter.FloorY(4) - .58f).Within(.001f),
                    "An elevator exit must be placed on the destination hallway immediately.");
                Assert.That(view.position.y, Is.GreaterThan(departureY + 1f));
                var arrivalX = view.position.x;
                yield return null;
                presenter.UpdateResidentPositions(Snapshot(TransitResidentStatus.Walking, 4, 20f), topology, 0f);
                Assert.That(view.position.y, Is.EqualTo(TowerStructurePresenter.FloorY(4) - .58f).Within(.001f));
                Assert.That(view.position.x, Is.GreaterThan(arrivalX));
                Assert.That(view.position.x, Is.LessThan(-2.4f + 20f * .5f + .25f),
                    "Subsequent hallway travel must retain horizontal interpolation.");
            }
            finally
            {
                presenter.Clear();
                UnityEngine.Object.Destroy(holder);
            }
        }
    }
}
