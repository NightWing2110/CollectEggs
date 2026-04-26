using UnityEngine;

namespace CollectEggs.Gameplay.Players.View
{
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        [SerializeField]
        private Transform trackedRoot;

        [SerializeField]
        private float speedMultiplier = 1f;

        [SerializeField]
        private float damping = 10f;

        [SerializeField]
        private float minDeltaTime = 0.0001f;

        [SerializeField]
        private float lookDirectionThreshold = 0.03f;

        [SerializeField]
        private float turnSharpness = 12f;

        [SerializeField]
        private Vector3 rotationOffsetEuler = Vector3.zero;

        private Vector3 _lastWorldPosition;
        private float _smoothedSpeed;
        private Vector3 _lastLookDirection;
        private bool _hasLookDirection;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        public void Configure(Animator targetAnimator, Transform targetRoot)
        {
            animator = targetAnimator;
            trackedRoot = targetRoot;
            InitializeSamplePosition();
        }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();

            if (trackedRoot == null) trackedRoot = transform.parent != null ? transform.parent : transform;

            InitializeSamplePosition();
        }

        private void LateUpdate()
        {
            if (animator == null || trackedRoot == null)
                return;

            var dt = Time.deltaTime;
            if (dt < minDeltaTime)
                return;

            var current = trackedRoot.position;
            var delta = current - _lastWorldPosition;
            delta.y = 0f;
            var rawSpeed = delta.magnitude / dt * speedMultiplier;
            var lerp = 1f - Mathf.Exp(-damping * dt);
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, rawSpeed, lerp);
            animator.SetFloat(SpeedHash, _smoothedSpeed);
            UpdateVisualFacing(delta, dt);
            _lastWorldPosition = current;
        }

        private void UpdateVisualFacing(Vector3 delta, float dt)
        {
            var sqrDistance = delta.sqrMagnitude;
            if (sqrDistance >= lookDirectionThreshold * lookDirectionThreshold)
            {
                _lastLookDirection = delta.normalized;
                _hasLookDirection = true;
            }

            if (!_hasLookDirection)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(_lastLookDirection, Vector3.up) * Quaternion.Euler(rotationOffsetEuler);
            var t = 1f - Mathf.Exp(-turnSharpness * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
        }

        private void InitializeSamplePosition()
        {
            var root = trackedRoot != null ? trackedRoot : transform;
            _lastWorldPosition = root.position;
            _smoothedSpeed = 0f;
            _lastLookDirection = transform.forward;
            _lastLookDirection.y = 0f;
            if (_lastLookDirection.sqrMagnitude > 0.0001f)
            {
                _lastLookDirection.Normalize();
                _hasLookDirection = true;
            }
            else
            {
                _lastLookDirection = Vector3.forward;
                _hasLookDirection = false;
            }
        }
    }
}
