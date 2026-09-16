using NUnit.Framework;
using OneRoof.Application.Overlays;
using OneRoof.Application.Prediction;
using OneRoof.UI.Prediction;
using UnityEngine;

namespace OneRoof.UI.Tests.EditMode
{
    public sealed class PlacementPreviewCardViewTests
    {
        private GameObject _holder;
        private PlacementPreviewCardView _cardView;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("PlacementPreviewTestHolder");
            _cardView = _holder.AddComponent<PlacementPreviewCardView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void PlacementCard_SetPreview_BindsAndOpens()
        {
            var projection = new ElevatorPlacementPreviewProjection(
                itemName: "Elevator Car",
                cost: 500,
                isValid: true,
                invalidReason: string.Empty,
                currentCarCount: 1,
                predictedCarCount: 2,
                currentAverageWaitTicks: 32.5f,
                predictedAverageWaitTicks: 16.3f,
                currentMaxWaitTicks: 70,
                predictedMaxWaitTicks: 35,
                currentSeverity: CongestionTier.Severe,
                predictedSeverity: CongestionTier.Clear,
                estimatedImprovementPercentage: 50f,
                confidence: PredictionConfidence.High,
                confidenceLabel: "High confidence (deterministic schedule model)");

            _cardView.SetPreview(projection);

            Assert.That(_cardView.IsOpen, Is.True);
            Assert.That(_cardView.CurrentProjection, Is.SameAs(projection));

            _cardView.Close();
            Assert.That(_cardView.IsOpen, Is.False);
        }

        [Test]
        public void PlacementCard_Confirm_InvokesCallbackAndCloses()
        {
            var confirmed = false;
            var projection = new ElevatorPlacementPreviewProjection(
                itemName: "Elevator Car",
                cost: 500,
                isValid: true,
                invalidReason: string.Empty,
                currentCarCount: 1,
                predictedCarCount: 2,
                currentAverageWaitTicks: 30f,
                predictedAverageWaitTicks: 15f,
                currentMaxWaitTicks: 60,
                predictedMaxWaitTicks: 30,
                currentSeverity: CongestionTier.Severe,
                predictedSeverity: CongestionTier.Clear,
                estimatedImprovementPercentage: 50f,
                confidence: PredictionConfidence.High,
                confidenceLabel: "High confidence");

            _cardView.SetPreview(projection, () => confirmed = true);
            _cardView.Confirm();

            Assert.That(confirmed, Is.True);
            Assert.That(_cardView.IsOpen, Is.False);
        }
    }
}
