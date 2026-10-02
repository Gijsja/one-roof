using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Persistence;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class UndercityEarthPresentationTests
    {
        [Test]
        public void AdjacentExcavationsExposeOnlyCombinedPerimeter()
        {
            var holder = new GameObject("Earth test");
            var material = new Material(Shader.Find("Sprites/Default"));
            var presenter = new BuildingExteriorPresenter();
            try
            {
                presenter.Initialize(holder.transform, material, new MaterialPropertyBlock());
                var single = new[] { new UndergroundCellSaveData { x = 10, depth = 2 } };
                presenter.EnsureExteriorViews(null, new UndergroundDigProjection(UndergroundDigState.FromSaveData(single)));
                var ground = presenter.Root.Find("Ground Dressing/Undercity");
                var singleFaces = ground.Find("Excavation Rock Faces").GetComponent<MeshFilter>().sharedMesh;
                var singleView = ground.Find("Excavated Cell 10_2");
                var seamX = singleView.localPosition.x + 0.5f;
                var cavityY = singleView.localPosition.y;
                Assert.That(HasFaceAtCavityMidpoint(singleFaces, seamX, cavityY), Is.True,
                    "An isolated excavation must expose rock/shadow against the undug neighbor.");

                var adjacent = new[] { single[0], new UndergroundCellSaveData { x = 11, depth = 2 } };
                // Use a new presenter so independent save snapshots with equal revisions are not treated as an update.
                presenter.Clear();
                presenter.Initialize(holder.transform, material, new MaterialPropertyBlock());
                presenter.EnsureExteriorViews(null, new UndergroundDigProjection(UndergroundDigState.FromSaveData(adjacent)));
                ground = presenter.Root.Find("Ground Dressing/Undercity");
                var joinedFaces = ground.Find("Excavation Rock Faces").GetComponent<MeshFilter>().sharedMesh;
                Assert.That(HasFaceAtCavityMidpoint(joinedFaces, seamX, cavityY), Is.False,
                    "The shared interior seam must remain open after excavating the neighboring cell.");
                var outerX = ground.Find("Excavated Cell 11_2").localPosition.x + 0.5f;
                Assert.That(HasFaceAtCavityMidpoint(joinedFaces, outerX, cavityY), Is.True,
                    "The new cavity perimeter must still expose rock against undug earth.");
                var earth = ground.Find("Continuous Earth Strata").GetComponent<MeshFilter>().sharedMesh;
                Assert.That(earth.bounds.size.x, Is.EqualTo(UndergroundDigState.GridWidthCells).Within(0.001f));
                Assert.That(earth.bounds.size.y, Is.EqualTo(UndergroundDigState.MaxDepthCells).Within(0.001f));
                Assert.That(ground.Find("Earth Grid 10_2"), Is.Null, "Geology uses one batch rather than one renderer per cell.");
            }
            finally
            {
                presenter.Clear();
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(holder);
            }
        }
        private static bool HasFaceAtCavityMidpoint(Mesh mesh, float seamX, float cavityY)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var centroid = (vertices[triangles[i]] + vertices[triangles[i + 1]]
                    + vertices[triangles[i + 2]]) / 3f;
                if (Mathf.Abs(centroid.x - seamX) < 0.13f
                    && Mathf.Abs(centroid.y - cavityY) < 0.3f)
                    return true;
            }
            return false;
        }

        [Test]
        public void UnchangedProjectionReusesEarthAndFacesAndClearReleasesBothMeshes()
        {
            var holder = new GameObject("Earth lifecycle test");
            var material = new Material(Shader.Find("Sprites/Default"));
            var presenter = new BuildingExteriorPresenter();
            try
            {
                presenter.Initialize(holder.transform, material, new MaterialPropertyBlock());
                var cells = new[] { new UndergroundCellSaveData { x = 10, depth = 2 } };
                var projection = new UndergroundDigProjection(UndergroundDigState.FromSaveData(cells));
                presenter.EnsureExteriorViews(null, projection);
                var earth = presenter.Root.Find("Ground Dressing/Undercity/Continuous Earth Strata").GetComponent<MeshFilter>().sharedMesh;
                var faces = presenter.Root.Find("Ground Dressing/Undercity/Excavation Rock Faces").GetComponent<MeshFilter>().sharedMesh;
                presenter.EnsureExteriorViews(null, projection);
                Assert.That(presenter.Root.Find("Ground Dressing/Undercity/Continuous Earth Strata").GetComponent<MeshFilter>().sharedMesh,
                    Is.SameAs(earth), "Unchanged terrain must not rebuild its mesh.");
                Assert.That(presenter.Root.Find("Ground Dressing/Undercity/Excavation Rock Faces").GetComponent<MeshFilter>().sharedMesh,
                    Is.SameAs(faces), "Unchanged excavation must not rebuild its boundary mesh.");
                presenter.Clear();
                Assert.That(earth == null, Is.True, "Generated earth mesh must be destroyed on teardown.");
                Assert.That(faces == null, Is.True, "Generated cavity mesh must be destroyed on teardown.");
            }
            finally
            {
                presenter.Clear();
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(holder);
            }
        }
    }
}
