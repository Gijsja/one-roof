using System;
using OneRoof.Application.Modes.Commands;

namespace OneRoof.Application.Modes
{
    /// <summary>
    /// Application coordinator managing player interaction modes (Build, Inspect, Data, Manage).
    /// Enforces mode transition rules and exposes read-only projections to UI and Presentation layers.
    /// </summary>
    public sealed class ModeShellSession
    {
        private readonly ModeShellState _state = new ModeShellState();

        public event Action<ModeShellProjection> ModeChanged;

        public InteractionMode CurrentMode => _state.CurrentMode;

        public ModeShellProjection Projection()
        {
            return new ModeShellProjection(
                _state.CurrentMode,
                _state.PreviousMode,
                _state.SelectedBuildTool,
                _state.TargetFloor,
                _state.TargetCellX,
                _state.SelectedEntityId,
                _state.SelectedFloor,
                _state.ActiveOverlayId);
        }

        public bool ExecuteCommand(SetInteractionModeCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var changed = false;
            if (_state.CurrentMode != command.TargetMode)
            {
                _state.TransitionTo(command.TargetMode);
                changed = true;
            }

            if (command.ToolId != null || command.TargetMode != InteractionMode.Build)
            {
                _state.SelectBuildTool(command.ToolId);
                changed = true;
            }

            if (command.OverlayId != null || command.TargetMode != InteractionMode.Data)
            {
                _state.SetActiveOverlay(command.OverlayId);
                changed = true;
            }

            if (command.EntityId.HasValue || command.TargetFloor.HasValue)
            {
                _state.SelectEntity(command.EntityId, command.TargetFloor);
                changed = true;
            }

            if (changed)
            {
                ModeChanged?.Invoke(Projection());
            }

            return true;
        }

        public void SwitchMode(InteractionMode mode)
        {
            ExecuteCommand(new SetInteractionModeCommand(mode));
        }

        public void SelectBuildTool(string toolId)
        {
            ExecuteCommand(new SetInteractionModeCommand(InteractionMode.Build, toolId: toolId));
        }

        public void SelectEntity(int entityId, int? floor = null)
        {
            ExecuteCommand(new SetInteractionModeCommand(InteractionMode.Inspect, entityId: entityId, targetFloor: floor));
        }

        public void SetActiveOverlay(string overlayId)
        {
            ExecuteCommand(new SetInteractionModeCommand(InteractionMode.Data, overlayId: overlayId));
        }

        public void SetPlacementTarget(int? floor, int? cellX)
        {
            _state.SetPlacementTarget(floor, cellX);
            ModeChanged?.Invoke(Projection());
        }

        public void CancelOrEscape()
        {
            if (_state.CurrentMode == InteractionMode.Build && _state.SelectedBuildTool != null)
            {
                _state.SelectBuildTool(null);
                _state.SetPlacementTarget(null, null);
                ModeChanged?.Invoke(Projection());
            }
            else if (_state.CurrentMode == InteractionMode.Data && _state.ActiveOverlayId != null)
            {
                _state.SetActiveOverlay(null);
                ModeChanged?.Invoke(Projection());
            }
            else if (_state.SelectedEntityId.HasValue)
            {
                _state.ClearSelection();
                ModeChanged?.Invoke(Projection());
            }
            else if (_state.CurrentMode != InteractionMode.Inspect)
            {
                SwitchMode(InteractionMode.Inspect);
            }
        }
    }
}
