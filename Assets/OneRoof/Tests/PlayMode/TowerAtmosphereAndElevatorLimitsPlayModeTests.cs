using System.Collections;
using NUnit.Framework;
using OneRoof.Domain.Transit;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class TowerAtmosphereAndElevatorLimitsPlayModeTests
    {
        [UnityTest]
        public IEnumerator TowerPresentation_ShowsWindowLightConesAndKeepsThreeCarsInsideTheShaft()
        {
            var holder = new GameObject("Tower Atmosphere And Elevator Limits Test");
            try
            {
                var controller = holder.AddComponent<TowerPlayableController>();
                yield return null;

                var atmosphere = controller.AtmospherePresenter;
                Assert.That(atmosphere.WindowLightCount, Is.GreaterThan(0));
                foreach (var renderer in atmosphere.WindowLightRenderers)
                {
                    Assert.That(renderer.enabled, Is.True);
                    Assert.That(renderer.transform.position.z, Is.LessThan(0.7f));
                }

                Assert.That(controller.TransitSession.AddCapacity().Accepted, Is.True);
                Assert.That(controller.TransitSession.AddCapacity().Accepted, Is.True);
                Assert.That(controller.TransitSession.AddCapacity().Accepted, Is.False);
                Assert.That(controller.TransitSession.ElevatorCarCount, Is.EqualTo(ElevatorBank.MaxCarsPerBank));

                controller.SyncPresenterGeometry();
                Assert.That(controller.ElevatorPresenter.ElevatorViews.Count, Is.EqualTo(3));
                for (var i = 0; i < controller.ElevatorPresenter.ElevatorViews.Count; i++)
                {
                    var car = controller.ElevatorPresenter.ElevatorViews[i];
                    var left = car.transform.position.x - car.transform.localScale.x * 0.5f;
                    var right = car.transform.position.x + car.transform.localScale.x * 0.5f;
                    Assert.That(left, Is.GreaterThanOrEqualTo(-2.36f));
                    Assert.That(right, Is.LessThanOrEqualTo(-1.44f));
                }
            }
            finally
            {
                Object.Destroy(holder);
                var camera = GameObject.Find("Tower Camera");
                if (camera != null) Object.Destroy(camera);
            }
        }
    }
}
