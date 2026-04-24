using System;
using System.Collections.Generic;
using System.Linq;
using CollectEggs.Gameplay;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Players;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.Client.View;
using CollectEggs.Bots;
using CollectEggs.Shared.Messages;
using CollectEggs.UI;
using CollectEggs.UI.Results;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollectEggs.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        private readonly struct EggCollectedResult
        {
            public readonly string EggId;
            public readonly int ScoreDelta;

            public EggCollectedResult(string eggId, int scoreDelta)
            {
                EggId = eggId;
                ScoreDelta = scoreDelta;
            }
        }

        public static GameManager Instance { get; private set; }
        public event Action MatchStarted;
        public event Action<string, string, int> EggCollected;
        public event Action<string, int> MatchEnded;

        [SerializeField]
        private GameObject playerPrefab;

        [SerializeField]
        private string fallbackLocalPlayerId = "local";

        [SerializeField]
        private float botGridCellSize = 0.5f;

        private readonly List<PlayerEntity> _players = new();
        private readonly List<Transform> _playerTransforms = new();
        private readonly List<Vector3> _playerPositionScratch = new();
        private readonly List<Vector3> _eggPositionScratch = new();
        private PlayerEntity _localPlayerEntity;
        private GameBootstrapper _bootstrapper;
        private EggViewManager _eggViewManager;
        private PlayerSpawner _playerSpawner;
        private EggSpawner _eggSpawner;
        private EggCollectSystem _eggCollectSystem;
        private MatchTimer _matchTimer;
        private ScoreService _scoreService;
        private DebugHud _debugHud;
        private ResultsPanelView _resultsPanel;
        private GridMap _gridMap;
        private bool _matchExpiryHandled;

        public bool IsMatchRunning => _matchTimer != null && _matchTimer.IsRunning;
        public GameObject PlayerPrefab => playerPrefab;

        private void Awake()
        {
            Instance = this;
            EnsureDependencies();
            ConfigureNavigationGrid();
            _playerSpawner = GetOrAdd<PlayerSpawner>();
            _playerSpawner.RebuildSpawnParents();
            if (GetComponent<GameBootstrapper>() == null)
                gameObject.AddComponent<GameBootstrapper>();
        }

        public void ApplyMatchStarted(
            MatchStartedMessage message,
            IReadOnlyList<PlayerEntity> players,
            PlayerEntity localPlayer,
            GameBootstrapper bootstrapper)
        {
            _bootstrapper = bootstrapper;
            _eggViewManager = GetOrAdd<EggViewManager>();
            if (_eggSpawner != null && _eggSpawner.EggPrefab != null)
                _eggViewManager.SetEggPrefab(_eggSpawner.EggPrefab);
            _players.Clear();
            _playerTransforms.Clear();
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (p == null)
                        continue;
                    _players.Add(p);
                    _playerTransforms.Add(p.transform);
                }
            }

            _localPlayerEntity = localPlayer;
            if (_localPlayerEntity == null)
            {
                Debug.LogError("ApplyMatchStarted: local player missing.");
                return;
            }

            _matchExpiryHandled = false;
            _eggCollectSystem.Initialize(_localPlayerEntity);
            if (message != null)
            {
                _matchTimer.BeginFromAuthority(message.Rules.MatchDuration);
                _eggCollectSystem.SetCollectRadiusFromAuthority(message.Rules.EggCollectRadius);
                foreach (var p in _players)
                {
                    var bot = p != null ? p.GetComponent<BotController>() : null;
                    bot?.SetCollectReachFromAuthority(message.Rules.EggCollectRadius);
                }
            }
            else
                _matchTimer.BeginFromAuthority(0f);

            _scoreService.ResetScores();
            foreach (var player in _players.Where(player => player != null))
                _scoreService.EnsurePlayer(player.PlayerId);
            _resultsPanel.Hide();
            _debugHud.SetMatchEndSummaryVisible(true);
            MatchStarted?.Invoke();
            _debugHud.Initialize(_matchTimer, _scoreService, _localPlayerEntity.PlayerId, RestartMatch);
        }

        private void EnsureDependencies()
        {
            _eggSpawner = GetOrAdd<EggSpawner>();
            _eggCollectSystem = GetOrAdd<EggCollectSystem>();
            _matchTimer = GetOrAdd<MatchTimer>();
            _scoreService = GetOrAdd<ScoreService>();
            _debugHud = GetOrAdd<DebugHud>();
            _resultsPanel = GetOrAdd<ResultsPanelView>();
            _gridMap = GetOrAdd<GridMap>();
        }

        private void ConfigureNavigationGrid()
        {
            if (_gridMap == null || _eggSpawner == null)
                return;
            _gridMap.ConfigureFromBounds(_eggSpawner.SpawnBoundsMin, _eggSpawner.SpawnBoundsMax, botGridCellSize);
            var groundLayer = LayerMask.NameToLayer("Ground");
            var obstacleLayer = LayerMask.NameToLayer("Obstacle");
            _gridMap.ConfigureLayers(groundLayer, obstacleLayer);
            _gridMap.Rebuild();
        }

        public bool CollectEgg(PlayerEntity collector, GameObject egg)
        {
            if (!MarkEggCollected(collector, egg, out var result))
                return false;
            ApplyEggCollectedResult(collector, result);
            FinalizeEggCollection(egg);
            return true;
        }

        private bool MarkEggCollected(PlayerEntity collector, GameObject egg, out EggCollectedResult result)
        {
            result = default;
            if (!IsMatchRunning || collector == null || egg == null)
                return false;
            var eggEntity = egg.GetComponent<EggEntity>();
            if (eggEntity != null && !eggEntity.MarkCollected())
                return false;
            var eggId = eggEntity != null ? eggEntity.EggId : string.Empty;
            var scoreDelta = eggEntity != null ? eggEntity.ScoreValue : 1;
            result = new EggCollectedResult(eggId, scoreDelta);
            return true;
        }

        private void ApplyEggCollectedResult(PlayerEntity collector, EggCollectedResult result)
        {
            var collectorId = ResolvePlayerId(collector);
            _scoreService.EnsurePlayer(collectorId);
            _scoreService.AddScore(collectorId, result.ScoreDelta);
            var totalScore = _scoreService.GetScore(collectorId);
            EggCollected?.Invoke(collectorId, result.EggId, totalScore);
            Debug.Log($"Score[{collectorId}]: {totalScore}");
        }

        private void FinalizeEggCollection(GameObject egg)
        {
            var eggEntity = egg.GetComponent<EggEntity>();
            var eggId = eggEntity != null ? eggEntity.EggId : string.Empty;
            egg.SetActive(false);
            Destroy(egg);
            if (_bootstrapper == null || _playerSpawner == null)
                return;
            _playerPositionScratch.Clear();
            foreach (var t in _playerTransforms)
            {
                if (t != null)
                    _playerPositionScratch.Add(t.position);
            }

            _eggPositionScratch.Clear();
            foreach (var e in EggEntity.Active)
            {
                if (e != null && e.gameObject.activeInHierarchy)
                    _eggPositionScratch.Add(e.transform.position);
            }

            _bootstrapper.NotifyEggCollectedForRespawn(eggId, _playerPositionScratch, _eggPositionScratch);
        }

        public void NotifyMatchExpiredFromNetwork()
        {
            if (_matchExpiryHandled || _matchTimer == null)
                return;
            _matchExpiryHandled = true;
            _matchTimer.StopFromAuthority();
            HandleMatchEnded();
        }

        private void HandleMatchEnded()
        {
            var sortedResults = BuildSortedMatchResults();
            _resultsPanel.Show(sortedResults);
            _debugHud.SetMatchEndSummaryVisible(false);
            if (_scoreService.GetWinner(out var winnerId, out var winnerScore))
            {
                MatchEnded?.Invoke(winnerId, winnerScore);
                Debug.Log($"Match ended. Winner: {winnerId}. Final score: {winnerScore}");
            }
            else
            {
                MatchEnded?.Invoke(string.Empty, 0);
                Debug.Log("Match ended. Winner: No winner. Final score: 0");
            }
        }

        private List<MatchResultEntry> BuildSortedMatchResults()
        {
            var results = new List<MatchResultEntry>(_players.Count);
            foreach (var player in _players)
            {
                if (player == null)
                    continue;
                var score = _scoreService.GetScore(player.PlayerId);
                var displayName = string.IsNullOrWhiteSpace(player.DisplayName) ? player.PlayerId : player.DisplayName;
                results.Add(new MatchResultEntry(displayName, score));
            }

            results.Sort((a, b) =>
            {
                var scoreCompare = b.EggCount.CompareTo(a.EggCount);
                return scoreCompare != 0 ? scoreCompare : string.CompareOrdinal(a.DisplayName, b.DisplayName);
            });
            for (var i = 0; i < results.Count; i++)
                Debug.Log($"[ResultsData] rank={i + 1} name={results[i].DisplayName} eggs={results[i].EggCount}");
            return results;
        }

        private static void RestartMatch()
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public T GetOrAdd<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }

        private string ResolvePlayerId(PlayerEntity collector)
        {
            if (collector != null && !string.IsNullOrWhiteSpace(collector.PlayerId))
                return collector.PlayerId;
            return fallbackLocalPlayerId;
        }
    }
}
