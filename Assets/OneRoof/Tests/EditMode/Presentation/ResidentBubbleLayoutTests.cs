using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Presentation.Population;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class ResidentBubbleLayoutTests
    {
        private readonly ResidentBubbleLayout _layout = new ResidentBubbleLayout();
        private readonly List<int> _visible = new List<int>();

        [Test]
        public void InspectedResidentWinsOverlappingEmotionalBubble()
        {
            var candidates = new List<ResidentBubbleLayoutCandidate>
            {
                Bubble(1, 0, 100), Bubble(2, 0, 0, true), Bubble(3, 100, 50)
            };
            _layout.SelectVisible(candidates, 8, _visible);
            Assert.That(_visible, Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void EmotionalPriorityThenStableIdDeterminesSelectionRegardlessOfInputOrder()
        {
            var candidates = new List<ResidentBubbleLayoutCandidate>
            {
                Bubble(9, 0, 1), Bubble(4, 100, 5), Bubble(2, 200, 5)
            };
            _layout.SelectVisible(candidates, 2, _visible);
            Assert.That(_visible, Is.EqualTo(new[] { 2, 4 }));
            candidates.Reverse();
            _layout.SelectVisible(candidates, 2, _visible);
            Assert.That(_visible, Is.EqualTo(new[] { 2, 4 }));
        }

        [Test]
        public void SpacingSuppressesNearbyBubblesButAllowsExactGap()
        {
            var candidates = new List<ResidentBubbleLayoutCandidate>
            {
                Bubble(1, 0), Bubble(2, 25), Bubble(3, 26)
            };
            _layout.SelectVisible(candidates, 8, _visible, 6);
            Assert.That(_visible, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void InvalidAndClippedRectanglesAreSuppressed()
        {
            var candidates = new List<ResidentBubbleLayoutCandidate>
            {
                new ResidentBubbleLayoutCandidate(1, new Rect(float.NaN, 0, 20, 20), false, 0),
                new ResidentBubbleLayoutCandidate(2, new Rect(0, 0, -20, 20), false, 0),
                new ResidentBubbleLayoutCandidate(3, new Rect(-1, 0, 20, 20), true, 0),
                new ResidentBubbleLayoutCandidate(4, new Rect(90, 0, 20, 20), false, 0),
                Bubble(5, 0)
            };
            _layout.SelectVisible(candidates, 8, _visible, viewport: new Rect(0, 0, 100, 100));
            Assert.That(_visible, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void RepeatedCallsClearPreviousResultsAndZeroBudgetHidesAll()
        {
            var candidates = new List<ResidentBubbleLayoutCandidate> { Bubble(1, 0) };
            _layout.SelectVisible(candidates, 8, _visible);
            candidates.Clear();
            candidates.Add(Bubble(2, 0));
            _layout.SelectVisible(candidates, 8, _visible);
            Assert.That(_visible, Is.EqualTo(new[] { 2 }));
            _layout.SelectVisible(candidates, 0, _visible);
            Assert.That(_visible, Is.Empty);
        }

        [Test]
        public void DuplicateResidentIdsProduceOneBubble()
        {
            var candidates = new List<ResidentBubbleLayoutCandidate> { Bubble(1, 0), Bubble(1, 100) };
            _layout.SelectVisible(candidates, 8, _visible);
            Assert.That(_visible, Is.EqualTo(new[] { 1 }));
        }

        private static ResidentBubbleLayoutCandidate Bubble(int id, float x, int priority = 0, bool inspected = false)
            => new ResidentBubbleLayoutCandidate(id, new Rect(x, 0, 20, 20), inspected, priority);
    }
}
