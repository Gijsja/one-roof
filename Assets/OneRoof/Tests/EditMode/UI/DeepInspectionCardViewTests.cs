using NUnit.Framework;
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
    }
}
