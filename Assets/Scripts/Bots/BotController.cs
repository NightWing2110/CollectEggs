using System.Collections.Generic;
using CollectEggs.Core;
using CollectEggs.Gameplay.Eggs;
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
        private float _authorityCollectReach;

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

        public void SetCollectReachFromServerRules(float radius) => _authorityCollectReach = Mathf.Max(0.01f, radius);

        public void SetNavigationGrid(GridMap gridMap) => _gridMap = gridMap;

        private void Awake()
        {
            GetComponent();
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

        private void GetComponent()
        {
            _movement = GetComponent<ActorMovement>();
            _entity = GetComponent<PlayerEntity>();
            _cc = GetComponent<CharacterController>();
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
    }
}
