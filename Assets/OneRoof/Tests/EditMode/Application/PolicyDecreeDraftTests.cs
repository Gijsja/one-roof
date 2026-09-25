using NUnit.Framework;
using OneRoof.Application.Management;
using OneRoof.Application.Tower;
using OneRoof.Domain.Population;

namespace OneRoof.Application.Tests.EditMode
{
    public sealed class PolicyDecreeDraftTests
    {
        [Test]
        public void DraftDoesNotMutateUntilConfirm_AndNoOpIsRejected()
        {
            var session = new TowerSimulationSession();
            var draft = new PolicyDecreeDraft(session);
            var initialVersion = session.Version;
            var original = draft.Current;
            Assert.That(draft.CanConfirm().Accepted, Is.False);
            Assert.That(draft.CanConfirm().Rejections[0].Code.Value, Is.EqualTo("policy:no_change"));
            draft.SelectRent(.7f);
            draft.SelectTax(.2f);
            draft.SelectTransitSubsidy(true);
            draft.SelectQuietHours(true);
            Assert.That(draft.Current, Is.SameAs(original));
            Assert.That(session.Version, Is.EqualTo(initialVersion));
            Assert.That(draft.CanConfirm().Accepted, Is.True);
            Assert.That(draft.Confirm().Accepted, Is.True);
            Assert.That(draft.Current.RentCapMultiplier, Is.EqualTo(.7f));
            Assert.That(draft.Current.CommercialTaxRate, Is.EqualTo(.2f));
            Assert.That(draft.Current.TransitSubsidyEnabled, Is.True);
            Assert.That(draft.Current.QuietHoursEnabled, Is.True);
            Assert.That(draft.Confirm().Accepted, Is.False);
            Assert.That(draft.Receipt, Does.Contain("DECREE ENACTED"));
        }

        [Test]
        public void ReceiptDistinguishesImmediateFromObservedSettlement()
        {
            var session = new TowerSimulationSession();
            var draft = new PolicyDecreeDraft(session);
            draft.SelectRent(.7f);
            Assert.That(draft.Estimate(), Does.Contain("estimate"));
            Assert.That(draft.Confirm().Accepted, Is.True);
            Assert.That(draft.SettlementReceipt(), Does.Contain("pending settlement"));
            for (var i = 0; i <= DailySchedule.TicksPerDay; i++) session.AdvanceOneTick();
            Assert.That(draft.SettlementReceipt(), Does.Contain("OBSERVED SINCE ENACTMENT"));
        }
    }
}
