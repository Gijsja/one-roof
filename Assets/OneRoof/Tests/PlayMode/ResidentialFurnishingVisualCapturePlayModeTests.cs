using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Population;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Furnishings;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace OneRoof.Tests.PlayMode
{
    public sealed class ResidentialFurnishingVisualCapturePlayModeTests : OwnedPlayModeSceneFixture
    {
        private const string ScenePath = "Assets/Scenes/Tower_GoldStandard30.unity";
        private const string ArtifactDir = "/home/geisha/.gemini/antigravity/brain/209c71ff-acf1-4c4b-8713-320ad2c07a9d";

        [UnityTest]
        public IEnumerator GoldStandard30_CaptureResidentialEmptyAndFurnishedCutaways()
        {
#if UNITY_EDITOR
            yield return LoadTestScene(ScenePath);
            yield return null;

            var playground = Object.FindFirstObjectByType<GoldStandardPlayground>();
            Assert.That(playground, Is.Not.Null, "GoldStandardPlayground bootstrap component must exist in scene.");
            var tower = playground.Tower;
            Assert.That(tower, Is.Not.Null, "Tower controller must be present.");
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null, "Main camera must be present.");

            try
            {
                tower.SetPaused(true);
                var camCtrl = camera.GetComponent<TowerCameraController>();
                if (camCtrl != null) camCtrl.enabled = false;

                // Set locked cutaway mode so interior backdrops and furniture are 100% visible
                tower.ModeSession.SetFacadeMode(FacadeDisplayMode.LockedCutaway);
                yield return null;

                var sim = tower.SimulationSession;
                Assert.That(sim.FloorCount, Is.EqualTo(30));

                // Find residential rooms on Floor 1
                var topo = sim.TopologyProjection();
                Room roomFloor1Left = null;
                Room roomFloor1Right = null;
                foreach (var r in topo.Rooms.Values)
                {
                    if (r.Floor == 1 && r.Bounds.MinX == -12) roomFloor1Left = r;
                    if (r.Floor == 1 && r.Bounds.MinX == -6) roomFloor1Right = r;
                }
                Assert.That(roomFloor1Left, Is.Not.Null);
                Assert.That(roomFloor1Right, Is.Not.Null);

                // Build housing lifecycle projections for presentation:
                // roomFloor1Left has a fully upgraded home with Bed, Coat Rack, Planter, Sofa, Coffee Table, Study Desk, and Bookcase
                var furnishedProps = new List<string>
                {
                    "prop.furniture.bed.v1",
                    "prop.civic.coatrack.v1",
                    "prop.decor.planter.v1",
                    "prop.furniture.sofa.v1",
                    "prop.furniture.table.v1",
                    "prop.workplace.desk.v1",
                    "prop.furniture.bookcase.v1"
                };

                var customHousingProjections = new List<HouseholdHousingLifecycleProjection>();
                // Keep existing projections for other rooms, but set roomFloor1Left to rich furnishings and omit roomFloor1Right (so it's vacant)
                foreach (var proj in sim.HousingProjection())
                {
                    if (proj.HomeRoomId == roomFloor1Left.Id)
                    {
                        customHousingProjections.Add(new HouseholdHousingLifecycleProjection(
                            proj.HouseholdId,
                            roomFloor1Left.Id,
                            proj.RentArrearsDays,
                            proj.RoomCondition,
                            proj.LeasePhase,
                            proj.HasNotice,
                            proj.NoticeStartedDay,
                            furnishedProps));
                    }
                    else if (proj.HomeRoomId == roomFloor1Right.Id)
                    {
                        // Omit from active housing -> roomFloor1Right becomes a completely vacant apartment shell!
                    }
                    else
                    {
                        customHousingProjections.Add(proj);
                    }
                }

                // Sync presentation geometry and housing conditions
                tower.SyncPresenterGeometry();
                tower.RoomPresenter.UpdateHousingConditions(customHousingProjections);
                yield return null;

                // Verify presenters have appropriate props
                Assert.That(tower.RoomPresenter.TryGetFurnishingPresenter(roomFloor1Left.Id, out var leftFurnishing), Is.True);
                Assert.That(leftFurnishing.PlacedCount, Is.GreaterThanOrEqualTo(6), "Furnished room should display multiple props.");

                Assert.That(tower.RoomPresenter.TryGetFurnishingPresenter(roomFloor1Right.Id, out var rightFurnishing), Is.True);
                Assert.That(rightFurnishing.PlacedCount, Is.EqualTo(0), "Vacant residential room must start completely empty (0 props).");

                Assert.That(tower.RoomPresenter.TryGetRoomBounds(roomFloor1Left.Id, topo, out var leftBounds), Is.True);
                Assert.That(tower.RoomPresenter.TryGetRoomBounds(roomFloor1Right.Id, topo, out var rightBounds), Is.True);
                var midCenter = (leftBounds.center + rightBounds.center) * 0.5f;

                // 1. Capture Overview showing Floors 0 to 4 in LockedCutaway
                camera.transform.position = new Vector3(midCenter.x, midCenter.y + 1.75f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 3.6f;
                yield return null;

                Directory.CreateDirectory(ArtifactDir);
                var overviewPath = Path.Combine(ArtifactDir, "golden30_cutaway_residential_overview.png");
                Capture(camera, overviewPath, 1920, 1080);
                Assert.That(File.Exists(overviewPath), Is.True, "Overview screenshot must be generated.");
                Debug.Log($"Captured residential overview to: {overviewPath}");

                // 2. Capture Close-Up comparison showing both side-by-side:
                // Left room (Furnished) vs Right room (Empty)
                camera.transform.position = new Vector3(midCenter.x, midCenter.y, -10f);
                camera.orthographicSize = 1.35f;
                yield return null;

                var closePath = Path.Combine(ArtifactDir, "golden30_empty_vs_furnished_close.png");
                Capture(camera, closePath, 1920, 1080);
                Assert.That(File.Exists(closePath), Is.True, "Close-up screenshot must be generated.");
                Debug.Log($"Captured close-up comparison to: {closePath}");

                // 3. Zoom directly into the furnished apartment unit to inspect detail (Bed, Planter, Sofa, Table, Desk, Bookcase, Coat Rack)
                camera.transform.position = new Vector3(leftBounds.center.x, leftBounds.center.y, -10f);
                camera.orthographicSize = 0.92f;
                yield return null;

                var detailFurnishedPath = Path.Combine(ArtifactDir, "golden30_furnished_room_detail.png");
                Capture(camera, detailFurnishedPath, 1920, 1080);
                Assert.That(File.Exists(detailFurnishedPath), Is.True, "Furnished detail screenshot must be generated.");
                Debug.Log($"Captured furnished room detail to: {detailFurnishedPath}");

                // 4. Zoom directly into the empty apartment shell unit to inspect clean backdrop shell
                camera.transform.position = new Vector3(rightBounds.center.x, rightBounds.center.y, -10f);
                camera.orthographicSize = 0.92f;
                yield return null;

                var detailEmptyPath = Path.Combine(ArtifactDir, "golden30_empty_room_detail.png");
                Capture(camera, detailEmptyPath, 1920, 1080);
                Assert.That(File.Exists(detailEmptyPath), Is.True, "Empty room detail screenshot must be generated.");
                Debug.Log($"Captured empty room detail to: {detailEmptyPath}");
            }
            finally
            {
                if (tower != null && tower.gameObject != null) Object.Destroy(tower.gameObject);
                if (playground != null && playground.gameObject != null) Object.Destroy(playground.gameObject);
            }
#else
            Assert.Ignore("Visual capture runs in Editor PlayMode.");
            yield break;
#endif
        }

        private static void Capture(Camera camera, string path, int width, int height)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.LogWarning("GraphicsDeviceType is Null, skipping texture read.");
                return;
            }

            var target = new RenderTexture(width, height, 24);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
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
