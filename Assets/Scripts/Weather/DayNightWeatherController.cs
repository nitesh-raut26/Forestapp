using System;
using UnityEngine;

namespace ForestFriendsQuest
{
    public enum TimeOfDay
    {
        Morning,
        Afternoon,
        Sunset,
        Dusk,
        Night
    }

    public enum WeatherState
    {
        Clear,
        Sunny,
        Rainy,
        Foggy,
        MeteorShower,
        Eclipse,
        Misty,       // light morning mist — used in Spring
        Snowy,       // snowfall — used in Winter
        Stormy,      // heavy storm with wind
        Windy        // breezy, leaves blowing — used in Autumn
    }

    public class DayNightWeatherController : MonoBehaviour
    {
        private float _timeOfDayValue = 0.2f; // 0.0 to 1.0
        private float _cycleSpeed     = 0.005f;

        private TimeOfDay    _currentTimeOfDay = TimeOfDay.Morning;
        private WeatherState _currentWeather   = WeatherState.Sunny;
        private string       _weatherName      = "Sunlight";

        // ─── Transition smoothing ─────────────────────────────────────────────────
        private Color _currentSkyColor;
        private Color _targetSkyColor;
        private const float SkyLerpSpeed = 1.2f;   // units/sec — smooth cross-fade

        // ─── Optional audio bridge (set by ForestSystemsContainer) ────────────────
        private ProceduralAudioSystem _audioSystem;

        public TimeOfDay    CurrentTime    => _currentTimeOfDay;
        public WeatherState CurrentWeather => _currentWeather;
        public string       WeatherName    => _weatherName;
        public float        TimeValue      => _timeOfDayValue;
        /// <summary>Current time as a 24-hour float (0–23.99). Derived from TimeValue.</summary>
        public float        CurrentHour    => _timeOfDayValue * 24f;
        /// <summary>Smoothly blended sky overlay colour — use this for camera tints.</summary>
        public Color        SmoothedSkyColor => _currentSkyColor;

        public event Action<TimeOfDay> OnTimeChanged;
        public event Action<WeatherState> OnWeatherChanged;

        // ─── Public bridge ────────────────────────────────────────────────────────

        /// <summary>Called by ForestSystemsContainer to wire in the audio system.</summary>
        public void SetAudioBridge(ProceduralAudioSystem audioSystem)
        {
            _audioSystem = audioSystem;
        }

        /// <summary>Override sky and fog tint for the current biome zone.</summary>
        public void SetBiomeTint(Color fogColor, Color ambientLightColor)
        {
            _targetSkyColor = Color.Lerp(_currentSkyColor, fogColor, 0.35f);
            // ambientLightColor stored for future lighting integration
        }

        private void Awake()
        {
            _currentSkyColor = GetSkyOverlayColor();
            _targetSkyColor  = _currentSkyColor;
        }

        private void Update()
        {
            AdvanceTime(Time.deltaTime * _cycleSpeed);

            // Smooth-lerp sky colour every frame
            _currentSkyColor = Color.Lerp(_currentSkyColor, _targetSkyColor, Time.deltaTime * SkyLerpSpeed);
        }

        public void SetCycleSpeed(float speed)
        {
            _cycleSpeed = speed;
        }

        public void AdvanceTime(float delta)
        {
            var oldVal = _timeOfDayValue;
            _timeOfDayValue = (_timeOfDayValue + delta) % 1.0f;

            var oldTime = _currentTimeOfDay;
            if (_timeOfDayValue < 0.25f) _currentTimeOfDay = TimeOfDay.Morning;
            else if (_timeOfDayValue < 0.5f) _currentTimeOfDay = TimeOfDay.Afternoon;
            else if (_timeOfDayValue < 0.75f) _currentTimeOfDay = TimeOfDay.Sunset;
            else _currentTimeOfDay = TimeOfDay.Night;

            if (oldTime != _currentTimeOfDay)
            {
                OnTimeChanged?.Invoke(_currentTimeOfDay);
                TriggerDailyWeatherRotation();

                // Update sky target for smooth transition
                _targetSkyColor = GetSkyOverlayColor();

                // Audio bridge — play mood chord on time-of-day shift
                _audioSystem?.PlayContextChord(_currentTimeOfDay == TimeOfDay.Morning ||
                                               _currentTimeOfDay == TimeOfDay.Afternoon);
            }
        }

        public void SetTimeOfDay(TimeOfDay time)
        {
            _currentTimeOfDay = time;
            switch (time)
            {
                case TimeOfDay.Morning: _timeOfDayValue = 0.1f; break;
                case TimeOfDay.Afternoon: _timeOfDayValue = 0.35f; break;
                case TimeOfDay.Sunset: _timeOfDayValue = 0.6f; break;
                case TimeOfDay.Night: _timeOfDayValue = 0.85f; break;
            }
            OnTimeChanged?.Invoke(_currentTimeOfDay);
        }

        private void TriggerDailyWeatherRotation()
        {
            // Weather changes during day/night cycles to simulate variety
            var rand = UnityEngine.Random.Range(0, 100);
            var oldWeather = _currentWeather;

            if (rand < 40)
            {
                _currentWeather = WeatherState.Sunny;
                _weatherName = "Sunlight";
            }
            else if (rand < 70)
            {
                _currentWeather = WeatherState.Rainy;
                _weatherName = "Soft Drizzle";
            }
            else if (rand < 90)
            {
                _currentWeather = WeatherState.Foggy;
                _weatherName = "Misty Parallax Fog";
            }
            else if (rand < 96)
            {
                _currentWeather = WeatherState.MeteorShower;
                _weatherName = "Rare Star Fall";
            }
            else
            {
                _currentWeather = WeatherState.Eclipse;
                _weatherName = "Solar Eclipse";
            }

            if (oldWeather != _currentWeather)
            {
                OnWeatherChanged?.Invoke(_currentWeather);

                // Audio bridge — upbeat tone for good weather, minor for storm/fog
                var isPositive = _currentWeather == WeatherState.Sunny ||
                                 _currentWeather == WeatherState.Clear ||
                                 _currentWeather == WeatherState.MeteorShower;
                _audioSystem?.PlayContextChord(isPositive);
            }
        }

        public void ForceWeather(WeatherState state)
        {
            _currentWeather = state;
            switch (state)
            {
                case WeatherState.Clear: _weatherName = "Clear Skies"; break;
                case WeatherState.Sunny: _weatherName = "Bright Sun"; break;
                case WeatherState.Rainy: _weatherName = "Rhythm Rain"; break;
                case WeatherState.Foggy: _weatherName = "Cozy Fog"; break;
                case WeatherState.MeteorShower: _weatherName = "Meteor Shower"; break;
                case WeatherState.Eclipse: _weatherName = "Eclipse"; break;
            }
            OnWeatherChanged?.Invoke(_currentWeather);
        }

        public Color GetSkyOverlayColor()
        {
            // Twilight, soft orange, moonlight blue
            if (_currentTimeOfDay == TimeOfDay.Morning)
            {
                return new Color(0.9f, 0.76f, 0.58f, 0.08f); // Soft gold morning
            }
            else if (_currentTimeOfDay == TimeOfDay.Afternoon)
            {
                return new Color(1f, 1f, 1f, 0f); // Clear day
            }
            else if (_currentTimeOfDay == TimeOfDay.Sunset)
            {
                return new Color(0.94f, 0.44f, 0.28f, 0.16f); // Warm sunset red/amber
            }
            else
            {
                return new Color(0.04f, 0.08f, 0.24f, 0.48f); // Soothing dream forest night dark blue
            }
        }

        public bool IsCreatureSleeping(string characterId)
        {
            // Pip: Fox. Mimi: Bird. Tomo: Turtle. Luma: Firefly. Sol: Owl.
            // Sol the owl is awake only at night, sleeps during the day
            if (characterId == "sol")
            {
                return _currentTimeOfDay != TimeOfDay.Night;
            }

            // Normal forest creatures sleep at night
            if (_currentTimeOfDay == TimeOfDay.Night)
            {
                // Fireflies are awake at night!
                if (characterId == "luma") return false;
                return true;
            }

            return false;
        }

        public string GetCreatureSleepingPose(string characterId)
        {
            if (IsCreatureSleeping(characterId))
            {
                return " (sleeping...)";
            }
            return "";
        }
    }
}
