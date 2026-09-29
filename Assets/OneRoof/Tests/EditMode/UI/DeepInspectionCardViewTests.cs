using NUnit.Framework;
using System.Collections.Generic;
using OneRoof.Application.Inspectors;
using OneRoof.UI.Inspectors;
using UnityEngine;

namespace OneRoof.UI.Tests.EditMode
{
    public sealed class DeepInspectionCardViewTests
    {
        private GameObject _holder;
        private DeepInspectionCardView _card;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("DeepInspectionCardTestHolder");
            _card = _holder.AddComponent<DeepInspectionCardView>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_holder);

        [Test]
        public void Inspect_OpensTheCardWithImmutableProjection()
        {
            var projection = new InspectorDetailProjection("Room #100", "At capacity.", new[] { "Occupancy: 4 / 4" }, "Build another room.");
            _card.Inspect(projection);

            Assert.That(_card.IsOpen, Is.True);
            Assert.That(_card.CurrentProjection, Is.SameAs(projection));
            _card.Close();
            Assert.That(_card.IsOpen, Is.False);
        }

        [Test]
        public void ResidentLink_RequestsOnlyAnOpenCardCounterpart()
        {
            var links = new List<ResidentInspectorLink> { new ResidentInspectorLink(7) };
            var projection = new InspectorDetailProjection("Resident #1", "Here", new string[0], "Observe", links);
            links.Clear();
            _card.Inspect(projection);
            var requested = 0;
            _card.ResidentInspectionRequested += id => requested = id;

            Assert.That(_card.TryInspectResidentLink(8), Is.False);
            Assert.That(_card.TryInspectResidentLink(7), Is.True);
            Assert.That(requested, Is.EqualTo(7));
            Assert.That(projection.ResidentLinks.Count, Is.EqualTo(1));
            _card.Close();
            Assert.That(_card.TryInspectResidentLink(7), Is.False);
        }
    }
}
