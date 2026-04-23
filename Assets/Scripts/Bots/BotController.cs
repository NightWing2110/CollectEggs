using System.Collections.Generic;
using CollectEggs.Core;
using CollectEggs.Gameplay;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Movement;
using CollectEggs.Gameplay.Navigation;
using CollectEggs.Gameplay.Players;
using UnityEngine;

namespace CollectEggs.Bots
{
    [RequireComponent(typeof(ActorMovement), typeof(PlayerEntity))]
    public partial class BotController : MonoBehaviour
    {
        private enum BotState
        {
            Idle = 0,
            Chasing = 1,
            Recovering = 2
        }

        #region Serialized config

        [SerializeField]
        private float startMoveDelay = 0.5f;

        [SerializeField]
        private float retargetInterval = 0.4f;

        [SerializeField]
        private float retargetIntervalChasing = 0.72f;

        [SerializeField]
        private float repathInterval = 0.45f;

        [SerializeField]
        private float repathIntervalChasing = 0.9f;

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

        [SerializeField]
        private float collectReachRadius = 0.75f;

        [SerializeField]
        private float collectProximitySlack = 0.14f;

        [SerializeField]
        private float approachMargin = 0.08f;

        [SerializeField]
        private int nearestEggsToPathfind = 4;

        [SerializeField]
        private float eggPositionRepathThreshold = 0.3f;

        [SerializeField]
        private int earlyExitPathCellCount = 5;

        [SerializeField]
        private float earlyExitPathCostMultiplier = 2.2f;

        [SerializeField]
        private float eggIgnoreSecondsAfterPathFail = 7f;

        [SerializeField]
        private float eggIgnoreSecondsAfterStuck = 6f;

        [SerializeField]
        private int stuckRecoveriesBeforeEggIgnore = 2;

        #endregion

        #region Runtime references

        private ActorMovement _movement;
        private PlayerEntity _entity;
        private GridMap _gridMap;
        private CharacterController _cc;
        private bool _eggCollectedSubscribed;

        #endregion

        #region Target and path state

        private EggEntity _targetEgg;
        private readonly List<Vector3> _path = new(64);
        private int _pathIndex;
        private Vector3 _approachGoalWorld;
        private bool _hasApproachGoal;
        private Vector3 _pathEggAnchor;

        #endregion

        #region Timers and bot state

        private BotState _state = BotState.Idle;
        private float _startDelayTimer;
        private float _retargetTimer;
        private float _repathTimer;
        private float _stuckTimer;
        private int _stuckChecks;
        private Vector3 _lastStuckPosition;
        private float _recoverTimer;
        private Vector2 _recoverDirection;
        private int _stuckRecoveriesOnTarget;
        private int _coastRetargetSkips;

        #endregion

        #region Scratch and cache buffers

        private readonly List<Vector3> _astarBuffer = new(64);
        private readonly List<Vector3> _bestEggPathScratch = new(64);
        private readonly List<Vector3> _evalBestPathCache = new(64);
        private readonly List<EggTargetCandidate> _eggCandidates = new(32);
        private readonly Dictionary<int, float> _eggIgnoreUntil = new();

        #endregion

        public EggEntity CurrentTargetEgg => _targetEgg;

        public Vector3 DebugPathEndWorld
        {
            get
            {
                if (_hasApproachGoal)
                    return _approachGoalWorld;
                if (_path.Count > 0)
                    return _path[^1];
                return _targetEgg != null ? _targetEgg.transform.position : transform.position;
            }
        }

        public bool UsesExactApproachGoal => _targetEgg != null && (_hasApproachGoal || _path.Count > 0);

        private void Awake()
        {
            _movement = GetComponent<ActorMovement>();
            _entity = GetComponent<PlayerEntity>();
            _cc = GetComponent<CharacterController>();
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
            nearestEggsToPathfind = Mathf.Clamp(nearestEggsToPathfind, 2, 8);
            SubscribeEggCollectedEvent();
        }

        private void Update()
        {
            SubscribeEggCollectedEvent();
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

            HandleExternalTargetInvalidationEarly();

            _retargetTimer -= Time.deltaTime;
            if (_retargetTimer <= 0f)
            {
                EvaluateTarget();
                var coasting = _state == BotState.Chasing && _path.Count > 0 && _pathIndex < _path.Count;
                _retargetTimer = coasting ? retargetIntervalChasing : retargetInterval;
            }

            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0f)
            {
                var coasting = _state == BotState.Chasing && _path.Count > 0 && _pathIndex < _path.Count;
                RefreshPath(!coasting);
                _repathTimer = coasting ? repathIntervalChasing : repathInterval;
            }

            TickState();
        }

        private void LateUpdate()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsMatchRunning)
                return;
            if (_startDelayTimer > 0f)
                return;
            CollectTargetEggProximity();
        }

        private void CollectTargetEggProximity()
        {
            var targetBeforeCollect = _targetEgg;
            var refXZ = CollectHorizontalReference();
            var extraRadius = _cc != null ? _cc.radius * 0.35f : 0f;
            if (!EggCollectProximity.CollectTargetEgg(
                    _entity,
                    targetBeforeCollect,
                    refXZ,
                    collectReachRadius,
                    collectProximitySlack,
                    extraRadius))
                return;
            if (targetBeforeCollect != null)
                ClearTargetState();

            ForceRetargetAfterCollect();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var targetBeforeCollect = _targetEgg;
            if (!EggCollectProximity.CollectFromCollider(_entity, hit.collider))
                return;
            if (targetBeforeCollect != null && hit.collider != null && hit.collider.gameObject == targetBeforeCollect.gameObject)
                ClearTargetState();

            ForceRetargetAfterCollect();
        }

        private void TickState()
        {
            switch (_state)
            {
                case BotState.Recovering:
                    TickRecovering();
                    return;
                case BotState.Idle:
                {
                    if (_targetEgg != null && _path.Count > 0 && _pathIndex < _path.Count)
                        _state = BotState.Chasing;
                    else
                        _movement.Move(Vector2.zero);
                    return;
                }
                case BotState.Chasing:
                default:
                    TickChasing();
                    break;
            }
        }

        private void TickChasing()
        {
            FollowPath();
            CheckStuck();
            var pathDone = _path.Count > 0 && _pathIndex >= _path.Count;
            if (pathDone && IsEggValid(_targetEgg))
            {
                var refXZ = CollectHorizontalReference();
                var e = _targetEgg.transform.position;
                var dx = e.x - refXZ.x;
                var dz = e.z - refXZ.y;
                var d = Mathf.Sqrt(dx * dx + dz * dz);
                var pickupHorizon = collectReachRadius + collectProximitySlack + (_cc != null ? _cc.radius * 0.4f : 0f);
                if (d > pickupHorizon + 0.18f)
                    BlacklistEgg(_targetEgg, eggIgnoreSecondsAfterPathFail);
            }

            if (!IsEggValid(_targetEgg) || _path.Count == 0 || pathDone)
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

        private void RefreshPath(bool forceWhileCoasting = false)
        {
            if (!IsEggValid(_targetEgg))
            {
                ClearTargetState();
                return;
            }

            if (!forceWhileCoasting && _state == BotState.Chasing && _path.Count > 0 && _pathIndex < _path.Count)
                return;

            if (!PlanPathToEgg(_targetEgg, _bestEggPathScratch, out var plan))
            {
                BlacklistEgg(_targetEgg, eggIgnoreSecondsAfterPathFail);
                return;
            }

            SetPathAndApproach(_bestEggPathScratch, plan.ApproachWorld);
        }

        private bool PlanPathToEgg(EggEntity egg, List<Vector3> pathOut, out ApproachPathResult plan)
        {
            var botR = _cc != null ? _cc.radius : 0.5f;
            var settings = new EggApproachPlannerSettings(
                botR,
                collectReachRadius,
                approachMargin,
                earlyExitPathCellCount,
                earlyExitPathCostMultiplier);
            return EggApproachPathPlanner.FindBestApproachPath(
                _gridMap,
                transform.position,
                settings,
                egg,
                pathOut,
                _astarBuffer,
                out plan);
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

        private void SetTarget(EggEntity target, List<Vector3> path, Vector3 approachWorld)
        {
            if (_targetEgg != target)
            {
                _stuckRecoveriesOnTarget = 0;
                _coastRetargetSkips = 0;
            }

            _targetEgg = target;
            _pathEggAnchor = target.transform.position;
            _approachGoalWorld = approachWorld;
            _hasApproachGoal = true;
            SetPath(path);
            _state = _path.Count > 0 ? BotState.Chasing : BotState.Idle;
        }

        private void SetPathAndApproach(List<Vector3> path, Vector3 approachWorld)
        {
            _approachGoalWorld = approachWorld;
            _hasApproachGoal = true;
            SetPath(path);
            if (_targetEgg != null)
                _pathEggAnchor = _targetEgg.transform.position;
        }

        private void SetPath(List<Vector3> path)
        {
            _path.Clear();
            if (path != null)
                _path.AddRange(path);
            _pathIndex = 0;
        }

        private void ClearTargetState()
        {
            _targetEgg = null;
            _path.Clear();
            _pathIndex = 0;
            _state = BotState.Idle;
            _hasApproachGoal = false;
            _stuckRecoveriesOnTarget = 0;
            _coastRetargetSkips = 0;
        }

        private void ForceRetargetAfterCollect()
        {
            _retargetTimer = 0f;
            _repathTimer = 0f;
            EvaluateTarget();
        }

        private void HandleExternalTargetInvalidationEarly()
        {
            if (_targetEgg == null)
                return;
            if (IsEggValid(_targetEgg))
                return;
            ClearTargetState();
            ForceRetargetAfterExternalInvalidation();
        }

        private void ForceRetargetAfterExternalInvalidation()
        {
            _retargetTimer = 0f;
            _repathTimer = 0f;
            EvaluateTarget();
        }

        private void SubscribeEggCollectedEvent()
        {
            if (_eggCollectedSubscribed || GameManager.Instance == null)
                return;
            GameManager.Instance.EggCollected += HandleEggCollectedEvent;
            _eggCollectedSubscribed = true;
        }

        private void UnsubscribeEggCollectedEvent()
        {
            if (!_eggCollectedSubscribed || GameManager.Instance == null)
                return;
            GameManager.Instance.EggCollected -= HandleEggCollectedEvent;
            _eggCollectedSubscribed = false;
        }

        private void HandleEggCollectedEvent(string collectorId, string eggId, int _)
        {
            if (_targetEgg == null || string.IsNullOrEmpty(eggId))
                return;
            if (_entity != null && collectorId == _entity.PlayerId)
                return;
            if (_targetEgg.EggId != eggId)
                return;
            ClearTargetState();
            ForceRetargetAfterExternalInvalidation();
        }

        private void BlacklistEgg(EggEntity egg, float seconds)
        {
            if (egg == null)
                return;
            var id = egg.GetInstanceID();
            var until = Time.time + seconds;
            if (_eggIgnoreUntil.TryGetValue(id, out var prev))
                until = Mathf.Max(until, prev);
            _eggIgnoreUntil[id] = until;
            if (_targetEgg == egg)
                ClearTargetState();
        }

        private bool IsEggIgnored(EggEntity egg)
        {
            if (egg == null)
                return true;
            return _eggIgnoreUntil.TryGetValue(egg.GetInstanceID(), out var until) && Time.time < until;
        }

        private static bool IsEggValid(EggEntity egg) => egg != null && egg.gameObject.activeInHierarchy;

        private float EstimatePathCost(List<Vector3> path) => EggApproachPathPlanner.EstimatePathCostFrom(transform.position, path);

        private Vector2 CollectHorizontalReference()
        {
            if (_cc == null)
                return new Vector2(transform.position.x, transform.position.z);
            var w = transform.TransformPoint(_cc.center);
            return new Vector2(w.x, w.z);
        }

        private void OnDisable()
        {
            UnsubscribeEggCollectedEvent();
        }

        private void OnDestroy()
        {
            UnsubscribeEggCollectedEvent();
        }

    }
}
