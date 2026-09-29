using NUnit.Framework;
using OneRoof.Application.Tower;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Architecture;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class FloorDeckViewTests
    {
        private GameObject _holder;
        private Material _material;
        private MaterialPropertyBlock _colorBlock;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Test_FloorDeck_Holder");
            _material = new Material(Shader.Find("Sprites/Default"));
            _colorBlock = new MaterialPropertyBlock();
            FloorThemeCatalog.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            if (_material != null) Object.DestroyImmediate(_material);
            if (_holder != null) Object.DestroyImmediate(_holder);
            FloorThemeCatalog.ClearCache();
        }

        [Test]
        public void FloorDeckView_GeneratesPhysicalDeckFillingInterFloorGap()
        {
            var deckObj = new GameObject("Floor Deck 1");
            deckObj.transform.SetParent(_holder.transform, false);

            var view = deckObj.AddComponent<FloorDeckView>();
            view.Initialize(1, _material, _colorBlock);

            var slab = new CellBounds(1, -14, 16);
            view.UpdateGeometry(slab, hasShaftCutout: true);

            var floorY = TowerStructurePresenter.FloorY(1);
            var expectedBaselineY = floorY - 0.74f;
            var expectedBelowCeilingY = TowerStructurePresenter.FloorY(0) + 0.74f;
            var expectedGap = expectedBaselineY - expectedBelowCeilingY; // 0.27f

            Assert.That(expectedGap, Is.EqualTo(0.27f).Within(0.001f));

            // Left wing exists and has tread, core, and soffit
            Assert.That(view.LeftWing, Is.Not.Null);
            Assert.That(view.LeftTread, Is.Not.Null);
            Assert.That(view.LeftCore, Is.Not.Null);
            Assert.That(view.LeftSoffit, Is.Not.Null);

            // Verify tread top sits right at baselineY
            var treadCenterY = view.LeftTread.transform.localPosition.y;
            var treadHeight = view.LeftTread.transform.localScale.y;
            var treadTop = treadCenterY + treadHeight * 0.5f;
            Assert.That(treadTop, Is.EqualTo(expectedBaselineY).Within(0.001f));

            // Verify soffit bottom meets the ceiling of the level below
            var soffitCenterY = view.LeftSoffit.transform.localPosition.y;
            var soffitHeight = view.LeftSoffit.transform.localScale.y;
            var soffitBottom = soffitCenterY - soffitHeight * 0.5f;
            Assert.That(soffitBottom, Is.EqualTo(expectedBelowCeilingY).Within(0.001f));

            // Verify total depth equals the inter-floor gap (0.27f)
            var totalDepth = treadTop - soffitBottom;
            Assert.That(totalDepth, Is.EqualTo(expectedGap).Within(0.001f));
        }

        [Test]
        public void FloorDeckView_SplitsAroundElevatorShaftLeavingVerticalChuteOpen()
        {
            var deckObj = new GameObject("Floor Deck 2");
            deckObj.transform.SetParent(_holder.transform, false);

            var view = deckObj.AddComponent<FloorDeckView>();
            view.Initialize(2, _material, _colorBlock);

            var slab = new CellBounds(2, -14, 16);
            view.UpdateGeometry(slab, hasShaftCutout: true);

            var worldLeft = -2.40f + slab.MinX * 0.5f; // -9.40f
            var worldRight = -2.40f + (slab.MaxX + 1) * 0.5f; // 6.10f

            // Left wing right edge must be at FloorDeckView.ShaftLeft (-2.40f)
            var leftCenterX = view.LeftTread.transform.localPosition.x;
            var leftWidth = view.LeftTread.transform.localScale.x;
            var leftRightEdge = leftCenterX + leftWidth * 0.5f;
            var leftLeftEdge = leftCenterX - leftWidth * 0.5f;

            Assert.That(leftRightEdge, Is.EqualTo(FloorDeckView.ShaftLeft).Within(0.001f));
            Assert.That(leftLeftEdge, Is.EqualTo(worldLeft).Within(0.001f));

            // Right wing left edge must be at FloorDeckView.ShaftRight (-1.40f)
            var rightCenterX = view.RightTread.transform.localPosition.x;
            var rightWidth = view.RightTread.transform.localScale.x;
            var rightLeftEdge = rightCenterX - rightWidth * 0.5f;
            var rightRightEdge = rightCenterX + rightWidth * 0.5f;

            Assert.That(rightLeftEdge, Is.EqualTo(FloorDeckView.ShaftRight).Within(0.001f));
            Assert.That(rightRightEdge, Is.EqualTo(worldRight).Within(0.001f));

            // Vertical chute between -2.40f and -1.40f remains open (no deck geometry in that span)
            Assert.That(rightLeftEdge - leftRightEdge, Is.EqualTo(1.00f).Within(0.001f));
        }

        [Test]
        public void FloorDeckView_PreservesDesignatedZDepthsAndSortingOrders()
        {
            var deckObj = new GameObject("Floor Deck 0");
            deckObj.transform.SetParent(_holder.transform, false);

            var view = deckObj.AddComponent<FloorDeckView>();
            view.Initialize(0, _material, _colorBlock);
            view.UpdateGeometry(new CellBounds(0, -14, 16));

            // Z positions: Tread (0.05f), Core (0.08f), Soffit (0.12f)
            Assert.That(view.LeftTread.transform.localPosition.z, Is.EqualTo(FloorDeckView.TreadZ).Within(0.001f));
            Assert.That(view.LeftCore.transform.localPosition.z, Is.EqualTo(FloorDeckView.CoreZ).Within(0.001f));
            Assert.That(view.LeftSoffit.transform.localPosition.z, Is.EqualTo(FloorDeckView.SoffitZ).Within(0.001f));

            // Sorting orders: Tread (-1), Core (-2), Soffit (-3)
            Assert.That(view.LeftTread.sortingOrder, Is.EqualTo(FloorDeckView.TreadSortingOrder));
            Assert.That(view.LeftCore.sortingOrder, Is.EqualTo(FloorDeckView.CoreSortingOrder));
            Assert.That(view.LeftSoffit.sortingOrder, Is.EqualTo(FloorDeckView.SoffitSortingOrder));
        }

        [Test]
        public void FloorDeckView_ApplyTheme_UpdatesColorsSuccessfully()
        {
            var deckObj = new GameObject("Floor Deck 1");
            deckObj.transform.SetParent(_holder.transform, false);

            var view = deckObj.AddComponent<FloorDeckView>();
            view.Initialize(1, _material, _colorBlock);
            view.UpdateGeometry(new CellBounds(1, -14, 16));

            var parquet = FloorThemeCatalog.GetTheme(FloorThemeCatalog.HardwoodParquet);
            view.ApplyTheme(parquet);

            Assert.That(view.CurrentTheme.Id, Is.EqualTo(FloorThemeCatalog.HardwoodParquet));

            var block = new MaterialPropertyBlock();
            view.LeftTread.GetPropertyBlock(block);
            var color = block.GetColor("_BaseColor");
            Assert.That(color, Is.EqualTo(Color.white), "Theme color is baked into the surface texture and must not tint it a second time.");
            Assert.That(block.GetTexture("_BaseMap"), Is.SameAs(FloorThemeCatalog.GetOrLoadSurfaceSprite(parquet).texture));

            view.LeftCore.GetPropertyBlock(block);
            var coreColor = block.GetColor("_BaseColor");
            Assert.That(coreColor.r, Is.EqualTo(parquet.CoreColor.r).Within(0.01f));
            Assert.That(coreColor.g, Is.EqualTo(parquet.CoreColor.g).Within(0.01f));
            Assert.That(coreColor.b, Is.EqualTo(parquet.CoreColor.b).Within(0.01f));
        }

        [Test]
        public void FloorDeckPresenter_LifecycleAndTheming_ManagesAllFloors()
        {
            var presenter = new FloorDeckPresenter();
            presenter.Initialize(_holder.transform, _material, _colorBlock);

            var topology = new TowerSimulationSession().TopologyProjection();
            presenter.EnsureFloorDecks(topology);

            Assert.That(presenter.RenderedDeckCount, Is.EqualTo(topology.FloorCount));

            // Floor 0 deck exists
            Assert.That(presenter.TryGetFloorDeck(0, out var deck0), Is.True);
            Assert.That(deck0, Is.Not.Null);

            // Can override floor theme
            presenter.SetFloorTheme(2, FloorThemeCatalog.HighTechGridTile);
            var deck2 = presenter.GetFloorDeck(2);
            Assert.That(deck2.CurrentTheme.Id, Is.EqualTo(FloorThemeCatalog.HighTechGridTile));

            // Clear removes views
            presenter.Clear();
            Assert.That(presenter.RenderedDeckCount, Is.EqualTo(0));
            Assert.That(_holder.transform.childCount, Is.EqualTo(0));
        }
    }
}
