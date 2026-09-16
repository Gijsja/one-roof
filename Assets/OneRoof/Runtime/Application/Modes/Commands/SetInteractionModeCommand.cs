namespace OneRoof.Application.Modes.Commands
{
    /// <summary>
    /// Command dispatched by user interface controls or hotkeys to request a transition in interaction mode.
    /// </summary>
    public sealed class SetInteractionModeCommand
    {
        public SetInteractionModeCommand(
            InteractionMode targetMode,
            string toolId = null,
            string overlayId = null,
            int? entityId = null,
            int? targetFloor = null)
        {
            TargetMode = targetMode;
            ToolId = toolId;
            OverlayId = overlayId;
            EntityId = entityId;
            TargetFloor = targetFloor;
        }

        public InteractionMode TargetMode { get; }

        public string ToolId { get; }

        public string OverlayId { get; }

        public int? EntityId { get; }

        public int? TargetFloor { get; }
    }
}
