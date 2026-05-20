using UnityEngine;
using UnityEngine.UI;

namespace ForestFriendsQuest
{
    public enum CreatureGesture
    {
        None,
        HappyJump,
        JoySpin,
        ThankfulPulse
    }

    /// <summary>
    /// Emotional creature AI controller.
    ///
    /// Drives cozy wandering, gesture animations, sleep cycles, weather reactions,
    /// and trust-level-gated behavior. All particle feedback is routed through
    /// EmotionalParticleEngine — ZERO emoji text rendering anywhere in this class.
    ///
    /// Emotional states map to particle types:
    ///   Sleep     -> SleepMoonDust (soft moon-dust bubbles)
    ///   Happy     -> HappyGoldenWisp + HappyPollenBurst
    ///   Joy Spin  -> JoyFireflySpiral + JoySparkle
    ///   Thankful  -> ThankfulFlowerPetal + ThankfulBloomRing
    /// </summary>
    public class CreatureAIController : MonoBehaviour
    {
        // ─── Inspector Settings ───────────────────────────────────────────────────

        [Header("Identity")]
        public string creatureId;

        [Header("Wandering")]
        public bool  enableWandering = true;
        public float wanderSpeed     = 35f;
        public float wanderRadius    = 60f;
        public float minWaitTime     = 3f;
        public float maxWaitTime     = 8f;

        [Header("Gestures")]
        public float gestureDuration = 1.2f;

        // ─── System Links ─────────────────────────────────────────────────────────

        private DayNightWeatherController _timeController;
        private ProceduralAudioSystem     _audioSystem;
        private EmotionalBondingEngine    _bondingEngine;
        private EmotionalParticleEngine   _particles;   // replaces all emoji text rendering

        // ─── Wander State ─────────────────────────────────────────────────────────

        private RectTransform _rectTransform;
        private Vector2       _startPosition;
        private Vector2       _targetPosition;
        private float         _waitTimer;
        private bool          _isMoving;

        // ─── Gesture State ────────────────────────────────────────────────────────

        private CreatureGesture _activeGesture  = CreatureGesture.None;
        private float           _gestureTimer   = 0f;
        private Vector2         _gestureOffset  = Vector2.zero;
        private float           _gestureRotation = 0f;
        private Vector3         _gestureScale   = Vector3.one;

        // ─── Sleep State ──────────────────────────────────────────────────────────

        private bool  _isSleeping          = false;
        private float _sleepParticleTimer  = 0f;
        private const float SleepParticleInterval = 2.2f;

        // ─── Weather Reaction ─────────────────────────────────────────────────────

        private WeatherState _lastKnownWeather = WeatherState.Clear;
        private float        _weatherShelterX  = 0f;    // huddled X offset in rain/fog

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            if (_rectTransform != null)
            {
                _startPosition  = _rectTransform.anchoredPosition;
                _targetPosition = _startPosition;
            }
        }

        public void Initialize(
            DayNightWeatherController timeController,
            ProceduralAudioSystem     audioSystem,
            EmotionalBondingEngine    bondingEngine,
            EmotionalParticleEngine   particles)
        {
            _timeController = timeController;
            _audioSystem    = audioSystem;
            _bondingEngine  = bondingEngine;
            _particles      = particles;

            UpdateSleepState();
        }

        private void Update()
        {
            if (_rectTransform == null) return;

            UpdateSleepState();
            UpdateWeatherReaction();

            if (_isSleeping)
            {
                HandleSleepingBehaviors();
                return;
            }

            if (_activeGesture != CreatureGesture.None)
            {
                UpdateGestureAnimation();
            }
            else
            {
                // Reset visual transforms when idle
                _gestureOffset   = Vector2.zero;
                _gestureRotation = 0f;
                _gestureScale    = Vector3.one;

                if (enableWandering)
                {
                    UpdateWandering();
                }
            }

            ApplyTransforms();
        }

        // ─── Sleep ────────────────────────────────────────────────────────────────

        private void UpdateSleepState()
        {
            if (_timeController == null) return;

            var shouldSleep = _timeController.IsCreatureSleeping(creatureId);
            if (shouldSleep == _isSleeping) return;

            _isSleeping    = shouldSleep;
            _isMoving      = false;
            _activeGesture = CreatureGesture.None;

            var canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = _isSleeping ? 0.60f : 1f;

            // Gentle tilt to show sleeping posture — no emoji
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, _isSleeping ? -14f : 0f);

            if (_isSleeping)
            {
                _targetPosition = _startPosition;
            }
        }

        private void HandleSleepingBehaviors()
        {
            // Drift home slowly if sleep kicked in mid-wander
            if (Vector2.Distance(_rectTransform.anchoredPosition, _startPosition) > 1f)
            {
                _rectTransform.anchoredPosition = Vector2.MoveTowards(
                    _rectTransform.anchoredPosition,
                    _startPosition,
                    wanderSpeed * Time.deltaTime * 0.4f
                );
            }

            // Emit sleep moon-dust particles (replaces emoji Zzz bubbles entirely)
            _sleepParticleTimer += Time.deltaTime;
            if (_sleepParticleTimer >= SleepParticleInterval)
            {
                _sleepParticleTimer = 0f;

                if (_particles != null)
                {
                    var headPos = _rectTransform.anchoredPosition + new Vector2(
                        Random.Range(-12f, 12f), 75f
                    );
                    _particles.SpawnSleepParticles(headPos);
                }
            }
        }

        // ─── Weather Reaction ─────────────────────────────────────────────────────

        private void UpdateWeatherReaction()
        {
            if (_timeController == null) return;

            var weather = _timeController.CurrentWeather;
            if (weather == _lastKnownWeather) return;

            _lastKnownWeather = weather;

            switch (weather)
            {
                case WeatherState.Rainy:
                    // Huddle near home, reduce wander radius
                    _weatherShelterX = Random.Range(-15f, 15f);
                    wanderRadius = 20f;
                    break;
                case WeatherState.Foggy:
                    wanderRadius = 30f;
                    _weatherShelterX = 0f;
                    break;
                default:
                    wanderRadius = 60f;
                    _weatherShelterX = 0f;
                    break;
            }
        }

        // ─── Wandering ────────────────────────────────────────────────────────────

        private void UpdateWandering()
        {
            if (_isMoving)
            {
                var currentPos = _rectTransform.anchoredPosition;
                _rectTransform.anchoredPosition = Vector2.MoveTowards(
                    currentPos,
                    _targetPosition,
                    wanderSpeed * Time.deltaTime
                );

                if (Vector2.Distance(_rectTransform.anchoredPosition, _targetPosition) < 0.5f)
                {
                    _isMoving  = false;
                    _waitTimer = Random.Range(minWaitTime, maxWaitTime);
                }
            }
            else
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                    var dist  = Random.Range(10f, wanderRadius);
                    _targetPosition = _startPosition + new Vector2(
                        Mathf.Cos(angle) * dist + _weatherShelterX,
                        Mathf.Sin(angle) * dist
                    );
                    _isMoving = true;
                }
            }
        }

        // ─── Gesture Animations ───────────────────────────────────────────────────

        public void TriggerGesture(CreatureGesture gesture)
        {
            if (_isSleeping) return;

            _activeGesture = gesture;
            _gestureTimer  = 0f;

            if (_particles == null) return;

            var pos = _rectTransform.anchoredPosition + Vector2.up * 55f;

            switch (gesture)
            {
                case CreatureGesture.HappyJump:
                    // Sprite wisps + pollen — replaces emoji heart
                    _particles.SpawnHappyBurst(pos);
                    _audioSystem?.PlayContextChord(true);
                    break;

                case CreatureGesture.JoySpin:
                    // Firefly spirals + sparkle dust — replaces emoji sparkle
                    _particles.SpawnJoyBurst(pos);
                    _audioSystem?.PlayContextChord(true);
                    break;

                case CreatureGesture.ThankfulPulse:
                    // Flower petals + bloom rings — replaces emoji blossom
                    _particles.SpawnThankfulParticles(pos);
                    _audioSystem?.PlayContextChord(true);
                    break;
            }
        }

        private void UpdateGestureAnimation()
        {
            _gestureTimer += Time.deltaTime;
            var progress = Mathf.Clamp01(_gestureTimer / gestureDuration);

            switch (_activeGesture)
            {
                case CreatureGesture.HappyJump:
                    var hopAngle  = progress * Mathf.PI * 3f;
                    var hopHeight = Mathf.Abs(Mathf.Sin(hopAngle)) * 32f;
                    _gestureOffset = new Vector2(0f, hopHeight);
                    _gestureScale  = new Vector3(
                        1f + Mathf.Sin(hopAngle * 2f) * 0.08f,
                        1f - Mathf.Sin(hopAngle * 2f) * 0.08f,
                        1f
                    );
                    break;

                case CreatureGesture.JoySpin:
                    _gestureRotation = progress * 360f;
                    var spinHeight   = Mathf.Sin(progress * Mathf.PI) * 20f;
                    _gestureOffset   = new Vector2(0f, spinHeight);
                    break;

                case CreatureGesture.ThankfulPulse:
                    var pulse       = Mathf.Sin(progress * Mathf.PI * 2f);
                    var scaleFactor = 1f + pulse * 0.15f;
                    _gestureScale    = new Vector3(scaleFactor, scaleFactor, 1f);
                    _gestureRotation = Mathf.Sin(progress * Mathf.PI * 4f) * 8f;
                    break;
            }

            if (progress >= 1.0f)
            {
                _activeGesture = CreatureGesture.None;
            }
        }

        private void ApplyTransforms()
        {
            _rectTransform.localRotation = Quaternion.Euler(
                0f, 0f, _gestureRotation + (_isSleeping ? -14f : 0f)
            );
            _rectTransform.localScale = _gestureScale;

            if (_activeGesture != CreatureGesture.None)
            {
                _rectTransform.anchoredPosition =
                    (_isMoving ? _rectTransform.anchoredPosition : _targetPosition) + _gestureOffset;
            }
        }

        // ─── Tap Interaction ──────────────────────────────────────────────────────

        public void HandleTap()
        {
            if (_isSleeping)
            {
                // Creature is disturbed — emit soft moon-dust (replaces emoji Zzz)
                if (_particles != null)
                {
                    _particles.SpawnSleepParticles(
                        _rectTransform.anchoredPosition + Vector2.up * 60f
                    );
                }
                _audioSystem?.PlayContextChord(false);
                return;
            }

            // Pick a trust-gated random gesture
            var bondLevel  = GetBondLevel();
            var gestureMax = bondLevel >= 3 ? 3 : bondLevel >= 2 ? 2 : 1;
            var rand       = Random.Range(0, gestureMax);

            TriggerGesture(
                rand == 0 ? CreatureGesture.HappyJump :
                rand == 1 ? CreatureGesture.JoySpin   :
                            CreatureGesture.ThankfulPulse
            );

            // Bond grows with each interaction
            _bondingEngine?.AddTrust(creatureId, 5);
        }

        // ─── Bond Level ───────────────────────────────────────────────────────────

        private int GetBondLevel()
        {
            if (_bondingEngine == null) return 1;
            var state = _bondingEngine.GetBondState(creatureId);
            return state?.bondLevel ?? 1;
        }
    }

    // ─── Floating Effect (Sprite-Only, Zero Text/Emoji) ──────────────────────────

    /// <summary>
    /// Lightweight floating image component used for simple one-off sprite particles
    /// that don't need the full EmotionalParticleEngine pool (e.g. UI feedback spots).
    /// Does NOT use Text components — purely Image-based.
    /// </summary>
    public class FloatingEffect : MonoBehaviour
    {
        private float   _life = 1.4f;
        private float   _timer;
        private Vector2 _velocity;
        private Image   _image;
        private Color   _startColor;

        public void Initialize(Color baseColor, Sprite sprite = null)
        {
            _image = GetComponent<Image>();
            if (_image == null) _image = gameObject.AddComponent<Image>();

            if (sprite != null) _image.sprite = sprite;

            _startColor = baseColor;
            _image.color = _startColor;
            _velocity    = new Vector2(Random.Range(-28f, 28f), Random.Range(50f, 80f));
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            var progress = _timer / _life;

            var rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition += _velocity * Time.deltaTime;
                _velocity.y           += 8f * Time.deltaTime;  // gentle upward acceleration
            }

            if (_image != null)
            {
                _image.color = new Color(
                    _startColor.r,
                    _startColor.g,
                    _startColor.b,
                    Mathf.Lerp(_startColor.a, 0f, progress)
                );
            }

            if (progress >= 1.0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
