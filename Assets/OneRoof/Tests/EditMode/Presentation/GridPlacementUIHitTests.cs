using System.Linq;
using NUnit.Framework;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Economy;
using OneRoof.Domain.Topology;
using OneRoof.Presentation.Tower;
using OneRoof.UI.Modes;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class GridPlacementUIHitTests
    {
        [Test]
        public void IsPointerOverUI_IdentifiesBottomAndTopUIRegions()
        {
            // Bottom UI region (Mode bar, context overlay, build palette)
            // guiY is Screen.height - screenPos.y, so a screenPos near y=30 has guiY = Screen.height - 30 (in bottom UI)
            var bottomScreenPos = new Vector3(100f, 30f, 0f);
            Assert.That(GridPlacementController.IsPointerOverUI(bottomScreenPos, screenHeight: 480, screenWidth: 640), Is.True);

            // World area must not assume a large viewport: the headless runner is
            // 640x480, where the fixed 650px palette rect covers the screen center.
            // (300, 375) stays world everywhere it matters: screenY 375 sits just
            // above the palette's top edge (368), x 300 sits right of the HUD
            // column and left of the inspector/preview cards, and top-origin
            // y (H-375) falls outside the HUD strip and the card y-ranges.
            var worldScreenPos = new Vector3(300f, 375f, 0f);
            Assert.That(GridPlacementController.IsPointerOverUI(worldScreenPos, screenHeight: 480, screenWidth: 640), Is.False);
        }

        [Test]
        public void IsPointerOverUI_ProtectsEveryBuildPaletteRow()
        {
            // Input coordinates originate at the lower-left; the upper palette row
            // therefore has a larger Y than the mode bar below it.
            var upperPaletteRow = new Vector3(100f, 330f, 0f);
            var lowerPaletteRow = new Vector3(100f, 90f, 0f);

            Assert.That(GridPlacementController.IsPointerOverUI(upperPaletteRow), Is.True);
            Assert.That(GridPlacementController.IsPointerOverUI(lowerPaletteRow), Is.True);
        }

        [Test]
        public void IsPointerOverUI_DoesNotReservePaletteAreaAfterToolSelection()
        {
            // A palette row outside the persistent context strip becomes world
            // space after the chooser closes; the selected tool can build there.
            // Pin the same compact viewport used by the neighboring pointer-area
            // regression so a tall Game view cannot move this point into the HUD.
            const int screenHeight = 480;
            const int screenWidth = 640;
            var palettePosition = new Vector3(100f, screenHeight - 240f, 0f);

            Assert.That(GridPlacementController.IsPointerOverUI(palettePosition, includeBuildPalette: true, screenHeight, screenWidth), Is.True);
            Assert.That(GridPlacementController.IsPointerOverUI(palettePosition, includeBuildPalette: false, screenHeight, screenWidth), Is.False);
        }

        [Test]
        public void IsPointerOverUI_WithToolSelected_StillProtectsVisibleModeBarAndContext()
        {
            var bar = ModeShellBarController.ModeBarRect(Screen.height);
            var context = ModeShellBarController.ContextRect(Screen.height, isBuildMode: true);

            Assert.That(GridPlacementController.IsPointerOverUI(
                new Vector3(bar.center.x, Screen.height - bar.center.y), includeBuildPalette: false), Is.True);
            Assert.That(GridPlacementController.IsPointerOverUI(
                new Vector3(context.center.x, Screen.height - context.center.y), includeBuildPalette: false), Is.True);
        }

        [Test]
        public void IsPointerOverUI_ShieldsActualPaletteBoundsWhileChooserIsOpen()
        {
            var palette = ModeShellBarController.BuildPaletteRect(Screen.height);
            var upperRow = new Vector3(palette.center.x, Screen.height - (palette.yMin + 20f));
            var lowerRow = new Vector3(palette.center.x, Screen.height - (palette.yMax - 20f));

            Assert.That(GridPlacementController.IsPointerOverUI(upperRow, includeBuildPalette: true), Is.True);
            Assert.That(GridPlacementController.IsPointerOverUI(lowerRow, includeBuildPalette: true), Is.True);
        }
    }
}
