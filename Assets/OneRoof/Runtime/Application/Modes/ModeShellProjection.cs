namespace OneRoof.Application.Modes
{
    /// <summary>
    /// Read-only snapshot of the active interaction mode and context for presentation and UI layers.
    /// </summary>
    public sealed class ModeShellProjection
    {
        public ModeShellProjection(
            InteractionMode currentMode,
            InteractionMode previousMode,
            string selectedBuildTool,
            int? targetFloor,
            int? targetCellX,
            int? selectedEntityId,
            int? selectedFloor,
            string activeOverlayId)
        {
            CurrentMode = currentMode;
            PreviousMode = previousMode;
            SelectedBuildTool = selectedBuildTool;
            TargetFloor = targetFloor;
            TargetCellX = targetCellX;
            SelectedEntityId = selectedEntityId;
            SelectedFloor = selectedFloor;
            ActiveOverlayId = activeOverlayId;
        }

        public InteractionMode CurrentMode { get; }

        public InteractionMode PreviousMode { get; }

        public string SelectedBuildTool { get; }

        public int? TargetFloor { get; }

        public int? TargetCellX { get; }

        public int? SelectedEntityId { get; }

        public int? SelectedFloor { get; }

        public string ActiveOverlayId { get; }

        public bool IsInspectMode => CurrentMode == InteractionMode.Inspect;

        public bool IsBuildMode => CurrentMode == InteractionMode.Build;

        public bool IsDataMode => CurrentMode == InteractionMode.Data;

        public bool IsManageMode => CurrentMode == InteractionMode.Manage;
    }
}
