using System.Collections.Generic;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.Gameplay.UI;
using CollectEggs.Gameplay.Players;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollectEggs.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField]
        private GameObject playerPrefab;

        [SerializeField]
        private Vector3 spawnPosition = new(0f, 1f, 0f);

        [SerializeField]
        private string fallbackLocalPlayerId = "local";

        private GameObject _player;
        private PlayerEntity _localPlayerEntity;
        public bool IsMatchRunning => _matchTimer != null && _matchTimer.IsRunning;

        private readonly HashSet<int> _pendingCollectedEggs = new();
        private PlayerSpawner _playerSpawner;
        private EggSpawner _eggSpawner;
        private CollectSystem _collectSystem;
        private MatchTimer _matchTimer;
        private ScoreService _scoreService;
        private DebugHud _debugHud;

        private void Awake()
        {
            Instance = this;
            EnsureDependencies();
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
        }

        private void Start()
        {
            _player = _playerSpawner.Spawn(playerPrefab, spawnPosition);
            if (_player == null) return;
            var movement = _player.GetComponent<PlayerMovement>();
            var controller = _player.GetComponent<PlayerController>();
            _localPlayerEntity = _player.GetComponent<PlayerEntity>();
            if (movement == null || controller == null || _localPlayerEntity == null)
            {
                Debug.LogError("Player prefab is missing required components: PlayerEntity, PlayerMovement, and/or PlayerController.");
                return;
            }
            _eggSpawner.Initialize(_playerSpawner.EggsRoot, _player.transform, _matchTimer);
            _collectSystem.Initialize(_localPlayerEntity);
            _matchTimer.MatchEnded += HandleMatchEnded;
            _scoreService.ResetScores();
            _scoreService.EnsurePlayer(_localPlayerEntity.PlayerId);
            _matchTimer.Begin();
            _eggSpawner.SpawnInitialEggs();
            _debugHud.Initialize(_matchTimer, _scoreService, _localPlayerEntity.PlayerId, RestartMatch);
        }

        public void CollectEgg(PlayerEntity collector, GameObject egg)
        {
            if (!IsMatchRunning) return;
            if (collector == null) return;
            if (egg == null) return;
            var id = egg.GetInstanceID();
            if (!_pendingCollectedEggs.Add(id)) return;
            egg.SetActive(false);
            Destroy(egg);
            var collectorId = ResolvePlayerId(collector);
            _scoreService.EnsurePlayer(collectorId);
            _scoreService.AddScore(collectorId, 1);
            Debug.Log($"Score[{collectorId}]: {_scoreService.GetScore(collectorId)}");
            _eggSpawner.SpawnEggs();
        }

        private void HandleMatchEnded()
        {
            if (_scoreService.TryGetWinner(out var winnerId, out var winnerScore))
                Debug.Log($"Match ended. Winner: {winnerId}. Final score: {winnerScore}");
            else
                Debug.Log("Match ended. Winner: No winner. Final score: 0");
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
