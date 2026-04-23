using System;
using System.Collections.Generic;
using System.Linq;
using CollectEggs.Gameplay;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.UI;
using CollectEggs.UI.Results;
using CollectEggs.Gameplay.Players;
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

            public EggCollectedResult(string eggId)
            {
                EggId = eggId;
            }
        }

        public static GameManager Instance { get; private set; }
        public event Action MatchStarted;
        public event Action<string, string, int> EggCollected;
        public event Action<string, int> MatchEnded;

        [SerializeField]
        private GameObject playerPrefab;

        [SerializeField]
        private Vector3 spawnPosition = new(0f, 1f, 0f);

        [SerializeField]
        private string fallbackLocalPlayerId = "local";

        private readonly List<PlayerEntity> _players = new();
        private readonly List<Transform> _playerTransforms = new();
        private PlayerEntity _localPlayerEntity;
        public bool IsMatchRunning => _matchTimer != null && _matchTimer.IsRunning;
        private PlayerSpawner _playerSpawner;
        private EggSpawner _eggSpawner;
        private EggCollectSystem _eggCollectSystem;
        private MatchTimer _matchTimer;
        private ScoreService _scoreService;
        private DebugHud _debugHud;
        private ResultsPanelView _resultsPanel;
        private GridMap _gridMap;

        [SerializeField]
        private float botGridCellSize = 0.5f;

        private void Awake()
        {
            Instance = this;
            EnsureDependencies();
            ConfigureNavigationGrid();
            _playerSpawner.RebuildSpawnParents();
        }

        private void EnsureDependencies()
        {
            _playerSpawner = GetOrAdd<PlayerSpawner>();
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

        private void Start()
        {
            if (!SpawnPlayers())
                return;
            InitializeGameplaySystems();
            StartMatchFlow();
            InitializeUi();
        }

        private bool SpawnPlayers()
        {
            _players.Clear();
            _players.AddRange(_playerSpawner.SpawnPlayers(playerPrefab, spawnPosition, out _localPlayerEntity));
            if (_localPlayerEntity == null)
            {
                Debug.LogError("Failed to spawn local player entity from player prefab.");
                return false;
            }

            _playerTransforms.Clear();
            foreach (var player in _players)
            {
                if (player != null)
                    _playerTransforms.Add(player.transform);
            }
            return true;
        }

        private void InitializeGameplaySystems()
        {
            _eggSpawner.Initialize(_playerSpawner.EggsRoot, _playerTransforms, _matchTimer);
            _eggCollectSystem.Initialize(_localPlayerEntity);
            _matchTimer.MatchEnded += HandleMatchEnded;
            _scoreService.ResetScores();
            foreach (var player in _players.Where(player => player != null)) _scoreService.EnsurePlayer(player.PlayerId);
        }

        private void StartMatchFlow()
        {
            _resultsPanel.Hide();
            _debugHud.SetMatchEndSummaryVisible(true);
            _matchTimer.Begin();
            MatchStarted?.Invoke();
            _eggSpawner.SpawnInitialEggs();
        }

        private void InitializeUi()
        {
            _debugHud.Initialize(_matchTimer, _scoreService, _localPlayerEntity.PlayerId, RestartMatch);
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
            result = new EggCollectedResult(eggId);
            return true;
        }

        private void ApplyEggCollectedResult(PlayerEntity collector, EggCollectedResult result)
        {
            var collectorId = ResolvePlayerId(collector);
            _scoreService.EnsurePlayer(collectorId);
            _scoreService.AddScore(collectorId, 1);
            var totalScore = _scoreService.GetScore(collectorId);
            EggCollected?.Invoke(collectorId, result.EggId, totalScore);
            Debug.Log($"Score[{collectorId}]: {totalScore}");
        }

        private void FinalizeEggCollection(GameObject egg)
        {
            egg.SetActive(false);
            Destroy(egg);
            _eggSpawner.SpawnEggs();
        }

        private void HandleMatchEnded()
        {
            var sortedResults = BuildSortedMatchResults();
            _resultsPanel.Show(sortedResults);
            _debugHud.SetMatchEndSummaryVisible(false);
            if (_scoreService.TryGetWinner(out var winnerId, out var winnerScore))
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
            if (_matchTimer != null)
                _matchTimer.MatchEnded -= HandleMatchEnded;
            if (Instance == this)
                Instance = null;
        }

        private T GetOrAdd<T>() where T : Component
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
