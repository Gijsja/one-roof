using NUnit.Framework;
using OneRoof.Domain.Weather;
using OneRoof.Presentation.Tower;
using UnityEngine;

namespace OneRoof.Presentation.Tests.EditMode
{
    /// <summary>Controller-to-weather-to-rain wiring; domain sampling rules have their own unit fixture.</summary>
    public sealed class WeatherSystemIntegrationTests
    {
        private GameObject _holder;
        private TowerPlayableController _controller;
        private GameObject _existingCamera;
        private bool _existingCameraWasActive;
        private GameObject _ownedCamera;

        [SetUp]
        public void SetUp()
        {
            _existingCamera = GameObject.Find("Tower Camera");
            if (_existingCamera != null)
            {
                _existingCameraWasActive = _existingCamera.activeSelf;
                _existingCamera.SetActive(false);
            }
            try
            {
                _holder = new GameObject("Weather integration tower");
                _controller = _holder.AddComponent<TowerPlayableController>();
                _controller.Initialize();
                _ownedCamera = GameObject.Find("Tower Camera");
            }
            catch
            {
                TearDown();
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_holder != null) Object.DestroyImmediate(_holder);
                var camera = _ownedCamera != null ? _ownedCamera : GameObject.Find("Tower Camera");
                if (camera != null && camera != _existingCamera) Object.DestroyImmediate(camera);
            }
            finally
            {
                if (_existingCamera != null) _existingCamera.SetActive(_existingCameraWasActive);
                _ownedCamera = null;
                _holder = null;
            }
        }

        [Test]
        public void AutoWeather_UsesControllerSeedAndSimulationTick_AndFeedsRainPresenter()
        {
            var expectedSeed = (ulong)(uint)_controller.SimulationSession.GetHashCode();
            if (expectedSeed == 0) expectedSeed = 0xDEADBEEFCAFEUL;
            Assert.That(_controller.WeatherCycle.WorldSeed, Is.EqualTo(expectedSeed));
            AssertAutoWeather();
            var initial = _controller.CurrentWeather;
            _controller.UpdateActiveWeather();
            Assert.That(_controller.CurrentWeather, Is.EqualTo(initial), "Repeated presentation refresh at the same tick must be stable.");
            _controller.SimulationSession.AdvanceOneTick();
            AssertAutoWeather();
        }

        [TestCase(WeatherCondition.Clear, 0f)]
        [TestCase(WeatherCondition.Drizzle, 0.25f)]
        [TestCase(WeatherCondition.Rain, 0.65f)]
        [TestCase(WeatherCondition.Storm, 1f)]
        [TestCase(WeatherCondition.Fog, 0.40f)]
        [TestCase(WeatherCondition.Snow, 0.50f)]
        public void ManualOverride_UpdatesControllerAndRain_ThenReturnsToAuto(WeatherCondition condition, float intensity)
        {
            _controller.SetWeatherOverride((int)condition);
            Assert.That(_controller.IsAutoWeather, Is.False);
            Assert.That(_controller.CurrentWeather.Condition, Is.EqualTo(condition));
            Assert.That(_controller.CurrentWeather.Intensity, Is.EqualTo(intensity));
            AssertRainMatchesController();
            _controller.SetWeatherOverride(-1);
            Assert.That(_controller.IsAutoWeather, Is.True);
            AssertAutoWeather();
        }

        private void AssertAutoWeather()
        {
            _controller.UpdateActiveWeather();
            var expected = _controller.WeatherCycle.Sample(_controller.SimulationSession.CurrentTick);
            Assert.That(_controller.CurrentWeather, Is.EqualTo(expected));
            AssertRainMatchesController();
        }

        private void AssertRainMatchesController()
        {
            Assert.That(_controller.RainPresenter, Is.Not.Null);
            Assert.That(_controller.RainPresenter.Condition, Is.EqualTo(_controller.CurrentWeather.Condition));
            Assert.That(_controller.RainPresenter.TargetIntensity, Is.EqualTo(_controller.CurrentWeather.Intensity));
            Assert.That(_controller.RainPresenter.WindSpeed, Is.EqualTo(_controller.CurrentWeather.WindSpeed));
        }
    }
}
