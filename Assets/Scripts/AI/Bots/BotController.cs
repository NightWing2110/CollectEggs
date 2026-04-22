using CollectEggs.Core;
using CollectEggs.AI.Pathfinding;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Players;
using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.AI.Bots
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerEntity))]
    public class BotController : MonoBehaviour
    {
        private enum BotState
        {
            Idle = 0,
            Chasing = 1,
            Recovering = 2
        }

        [SerializeField]
        private float startMoveDelay = 0.5f;

        [SerializeField]
        private float retargetInterval = 0.3f;

        [SerializeField]
        private float repathInterval = 0.35f;

        [SerializeField]
        private float repathTargetSwitchAdvantage = 0.75f;

        [SerializeField]
        private float waypointReachDistance = 0.2f;

        [SerializeField]
        private float stuckCheckInterval = 0.45f;

        [SerializeField]
        private float stuckDistanceThreshold = 0.08f;

        [SerializeField]
        private int maxStuckChecksBeforeRecover = 3;

        [SerializeField]
        private float recoverMoveDuration = 0.25f;

        private PlayerMovement _movement;
        private PlayerEntity _entity;
        private GridMap _gridMap;
        private EggEntity _targetEgg;
        private readonly List<Vector3> _path = new(64);
        private BotState _state = BotState.Idle;
        private int _pathIndex;
        private float _startDelayTimer;
        private float _retargetTimer;
        private float _repathTimer;
        private float _stuckTimer;
        private int _stuckChecks;
        private Vector3 _lastStuckPosition;
        private float _recoverTimer;
        private Vector2 _recoverDirection;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _entity = GetComponent<PlayerEntity>();
            _gridMap = FindFirstObjectByType<GridMap>();
            _startDelayTimer = startMoveDelay;
            _retargetTimer = Random.Range(0f, retargetInterval);
            _repathTimer = 0f;
            _stuckTimer = 0f;
            _stuckChecks = 0;
            _lastStuckPosition = transform.position;
            _recoverTimer = 0f;
            _recoverDirection = Vector2.zero;
            _state = BotState.Idle;
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsMatchRunning)
            {
                _movement.Move(Vector2.zero);
                return;
            }

            if (_startDelayTimer > 0f)
            {
                _startDelayTimer -= Time.deltaTime;
                _movement.Move(Vector2.zero);
                return;
            }

            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                EvaluateTarget();
                _retargetTimer = retargetInterval;
            }

            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                RefreshPath();
                _repathTimer = repathInterval;
            }

            TickState();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null)
                return;
            if (!hit.collider.CompareTag("Egg"))
                return;
            if (GameManager.Instance == null || _entity == null)
                return;
            GameManager.Instance.CollectEgg(_entity, hit.collider.gameObject);
            if (_targetEgg != null && hit.collider.gameObject == _targetEgg.gameObject)
            {
                _targetEgg = null;
                _path.Clear();
                _pathIndex = 0;
                _state = BotState.Idle;
            }
        }

        private void EvaluateTarget()
        {
            var eggs = EggEntity.Active;
            if (eggs.Count == 0)
            {
                _targetEgg = null;
                _path.Clear();
                _pathIndex = 0;
                _state = BotState.Idle;
                return;
            }

            if (!IsEggValid(_targetEgg))
            {
                _targetEgg = null;
                _path.Clear();
                _pathIndex = 0;
                _state = BotState.Idle;
            }

            EggEntity bestEgg = null;
            List<Vector3> bestPath = null;
            var bestCost = float.MaxValue;

            foreach (var egg in eggs)
            {
                if (!IsEggValid(egg))
                    continue;
                var candidatePath = BuildPathTo(egg.transform.position);
                if (candidatePath == null)
                    continue;
                var candidateCost = EstimatePathCost(candidatePath);
                if (!(candidateCost < bestCost)) continue;
                bestCost = candidateCost;
                bestEgg = egg;
                bestPath = candidatePath;
            }

            if (bestEgg == null)
                return;

            if (_targetEgg == null)
            {
                SetTarget(bestEgg, bestPath);
                return;
            }

            if (_targetEgg == bestEgg)
            {
                SetPath(bestPath);
                return;
            }

            var currentCost = EstimatePathCost(_path);
            var switchThreshold = currentCost * repathTargetSwitchAdvantage;
            if (bestCost <= switchThreshold)
                SetTarget(bestEgg, bestPath);
        }

        private void TickState()
        {
            if (_state == BotState.Recovering)
            {
                TickRecovering();
                return;
            }

            if (_state == BotState.Idle)
            {
                if (_targetEgg != null && _path.Count > 0 && _pathIndex < _path.Count)
                    _state = BotState.Chasing;
                else
                    _movement.Move(Vector2.zero);
                return;
            }

            TickChasing();
        }

        private void TickChasing()
        {
            FollowPath();
            CheckStuck();
            if (!IsEggValid(_targetEgg) || _path.Count == 0 || _pathIndex >= _path.Count)
                _state = BotState.Idle;
        }

        private void TickRecovering()
        {
            if (_recoverTimer > 0f)
            {
                _recoverTimer -= Time.deltaTime;
                _movement.Move(_recoverDirection);
                return;
            }

            _state = BotState.Chasing;
        }

        private void RefreshPath()
        {
            if (!IsEggValid(_targetEgg))
            {
                _targetEgg = null;
                _path.Clear();
                _pathIndex = 0;
                _state = BotState.Idle;
                return;
            }

            var rebuilt = BuildPathTo(_targetEgg.transform.position);
            if (rebuilt == null)
            {
                _path.Clear();
                _pathIndex = 0;
                _state = BotState.Idle;
                return;
            }

            SetPath(rebuilt);
        }

        private List<Vector3> BuildPathTo(Vector3 destination)
        {
            if (_gridMap == null)
            {
                var fallback = new List<Vector3>(1) { destination };
                return fallback;
            }

            var buffer = new List<Vector3>(64);
            return !AStarPathfinder.FindPath(_gridMap, transform.position, destination, buffer) ? null : buffer;
        }

        private void FollowPath()
        {
            if (_path.Count == 0 || _pathIndex >= _path.Count)
            {
                _movement.Move(Vector2.zero);
                return;
            }

            var current = transform.position;
            var target = _path[_pathIndex];
            var toTarget = target - current;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= waypointReachDistance * waypointReachDistance)
            {
                _pathIndex++;
                if (_pathIndex >= _path.Count)
                {
                    _movement.Move(Vector2.zero);
                    return;
                }

                target = _path[_pathIndex];
                toTarget = target - current;
                toTarget.y = 0f;
            }

            var dir = toTarget.sqrMagnitude > Mathf.Epsilon ? toTarget.normalized : Vector3.zero;
            _movement.Move(new Vector2(dir.x, dir.z));
        }

        private void CheckStuck()
        {
            if (_path.Count == 0 || _pathIndex >= _path.Count)
                return;

            _stuckTimer -= Time.deltaTime;
            if (_stuckTimer > 0f)
                return;

            _stuckTimer = stuckCheckInterval;
            var moved = (transform.position - _lastStuckPosition).magnitude;
            _lastStuckPosition = transform.position;

            if (moved >= stuckDistanceThreshold)
            {
                _stuckChecks = 0;
                return;
            }

            _stuckChecks++;
            if (_stuckChecks < maxStuckChecksBeforeRecover)
                return;

            _stuckChecks = 0;
            RefreshPath();
            _pathIndex = Mathf.Min(_pathIndex + 1, _path.Count);
            _recoverDirection = Random.insideUnitCircle.normalized;
            _recoverTimer = recoverMoveDuration;
            _state = BotState.Recovering;
        }

        private void SetTarget(EggEntity target, List<Vector3> path)
        {
            _targetEgg = target;
            SetPath(path);
            _state = _path.Count > 0 ? BotState.Chasing : BotState.Idle;
        }

        private void SetPath(List<Vector3> path)
        {
            _path.Clear();
            if (path != null)
                _path.AddRange(path);
            _pathIndex = 0;
        }

        private static bool IsEggValid(EggEntity egg)
        {
            return egg != null && egg.gameObject.activeInHierarchy;
        }

        private float EstimatePathCost(List<Vector3> path)
        {
            if (path == null || path.Count == 0)
                return float.MaxValue;
            var total = 0f;
            var prev = transform.position;
            foreach (var t in path)
            {
                var point = t;
                point.y = prev.y;
                total += Vector3.Distance(prev, point);
                prev = point;
            }

            return total;
        }
    }
}
