using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Splines;

namespace WindCompute
{
    [ExecuteInEditMode]
    public class SplineFollower : MonoBehaviour
    {
        [SerializeField] private SplineContainer _container;
        [SerializeField] private float _speed;
        [SerializeField] [Range(0, 1)] private float _phase;

        [SerializeField] [MinMaxSlider(0, 1)] private Vector2 _range = new(0, 1);

        [SerializeField] private Vector3 _localOffset;

        [SerializeField] private Direction _direction = Direction.Forward;
        [SerializeField] private MovementType _movementType;
        [SerializeField] private Mode _mode;

        [SerializeField] private bool _startOnAwake;

        public float Speed => _speed;
        public float CurrentDirection => _currentDirection;
        public Vector3 CurrentNormal => _currentNormal;

        public event Action BeginReached;
        public event Action EndReached;

        private float _length;
        private float _currentDirection;
        private float _currentPhase;
        private Vector3 _currentNormal;

        private bool _isFollowing;
        private bool _isInitialized;

        private Rigidbody2D _rigidbody;

        private void OnEnable()
        {
            _isInitialized = false;
            Initialize();

            if (Application.isPlaying && _startOnAwake)
            {
                StartFollow();
            }
        }

        private void OnValidate()
        {
            _isInitialized = false;

            Initialize();

            if (!_isInitialized)
                return;

            float phaseInRange = Mathf.LerpUnclamped(_range.x, _range.y, _phase);
            Move(phaseInRange, MovementType.Transform);
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            if (_container == null)
                return;

            _length = _container.Spline.GetLength();
            _length *= _range.y - _range.x;

            _currentPhase = _phase;
            _currentDirection = (float) _direction;

            if (_movementType == MovementType.Rigidbody)
            {
                _rigidbody = GetComponent<Rigidbody2D>();

                if (_rigidbody == null)
                    return;
            }

            Move(_phase, MovementType.Transform);

            _isInitialized = true;
        }

        public void StartFollow()
        {
            Initialize();

            _isFollowing = true;
        }

        public float GetCurrentSpeed()
        {
            if (!_isFollowing)
                return 0;

            return _speed * _currentDirection;
        }

        public void StopFollow()
        {
            _isFollowing = false;
        }

        private void FixedUpdate()
        {
            if (!_isFollowing)
                return;

            if (_movementType == MovementType.Rigidbody)
            {
                Process();
            }
        }

        private void Update()
        {
            if (!_isFollowing)
                return;

            if (_movementType == MovementType.Transform)
            {
                Process();
            }
        }

        private void Process()
        {
            _currentPhase += _speed * _currentDirection * Time.deltaTime / _length;

            float phase = Mathf.Clamp01(_currentPhase);
            float phaseInRange = Mathf.LerpUnclamped(_range.x, _range.y, phase);

            Move(phaseInRange, _movementType);
            PostProcess(phase);
        }

        private void Move(float phase, MovementType type)
        {
            Vector3 position = _container.EvaluatePosition(phase);
            _currentNormal = _container.EvaluateUpVector(phase);

            position += _localOffset;

            switch (type)
            {
                case MovementType.Transform:
                    transform.position = position;
                    break;
                case MovementType.Rigidbody:
                    _rigidbody.position = position;
                    break;
            }
        }

        private void PostProcess(float phase)
        {
            if (_currentDirection > 0)
            {
                if (phase >= 1)
                {
                    EndReached?.Invoke();

                    switch (_mode)
                    {
                        case Mode.Once:
                            _isFollowing = false;
                            break;
                        case Mode.Loop:
                            _currentPhase = 0;
                            break;
                        case Mode.PingPong:
                            _currentDirection = -1 * _currentDirection;
                            break;
                    }
                }
            }
            else
            {
                if (phase <= 0)
                {
                    BeginReached?.Invoke();

                    switch (_mode)
                    {
                        case Mode.Once:
                            _isFollowing = false;
                            break;
                        case Mode.Loop:
                            _currentPhase = 1;
                            break;
                        case Mode.PingPong:
                            _currentDirection = -1 * _currentDirection;
                            break;
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            var startPoint = _container.EvaluatePosition(_range.x);
            var endPoint = _container.EvaluatePosition(_range.y);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(startPoint, 0.1f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(endPoint, 0.1f);
        }

        private enum Direction
        {
            Forward = 1,
            Backward = -1
        }

        private enum MovementType
        {
            Transform,
            Rigidbody
        }

        private enum Mode
        {
            Once,
            Loop,
            PingPong
        }
    }
}