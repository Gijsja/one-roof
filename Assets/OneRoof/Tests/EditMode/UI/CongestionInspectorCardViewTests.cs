using NUnit.Framework;
using OneRoof.Application.Inspectors;
using OneRoof.Application.Modes;
using OneRoof.UI.Inspectors;
using UnityEngine;

namespace OneRoof.UI.Tests.EditMode
{
    public sealed class CongestionInspectorCardViewTests
    {
        private GameObject _holder;
        private CongestionInspectorCardView _cardView;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("InspectorCardTestHolder");
            _cardView = _holder.AddComponent<CongestionInspectorCardView>();
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
        public void CardView_Inspect_OpensCardWithProjection()
        {
            var projection = new ElevatorCongestionInspectorProjection(
                floorLevel: 0,
                title: "Floor 0 Elevator Congestion",
                symptomDescription: "50 residents waiting in queue",
                contributingCauses: new[] { "Only 1 car running" },
                suggestedResponseAction: "Add capacity in Build mode");

            _cardView.Inspect(projection);

            Assert.That(_cardView.IsOpen, Is.True);
            Assert.That(_cardView.CurrentProjection, Is.SameAs(projection));

            _cardView.Close();
            Assert.That(_cardView.IsOpen, Is.False);
        }

        [Test]
        public void CardView_ExecuteDirectResponse_SwitchesSessionToBuildMode()
        {
            var session = new ModeShellSession();
            _cardView.Session = session;

            var projection = new ElevatorCongestionInspectorProjection(
                floorLevel: 0,
                title: "Floor 0 Elevator Congestion",
                symptomDescription: "50 residents waiting in queue",
                contributingCauses: new[] { "Capacity exceeded" },
                suggestedResponseAction: "Add elevator car in Build mode",
                canDirectRouteToBuild: true,
                targetBuildTool: "transit:elevator_car");

            _cardView.Inspect(projection);
            _cardView.ExecuteDirectResponse();

            Assert.That(_cardView.IsOpen, Is.False);
            Assert.That(session.CurrentMode, Is.EqualTo(InteractionMode.Build));
            Assert.That(session.Projection().SelectedBuildTool, Is.EqualTo("transit:elevator_car"));
        }
    }
}
