using System.Collections;
using NUnit.Framework;
using OneRoof.Presentation.Tower;
using UnityEngine;
using UnityEngine.TestTools;

namespace OneRoof.Tests.PlayMode
{
    public sealed class OutsideCityPlayModeTests
    {
        [UnityTest]
        public IEnumerator TowerControllerCreatesParallaxCityWithoutBlockingInput()
        {
            var holder = new GameObject("Outside City PlayMode Test");
            try
            {
                holder.AddComponent<TowerPlayableController>();
                yield return null;

                var city = holder.GetComponent<OutsideCityPresenter>();
                Assert.That(city, Is.Not.Null);
                Assert.That(city.LayerCount, Is.EqualTo(3));
                Assert.That(city.FacadeCount, Is.GreaterThan(20));
                var cityRoot = holder.transform.Find("Outside City View");
                Assert.That(cityRoot, Is.Not.Null);
                Assert.That(cityRoot.GetComponentsInChildren<Collider>(true), Is.Empty);

                var camera = GameObject.Find("Tower Camera").GetComponent<TowerCameraController>();
                var start = city.Layers[2].localPosition.x;
                camera.PanBy(new Vector2(5f, 0f));
                yield return null;
                Assert.That(city.Layers[2].localPosition.x - start, Is.GreaterThan(1f));
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
