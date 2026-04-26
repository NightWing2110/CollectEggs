using System;
using System.Collections.Generic;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Navigation;
using CollectEggs.Gameplay.Players;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.Client.View;
using CollectEggs.Bots;
using CollectEggs.Shared.Messages;
using CollectEggs.UI.Results;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollectEggs.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public event Action MatchStarted;
        public event Action<string, int> MatchEnded;

        [SerializeField]
        private float botGridCellSize = 0.5f;

        [SerializeField]
        private GameSceneContext sceneContext;

        private readonly List<PlayerEntity> _players = new();
        private readonly WinnerNameResolver _winnerNameResolver = new();
        private PlayerEntity _localPlayerEntity;
        private GameBootstrapper _localBootstrapper;
        private EggViewManager _eggViewManager;
        private PlayerSpawner _playerSpawner;
        private EggSpawner _eggSpawner;
        private ProximityEggCollector _proximityEggCollector;
        private MatchTimer _matchTimer;
        private ScoreService _scoreService;
        private EggCollectRequestController _eggCollectRequestController;
        private MatchHud _matchHud;
        private ResultsPanelView _resultsPanel;
        private GridMap _gridMap;
        private bool _matchExpiryHandled;

        public bool IsMatchRunning => _matchTimer != null && _matchTimer.IsRunning;
        public GameSceneContext SceneContext => sceneContext;

        private void Awake()
        {
            Instance = this;
            if (!EnsureDependencies())
                return;
            ConfigureNavigationGrid();
            _playerSpawner.SetNavigationGrid(_gridMap);
            _playerSpawner.RebuildSpawnParents();
            if (_localBootstrapper == null)
                Debug.LogError("GameBootstrapper is missing from Game/GameManager.");
        }

        public void ApplyServerMatchStarted(
            MatchStartedMessage message,
            IReadOnlyList<PlayerEntity> players,
            PlayerEntity localPlayer,
            GameBootstrapper localBootstrapper)
        {
            _localBootstrapper = localBootstrapper;
            if (_eggSpawner != null && _eggSpawner.EggPrefab != null)
                _eggViewManager.SetEggPrefab(_eggSpawner.EggPrefab);
            _players.Clear();
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (p == null)
                        continue;
                    _players.Add(p);
                }
            }

            _localPlayerEntity = localPlayer;
            if (_localPlayerEntity == null)
            {
                Debug.LogError("ApplyServerMatchStarted: local player missing.");
                return;
            }

            _matchExpiryHandled = false;
            _proximityEggCollector.Initialize(_localPlayerEntity);
            if (message != null)
            {
                _matchTimer.StartFromServerRules(message.rules.matchDuration);
                _proximityEggCollector.SetCollectRadiusFromServerRules(message.rules.eggCollectRadius);
                foreach (var p in _players)
                {
                    var bot = p != null ? p.GetComponent<BotController>() : null;
                    bot?.SetCollectReachFromServerRules(message.rules.eggCollectRadius);
                }
            }
            else
                _matchTimer.StartFromServerRules(0f);

            _eggCollectRequestController.BeginMatch(_players, localBootstrapper.ClientServerTransport);
            _resultsPanel.Hide();
            _matchHud.SetMatchEndSummaryVisible(true);
            MatchStarted?.Invoke();
            _matchHud.Initialize(_matchTimer, _scoreService, _resultsPanel, _localPlayerEntity.PlayerId, RestartMatch);
        }

        private bool EnsureDependencies()
        {
            if (sceneContext == null || !sceneContext.IsValid)
            {
                Debug.LogError("GameSceneContext is missing or incomplete.");
                return false;
            }

            _localBootstrapper = sceneContext.GameBootstrapper;
            _eggViewManager = sceneContext.EggViewManager;
            _playerSpawner = sceneContext.PlayerSpawner;
            _eggSpawner = sceneContext.EggSpawner;
            _proximityEggCollector = sceneContext.ProximityEggCollector;
            _matchTimer = sceneContext.MatchTimer;
            _scoreService = sceneContext.ScoreService;
            _eggCollectRequestController = sceneContext.EggCollectRequestController;
            _matchHud = sceneContext.MatchHud;
            _resultsPanel = sceneContext.ResultsPanelView;
            _gridMap = sceneContext.GridMap;
            return true;
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

        public void NotifyMatchEndedFromServer(IReadOnlyCollection<string> winnerPlayerIds)
        {
            if (_matchExpiryHandled || _matchTimer == null)
                return;
            _matchExpiryHandled = true;
            _matchTimer.StopFromServerState();
            HandleMatchEnded(winnerPlayerIds);
        }

        private void HandleMatchEnded(IReadOnlyCollection<string> winnerPlayerIds)
        {
            var sortedResults = MatchResultBuilder.Build(_players, _scoreService);
            _resultsPanel.Show(sortedResults);
            _matchHud.SetMatchEndSummaryVisible(false);
            var winners = WinnerNameResolver.Resolve(winnerPlayerIds, _players, sortedResults);
            if (winners.Count > 0)
            {
                var winnerNames = string.Join(", ", winners);
                var winnerScore = sortedResults.Count > 0 ? sortedResults[0].EggCount : 0;
                MatchEnded?.Invoke(winnerNames, winnerScore);
                Debug.Log($"Match ended. Winner: {winnerNames}. Final score: {winnerScore}");
            }
            else
            {
                MatchEnded?.Invoke(string.Empty, 0);
                Debug.Log("Match ended. Winner: No winner. Final score: 0");
            }
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

    }
}
