using System;
using System.Collections.Generic;
using System.Linq;
using CollectEggs.AI.Pathfinding;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.UI;
using CollectEggs.Gameplay.Players;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollectEggs.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
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
        private CollectSystem _collectSystem;
        private MatchTimer _matchTimer;
        private ScoreService _scoreService;
        private DebugHud _debugHud;
        private GridMap _gridMap;

        [SerializeField]
        private float botGridCellSize = 0.5f;

        private void Awake()
        {
            Instance = this;
            EnsureDependencies();
            SetupGridMapDefaults();
            _playerSpawner.RebuildSpawnParents();
        }

        private void EnsureDependencies()
        {
            _playerSpawner = GetOrAdd<PlayerSpawner>();
            _eggSpawner = GetOrAdd<EggSpawner>();
            _collectSystem = GetOrAdd<CollectSystem>();
            _matchTimer = GetOrAdd<MatchTimer>();
            _scoreService = GetOrAdd<ScoreService>();
            _debugHud = GetOrAdd<DebugHud>();
            _gridMap = GetOrAdd<GridMap>();
        }

        private void SetupGridMapDefaults()
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
            _players.Clear();
            _players.AddRange(_playerSpawner.SpawnPlayers(playerPrefab, spawnPosition, out _localPlayerEntity));
            if (_localPlayerEntity == null)
            {
                Debug.LogError("Failed to spawn local player entity from player prefab.");
                return;
            }
            _playerTransforms.Clear();
            foreach (var player in _players)
            {
                if (player != null)
                    _playerTransforms.Add(player.transform);
            }

            _eggSpawner.Initialize(_playerSpawner.EggsRoot, _playerTransforms, _matchTimer);
            _collectSystem.Initialize(_localPlayerEntity);
            _matchTimer.MatchEnded += HandleMatchEnded;
            _scoreService.ResetScores();
            foreach (var player in _players.Where(player => player != null)) _scoreService.EnsurePlayer(player.PlayerId);
            _matchTimer.Begin();
            MatchStarted?.Invoke();
            _eggSpawner.SpawnInitialEggs();
            _debugHud.Initialize(_matchTimer, _scoreService, _localPlayerEntity.PlayerId, RestartMatch);
        }

        public void CollectEgg(PlayerEntity collector, GameObject egg)
        {
            if (!IsMatchRunning) return;
            if (collector == null) return;
            if (egg == null) return;
            var eggEntity = egg.GetComponent<EggEntity>();
            if (eggEntity != null && !eggEntity.MarkCollected()) return;
            var eggId = eggEntity != null ? eggEntity.EggId : string.Empty;
            egg.SetActive(false);
            Destroy(egg);
            var collectorId = ResolvePlayerId(collector);
            _scoreService.EnsurePlayer(collectorId);
            _scoreService.AddScore(collectorId, 1);
            var totalScore = _scoreService.GetScore(collectorId);
            EggCollected?.Invoke(collectorId, eggId, totalScore);
            Debug.Log($"Score[{collectorId}]: {totalScore}");
            _eggSpawner.SpawnEggs();
        }

        private void HandleMatchEnded()
        {
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

        private void RestartMatch()
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
