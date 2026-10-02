using NUnit.Framework;
using OneRoof.Domain.Identity;
using OneRoof.Domain.Trips;

namespace OneRoof.Domain.Tests.EditMode
{
    public sealed class TransitDemandObserverTests
    {
        [Test]
        public void TripLookupIsRebuiltAndRemovedAtTerminalOutcome()
        {
            var observer = new TransitDemandObserver(20);
            long demand=observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1, 1);
            observer.AcceptTrip(demand, 55);
            observer=TransitDemandObserver.FromSaveData(observer.ToSaveData());
            Assert.That(observer.FindSequenceForTrip(55), Is.EqualTo(demand));
            observer.Complete(demand, 2);
            Assert.That(observer.FindSequenceForTrip(55), Is.Zero);
        }
        [Test]
        public void TimedOutQueueStillContributesLiveTail()
        {
            var observer = new TransitDemandObserver(20);
            long demand=observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1, 1);
            observer.AcceptTrip(demand, 55);
            observer.Advance(361);
            Assert.That(observer.FindSequenceForTrip(55), Is.Zero);
            observer.ObserveLiveWait(900);
            observer.MarkObservationComplete();
            Assert.That(observer.Current.liveMaximum, Is.EqualTo(900));
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Failed));
        }
        [Test]
        public void SaveAndRestoreDoNotAliasMutableHistogramsOrEpisodes()
        {
            var observer = new TransitDemandObserver(20);
            long demand=observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1, 1);
            observer.AcceptTrip(demand, 55);
            var save=observer.ToSaveData();
            var restored=TransitDemandObserver.FromSaveData(save);
            save.current.waitHistogram[10]=999;
            save.episodes[0].tripId=88;
            Assert.That(observer.Current.waitHistogram[10], Is.Zero);
            Assert.That(restored.Current.waitHistogram[10], Is.Zero);
            Assert.That(restored.FindSequenceForTrip(55), Is.EqualTo(demand));
        }
        [TestCase(-1, 0)]
        [TestCase(55, 99)]
        public void CorruptTripIdentityOrPurposeIsRejected(long tripId, int purpose)
        {
            var observer = new TransitDemandObserver(20);
            observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1, 1);
            var save=observer.ToSaveData();
            save.episodes[0].tripId=tripId;
            save.episodes[0].purpose=(TripPurpose)purpose;
            Assert.Throws<System.ArgumentException>(() => TransitDemandObserver.FromSaveData(save));
        }
        [Test]
        public void RestoredMissingObservationCannotBecomeHealthy()
        {
            var observer=TransitDemandObserver.FromSaveData(new TransitDemandObserver(20).ToSaveData());
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Unavailable));
        }
        [Test]
        public void MissingObservationDoesNotPassZeroDemand()
        {
            var observer = new TransitDemandObserver(20);
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Unavailable));
            observer.MarkObservationComplete();
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Passed));
        }
        [Test]
        public void RejectedRetriesCountOnceAndLocalCompletionCounts()
        {
            var observer = new TransitDemandObserver(20);
            long failed = observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 10, 10);
            observer.Reject(failed, 10);
            Assert.That(observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 10, 11), Is.EqualTo(failed));
            long local = observer.ObserveAttempt(new EntityId(2), TripPurpose.Home, 10, 10);
            observer.Complete(local, 10);
            observer.MarkObservationComplete();
            Assert.That(observer.Current.attempted, Is.EqualTo(2));
            Assert.That(observer.Current.failed, Is.EqualTo(1));
            Assert.That(observer.Current.succeeded, Is.EqualTo(1));
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Failed));
        }
        [Test]
        public void InclusiveDeadlineSurvivesPendingDaySaveAndFinalizesOnce()
        {
            var observer = new TransitDemandObserver(20);
            long sequence = observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1439, 1439);
            observer.MarkObservationComplete();
            Assert.That(observer.Advance(1440), Is.Null);
            observer = TransitDemandObserver.FromSaveData(observer.ToSaveData());
            observer.Complete(sequence, 1799);
            var day = observer.Advance(1799);
            Assert.That(day.Status, Is.EqualTo(DemandGateStatus.Passed));
            Assert.That(observer.LastFinalizedDay, Is.EqualTo(0));
            Assert.That(observer.Advance(1800), Is.Null);
        }
        [Test]
        public void BeginTickRotatesBeforeAttemptWithoutExpiringInclusiveCompletion()
        {
            var observer = new TransitDemandObserver(20);
            long prior = observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1080, 1080);
            observer.MarkObservationComplete();
            observer.BeginTick(1440);
            long next = observer.ObserveAttempt(new EntityId(2), TripPurpose.Home, 1440, 1440);
            observer.Complete(prior, 1440);
            observer.Complete(next, 1440);
            Assert.That(observer.Advance(1440).succeeded, Is.EqualTo(1));
            Assert.That(observer.Current.attempted, Is.EqualTo(1));
        }
        [Test]
        public void DiversionWaitAndLiveTailRemainFailureEvidence()
        {
            var observer = new TransitDemandObserver(20);
            long sequence = observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1, 1);
            observer.ObserveQueueWait(sequence, 241);
            observer.Complete(sequence, 250);
            observer.MarkObservationComplete();
            Assert.That(observer.Current.P95Wait, Is.EqualTo(241));
            Assert.That(observer.Current.liveMaximum, Is.EqualTo(241));
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Failed));
        }
        [Test]
        public void UnresolvedRetriesCannotDiluteFailureAndCapacityCannotDropSilently()
        {
            var observer = new TransitDemandObserver(1);
            long sequence = observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 1, 1);
            Assert.That(observer.ObserveAttempt(new EntityId(1), TripPurpose.Work, 2, 2), Is.EqualTo(sequence));
            Assert.That(observer.ObserveAttempt(new EntityId(2), TripPurpose.Work, 2, 2), Is.Zero);
            observer.Advance(361);
            observer.MarkObservationComplete();
            Assert.That(observer.Current.failed, Is.EqualTo(1));
            Assert.That(observer.Current.Status, Is.EqualTo(DemandGateStatus.Unavailable));
        }
    }
}
