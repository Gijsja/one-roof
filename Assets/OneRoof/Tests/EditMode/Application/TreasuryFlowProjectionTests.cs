using NUnit.Framework;
using OneRoof.Application.Economy;

namespace OneRoof.Application.Tests.EditMode
{
    [TestFixture]
    public sealed class TreasuryFlowProjectionTests
    {
        [Test]
        public void TreasuryFlowProjection_TracksDailyNetFlowsAccurately()
        {
            var flow = new TreasuryFlowProjection(
                rent: 150,
                tax: 30,
                upkeep: 40,
                subsidy: 40,
                construction: 1000,
                constructionSalvage: 500);

            Assert.That(flow.Rent, Is.EqualTo(150));
            Assert.That(flow.Tax, Is.EqualTo(30));
            Assert.That(flow.Upkeep, Is.EqualTo(40));
            Assert.That(flow.Subsidy, Is.EqualTo(40));
            Assert.That(flow.Construction, Is.EqualTo(1000));
            Assert.That(flow.ConstructionSalvage, Is.EqualTo(500));
            Assert.That(flow.Net, Is.EqualTo(-400)); // 150 + 30 + 500 - 40 - 40 - 1000 = -400
        }

        [Test]
        public void TreasuryFlowProjection_DefaultSalvageIsZero()
        {
            var flow = new TreasuryFlowProjection(
                rent: 100,
                tax: 20,
                upkeep: 30,
                subsidy: 10,
                construction: 50);

            Assert.That(flow.ConstructionSalvage, Is.EqualTo(0));
            Assert.That(flow.Net, Is.EqualTo(30)); // 100 + 20 - 30 - 10 - 50 = 30
        }
    }
}
