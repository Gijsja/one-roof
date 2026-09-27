using System.Collections.Generic;
using NUnit.Framework;
using OneRoof.Application.Transit;
using OneRoof.Presentation.Population;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    public sealed class UndergroundPresentationCapTests
    {
        [Test]
        public void ThreeHundredProjectedResidents_UseAtMostFortyPooledViews()
        {
            var root = new GameObject("ThreeHundredResidentCapTest");
            var pool = root.AddComponent<NpcViewPool>();
            var presenter = new TowerResidentPresenter { ViewPool = pool };
            try
            {
                presenter.Initialize(root.transform);
                presenter.EnsureResidentViews(300);
                var residents = new List<TransitResidentProjection>(300);
                for (var id = 1; id <= 300; id++)
                    residents.Add(new TransitResidentProjection(id, 0, TransitResidentStatus.Walking));
                var snapshot = new TowerProjection(0, 0, 0, 0f, residents, new List<ElevatorProjection>());

                presenter.UpdateResidentPositions(snapshot, null, 0f);

                Assert.That(snapshot.Residents.Count, Is.EqualTo(300));
                Assert.That(pool.ActiveCount, Is.LessThanOrEqualTo(NpcViewPool.DefaultMaxCapacity));
                Assert.That(presenter.ResidentViews.Count, Is.LessThanOrEqualTo(NpcViewPool.DefaultMaxCapacity));
            }
            finally
            {
                presenter.Clear();
                Object.DestroyImmediate(root);
            }
        }
    }
}
