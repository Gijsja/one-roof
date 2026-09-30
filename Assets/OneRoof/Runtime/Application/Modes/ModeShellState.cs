using System;

namespace OneRoof.Application.Modes
{
    public enum FacadeDisplayMode { Auto, LockedFacade, LockedCutaway }

    /// <summary>
    /// Tracks the player's active interaction context across Build, Inspect, and Data modes.
    /// UI controls dispatch commands to change mode context without directly mutating simulation state.
    /// </summary>
    public sealed class ModeShellState
    {
        public FacadeDisplayMode FacadeMode { get; private set; } = FacadeDisplayMode.Auto;

        public void SetFacadeMode(FacadeDisplayMode mode)
        {
            if (!Enum.IsDefined(typeof(FacadeDisplayMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            FacadeMode = mode;
        }

        public InteractionMode CurrentMode { get; private set; } = InteractionMode.Inspect;

        public InteractionMode PreviousMode { get; private set; } = InteractionMode.Inspect;

        public string SelectedBuildTool { get; private set; }

        public int? TargetFloor { get; private set; }

        public int? TargetCellX { get; private set; }

        public int? SelectedEntityId { get; private set; }

        public int? SelectedFloor { get; private set; }

        public string ActiveOverlayId { get; private set; }

        public bool TransitionTo(InteractionMode newMode)
        {
            if (CurrentMode == newMode)
            {
                return false;
            }

            PreviousMode = CurrentMode;
            CurrentMode = newMode;
            return true;
        }

        public void SelectBuildTool(string toolId)
        {
            SelectedBuildTool = toolId;
        }

        public void SetPlacementTarget(int? floor, int? cellX)
        {
            TargetFloor = floor;
            TargetCellX = cellX;
        }

        public void SelectEntity(int? entityId, int? floor = null)
        {
            SelectedEntityId = entityId;
            SelectedFloor = floor;
        }

        public void SetActiveOverlay(string overlayId)
        {
            ActiveOverlayId = overlayId;
        }

        public void ClearSelection()
        {
            SelectedEntityId = null;
            SelectedFloor = null;
            SelectedBuildTool = null;
            TargetFloor = null;
            TargetCellX = null;
        }
    }
}
