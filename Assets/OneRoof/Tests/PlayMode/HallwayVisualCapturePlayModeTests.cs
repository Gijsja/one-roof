using System.Collections;
using System.IO;
using NUnit.Framework;
using OneRoof.Domain.Commands;
using OneRoof.Domain.Transit;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class HallwayVisualCapturePlayModeTests : OwnedPlayModeSceneFixture
    {
        [UnityTest]
        public IEnumerator Hallway_OpenCutawayRetainsNumberedEntrancesAndRevealsInteriors()
        {
            var holder = new GameObject("Hallway frontage production fixture");
            try
            {
                var controller = holder.AddComponent<TowerPlayableController>();
                controller.SetPaused(true);
                yield return null;
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                camera.GetComponent<TowerCameraController>().enabled = false;
                var hallway = controller.HallwayPresenter;
                var topology = controller.SimulationSession.TopologyProjection();
                OneRoof.Domain.Topology.Room home = null;
                OneRoof.Domain.Topology.Room diner = null;
                var expectedFrontages = 0;
                foreach (var room in topology.Rooms.Values)
                {
                    var content = room.ContentType.Value ?? "";
                    if (content.Contains("elevator_shaft") || room.Floor < 0) continue;
                    expectedFrontages++;
                    if (home == null && content.StartsWith("residential:")) home = room;
                    if (diner == null && content == "commercial:diner") diner = room;
                }
                foreach (var renderer in holder.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.name.StartsWith("Floor Slab "))
                        Assert.That(renderer.sortingOrder, Is.LessThan(-10), "Floor backing must draw behind wallpaper and furniture.");
                Assert.That(home, Is.Not.Null);
                Assert.That(diner, Is.Not.Null);
                Assert.That(hallway.FrontageCount, Is.EqualTo(expectedFrontages));
                Assert.That(hallway.NumberedDoorCount, Is.EqualTo(expectedFrontages));
                Assert.That(hallway.OpenBayCount, Is.GreaterThanOrEqualTo(1));
                var output = "/tmp/one-roof-hallway-visual";
                Directory.CreateDirectory(output);

                camera.orthographicSize = 8f;
                camera.transform.position = new Vector3(-2.4f, TowerStructurePresenter.FloorY(2), -10f);
                yield return null;
                yield return null;
                Assert.That(hallway.WallAlpha, Is.GreaterThan(.95f));
                Assert.That(controller.ResidentPresenter.InteriorsVisible, Is.True);
                Assert.That(hallway.TryGetRoomAccess(home.Id, out var access), Is.True);
                var point = new Vector2(access.WindowBounds.center.x, TowerStructurePresenter.FloorY(home.Floor) - .45f);
                var vertices = hallway.FrontageMesh.vertices;
                for (var i = 0; i < vertices.Length; i += 4)
                    Assert.That(new Rect(vertices[i].x, vertices[i].y, vertices[i + 2].x - vertices[i].x,
                        vertices[i + 2].y - vertices[i].y).Contains(point), Is.False,
                        "Full-opacity hallway must leave the furnished room open.");
                Capture(camera, Path.Combine(output, "hallway-numbered-frontages.png"));

                camera.orthographicSize = 2f;
                camera.transform.position = new Vector3(RoomCenterX(home), TowerStructurePresenter.FloorY(home.Floor), -10f);
                yield return null;
                yield return null;
                Assert.That(hallway.WallAlpha, Is.LessThan(.05f), "Close zoom reveals room interiors.");
                Capture(camera, Path.Combine(output, "hallway-residential-interior-close.png"));

                camera.orthographicSize = 8f;
                camera.transform.position = new Vector3(RoomCenterX(diner), TowerStructurePresenter.FloorY(diner.Floor), -10f);
                yield return null;
                yield return null;
                Assert.That(hallway.WallAlpha, Is.GreaterThan(.95f));
                Capture(camera, Path.Combine(output, "hallway-diner-open-bay.png"));

                camera.orthographicSize = 2f;
                yield return null;
                yield return null;
                Assert.That(hallway.WallAlpha, Is.LessThan(.05f));
                Capture(camera, Path.Combine(output, "hallway-diner-interior-close.png"));
                camera.orthographicSize = 18f;
                yield return null;
                yield return null;
                Assert.That(controller.ResidentPresenter.InteriorsVisible, Is.False,
                    "The facade must conceal indoor views and their bubbles.");
            }
            finally { Object.Destroy(holder); }
        }

        private static float RoomCenterX(OneRoof.Domain.Topology.Room room) =>
            -2.4f + (room.Bounds.MinX + room.Bounds.MaxX + 1) * .25f;

        [UnityTest]
        public IEnumerator Hallway_ActualElevatorCycleAndFloorExpansion()
        {
            var holder = new GameObject("Hallway production fixture");
            try
            {
                var controller = holder.AddComponent<TowerPlayableController>();
                controller.SetPaused(true);
                yield return null;
                var camera = Camera.main;
                Assert.That(camera, Is.Not.Null);
                camera.GetComponent<TowerCameraController>().enabled = false;
                camera.orthographicSize = 5f;
                camera.transform.position = new Vector3(-2.4f, 0f, -10f);
                yield return null;
                var session = controller.SimulationSession;
                var presenter = controller.ElevatorPresenter;
                var output = "/tmp/one-roof-hallway-visual";
                Directory.CreateDirectory(output);
                presenter.UpdateElevatorPositions(session.Projection());
                var initialLanding = holder.transform.Find("Elevator Landing 0 Car 0");
                Assert.That(initialLanding.Find("Landing Door Left").localScale.x, Is.GreaterThan(.4f));
                Capture(camera, Path.Combine(output, "hallway-closed.png"));
                camera.orthographicSize = 1.5f;
                camera.transform.position = new Vector3(initialLanding.position.x, initialLanding.position.y, -10f);
                Capture(camera, Path.Combine(output, "elevator-closed-close.png"));
                camera.orthographicSize = 5f;
                var carIndex = -1;
                for (var tick = 0; tick < 1440 && carIndex < 0; tick++)
                {
                    session.AdvanceOneTick();
                    var snapshot = session.Projection();
                    for (var car = 0; car < snapshot.Elevators.Count; car++)
                        if (snapshot.Elevators[car].Phase == ElevatorCarPhase.DoorsOpening)
                        { carIndex = car; break; }
                }
                Assert.That(carIndex, Is.GreaterThanOrEqualTo(0), "Standard Tower must produce an actual elevator arrival.");
                var opening = session.Projection().Elevators[carIndex];
                Assert.That(opening.DoorCycleTicks, Is.GreaterThan(0));
                presenter.UpdateElevatorPositions(session.Projection());
                var landing = holder.transform.Find($"Elevator Landing {opening.Floor} Car {carIndex}");
                Assert.That(landing, Is.Not.Null);
                Assert.That(landing.gameObject.activeSelf, Is.True);
                var panel = landing.Find("Landing Door Left");
                Assert.That(panel.localScale.x, Is.EqualTo(.485f * (1f - ElevatorBankPresenter.DoorOpenFraction(opening))).Within(.001f));
                camera.transform.position = new Vector3(-2.4f, TowerStructurePresenter.FloorY(opening.Floor), -10f);
                yield return null;
                Capture(camera, Path.Combine(output, "hallway-opening.png"));
                var widePosition = camera.transform.position;
                camera.orthographicSize = 1.5f;
                camera.transform.position = new Vector3(landing.position.x, landing.position.y, -10f);
                Capture(camera, Path.Combine(output, "elevator-opening-close.png"));
                camera.orthographicSize = 5f;
                camera.transform.position = widePosition;
                for (var tick = 0; tick < 30 && session.Projection().Elevators[carIndex].Phase != ElevatorCarPhase.OpenLoading; tick++)
                    session.AdvanceOneTick();
                var loading = session.Projection().Elevators[carIndex];
                Assert.That(loading.Phase, Is.EqualTo(ElevatorCarPhase.OpenLoading));
                Assert.That(loading.Floor, Is.EqualTo(opening.Floor));
                presenter.UpdateElevatorPositions(session.Projection());
                Assert.That(panel.localScale.x, Is.Zero);
                for (var otherFloor = session.ElevatorMinFloor; otherFloor <= session.ElevatorMaxFloor; otherFloor++)
                    if (otherFloor != loading.Floor)
                        Assert.That(holder.transform.Find($"Elevator Landing {otherFloor} Car {carIndex}/Landing Door Left").localScale.x,
                            Is.GreaterThan(.4f), "Only the arrived car's landing may open.");
                yield return null;
                Capture(camera, Path.Combine(output, "hallway-open.png"));
                camera.orthographicSize = 1.5f;
                camera.transform.position = new Vector3(landing.position.x, landing.position.y, -10f);
                Capture(camera, Path.Combine(output, "elevator-open-close.png"));
                camera.orthographicSize = 5f;
                camera.transform.position = widePosition;
                for (var tick = 0; tick < 30 && session.Projection().Elevators[carIndex].Phase != ElevatorCarPhase.DoorsClosing; tick++)
                    session.AdvanceOneTick();
                Assert.That(session.Projection().Elevators[carIndex].Phase, Is.EqualTo(ElevatorCarPhase.DoorsClosing));
                for (var tick = 0; tick < opening.DoorCycleTicks; tick++) session.AdvanceOneTick();
                presenter.UpdateElevatorPositions(session.Projection());
                Assert.That(panel.localScale.x, Is.GreaterThan(.4f), "Landing must close before travel.");
                yield return null;
                Capture(camera, Path.Combine(output, "hallway-departed.png"));
                var oldCount = presenter.LandingDoorCount;
                var floor = session.FloorCount;
                Assert.That(session.ExecuteCommand(new BuildFloorSlabCommand(floor, -14, 16)).Accepted, Is.True);
                Assert.That(session.ExecuteCommand(new AddElevatorShaftCommand(0, 1, 0, floor)).Accepted, Is.True);
                controller.SyncPresenterGeometry();
                presenter.UpdateElevatorPositions(session.Projection());
                Assert.That(presenter.LandingDoorCount, Is.EqualTo(oldCount + session.ElevatorCarCount));
                Assert.That(holder.transform.Find($"Elevator Landing {floor} Car 0"), Is.Not.Null);
                var hallway = holder.transform.Find("Hallway Circulation");
                Assert.That(hallway, Is.Not.Null);
                Assert.That(hallway.GetComponent<MeshRenderer>().bounds.max.y,
                    Is.GreaterThan(TowerStructurePresenter.FloorY(floor) - .75f));
                camera.orthographicSize = 6.5f;
                camera.transform.position = new Vector3(-2.4f, TowerStructurePresenter.FloorY(2), -10f);
                yield return null;
                Capture(camera, Path.Combine(output, "hallway-expanded-overview.png"));
            }
            finally { Object.Destroy(holder); }
        }

        private static void Capture(Camera camera, string path)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var target = new RenderTexture(1280, 720, 24);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Assert.That(File.Exists(path), Is.True);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.Destroy(target);
                Object.Destroy(image);
            }
        }
    }
}
