using UnityEngine;

namespace OneRoof.Presentation.Tower
{
    /// <summary>Bounded decorative animation; never changes resident or simulation state.</summary>
    public sealed class GoldCityEnvironment : MonoBehaviour
    {
        [SerializeField] private Transform[] _traffic;
        [SerializeField] private Transform[] _clouds;
        [SerializeField] private Transform[] _birds;
        [SerializeField] private Renderer[] _surfaces;
        private TowerPlayableController _controller;
        private MaterialPropertyBlock _properties;
        private float _clock;
        private float _lastLightTime = -1;
        private long _lastTick = -1;
        private float _lastWeather = -1;
        private static readonly int NightId = Shader.PropertyToID("_Night");
        private static readonly int WeatherId = Shader.PropertyToID("_Weather");
        private static readonly int ClockId = Shader.PropertyToID("_Clock");
        public int TrafficCount => _traffic?.Length ?? 0;
        public float AnimationClock => _clock;

        public void Bind(TowerPlayableController controller)
        {
            _controller = controller;
            _properties = new MaterialPropertyBlock();
            UpdateLighting();
        }

        private void Update()
        {
            if (_controller == null) return;
            if (_controller.IsPaused) { UpdateLighting(); return; }
            _clock += Time.deltaTime;
            for (var i = 0; i < _traffic.Length; i++)
            {
                var side = i < _traffic.Length / 2 ? -1 : 1;
                var local = i % (_traffic.Length / 2);
                var distance = Mathf.Repeat(local * 8.7f + _clock * (local % 2 == 0 ? 1.8f : -1.35f), 48f);
                _traffic[i].localPosition = new Vector3(side < 0 ? -17f-distance : 11f+distance,
                    -1.08f - (local % 2)*.4f, .8f-(local%2)*.1f);
            }
            for (var i = 0; i < _clouds.Length; i++)
            {
                var p = _clouds[i].localPosition;
                p.x = -110f + Mathf.Repeat(i * 31f + _clock * (.12f + i*.015f),220f);
                _clouds[i].localPosition = p;
            }
            for (var i = 0; i < _birds.Length; i++)
                _birds[i].localPosition = new Vector3(-90f + Mathf.Repeat(i*6f+_clock*1.2f,180f),
                    27f + i*.6f + Mathf.Sin(_clock*1.5f+i)*.25f, 10f);
            if (_clock - _lastLightTime > .2f) UpdateLighting();
        }

        private void UpdateLighting()
        {
            if (_controller?.SimulationSession == null) return;
            _properties ??= new MaterialPropertyBlock();
            var tick = _controller.SimulationSession.CurrentTick;
            var weather = _controller.CurrentWeather.Intensity;
            if (_clock - _lastLightTime < .2f && tick == _lastTick && weather == _lastWeather) return;
            _lastTick = tick;
            _lastWeather = weather;
            var phase = _controller.SimulationSession.DayPhase;
            var hour = phase.Hour + phase.Minute/60f;
            var night = hour < 5.5f || hour >= 20.5f ? 1f : hour < 6.5f ? 6.5f-hour : hour < 19.5f ? 0f : hour-19.5f;
            _properties.SetFloat(NightId, night);
            _properties.SetFloat(WeatherId, _controller.CurrentWeather.Intensity);
            _properties.SetFloat(ClockId, _clock);
            if (_surfaces != null)
            {
                for (var i = 0; i < _surfaces.Length; i++) if (_surfaces[i] != null) _surfaces[i].SetPropertyBlock(_properties);
            }
            _lastLightTime = _clock;
        }
    }
}
