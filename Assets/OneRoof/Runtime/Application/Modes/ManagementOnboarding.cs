using System;

namespace OneRoof.Application.Modes
{
    /// <summary>Evidence-driven, optional first management lesson. UI state is not simulation state.</summary>
    public sealed class ManagementOnboarding
    {
        public enum Step
        {
            NoticeQueue,
            OpenOverlay,
            InspectCause,
            PreviewCapacity,
            BuildCapacity,
            MeasureImprovement,
            OpenManage,
            Complete
        }

        private float _baselineWait;
        private int _baselineQueue;
        private bool _skipped;

        public Step CurrentStep { get; private set; }
        public bool IsSkipped => _skipped;
        public bool IsComplete => CurrentStep == Step.Complete;

        public string Instruction
        {
            get
            {
                switch (CurrentStep)
                {
                    case Step.NoticeQueue: return "Watch the morning elevator queue as residents travel to work.";
                    case Step.OpenOverlay: return "Open Data (3) and choose Elevator Wait to see the bottleneck.";
                    case Step.InspectCause: return "Inspect the queue to see what is causing the wait.";
                    case Step.PreviewCapacity: return "Open Build (1) and preview another elevator car.";
                    case Step.BuildCapacity: return "Build the extra car when the preview is valid and affordable.";
                    case Step.MeasureImprovement: return "Watch the queue shrink after the new car begins carrying residents.";
                    case Step.OpenManage: return "Open Manage (4) to compare the costs and effects of a decree.";
                    default: return "The tower is yours to manage. Contextual help remains available.";
                }
            }
        }

        public void ObserveQueue(int queuedResidents, float averageWaitTicks)
        {
            if (_skipped || queuedResidents <= 0 || averageWaitTicks <= 0f || CurrentStep >= Step.MeasureImprovement) return;
            _baselineWait = Math.Max(_baselineWait, averageWaitTicks);
            _baselineQueue = Math.Max(_baselineQueue, queuedResidents);
            if (CurrentStep == Step.NoticeQueue && queuedResidents >= 5 && averageWaitTicks >= 5f)
                CurrentStep = Step.OpenOverlay;
        }

        public void ObserveOverlay(string overlayId)
        {
            if (!_skipped && CurrentStep == Step.OpenOverlay && overlayId == "overlay:elevator_wait")
                CurrentStep = Step.InspectCause;
        }

        public void ObserveInspector(bool showsCongestionCause)
        {
            if (!_skipped && CurrentStep == Step.InspectCause && showsCongestionCause)
                CurrentStep = Step.PreviewCapacity;
        }

        public void ObservePreview(bool isValid, float estimatedImprovementPercent)
        {
            if (!_skipped && CurrentStep == Step.PreviewCapacity && isValid && estimatedImprovementPercent > 0f)
                CurrentStep = Step.BuildCapacity;
        }

        public void ObserveCapacityBuilt(bool accepted)
        {
            if (!_skipped && CurrentStep == Step.BuildCapacity && accepted)
                CurrentStep = Step.MeasureImprovement;
        }

        public void ObserveMeasuredWait(float comparableAverageWaitTicks)
        {
            if (_skipped || CurrentStep != Step.MeasureImprovement || comparableAverageWaitTicks < 0f) return;
            if (comparableAverageWaitTicks < _baselineWait)
                CurrentStep = Step.OpenManage;
        }

        public void ObserveMeasuredQueue(int queuedResidents)
        {
            if (_skipped || CurrentStep != Step.MeasureImprovement || _baselineQueue < 5) return;
            if (queuedResidents <= _baselineQueue * .75f) CurrentStep = Step.OpenManage;
        }

        public void ObserveManageMode(bool open)
        {
            if (!_skipped && CurrentStep == Step.OpenManage && open)
                CurrentStep = Step.Complete;
        }

        public void Skip() => _skipped = true;

        public void Restart()
        {
            _skipped = false;
            _baselineWait = 0f;
            _baselineQueue = 0;
            CurrentStep = Step.NoticeQueue;
        }
    }
}
