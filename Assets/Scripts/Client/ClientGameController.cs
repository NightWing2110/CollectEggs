using System.Collections.Generic;
using CollectEggs.Core;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Players;
using CollectEggs.Gameplay.Timer;
using CollectEggs.Client.View;
using CollectEggs.Networking.Transport;
using CollectEggs.Shared.Messages;
using UnityEngine;

namespace CollectEggs.Client
{
    public sealed class ClientGameController : MonoBehaviour
    {
        private PlayerSpawner _playerSpawner;
        private EggViewManager _eggViewManager;
        private IGameTransport _transport;
        private MatchTimer _matchTimer;
        private GameManager _gameManager;
        private EggCollectRequestController _eggCollectRequests;
        [SerializeField] private List<PlayerEntity> matchPlayers = new();
        private readonly Dictionary<string, PlayerEntity> _playersById = new();
        private readonly ClientMatchInitializer _matchInitializer = new();
        private readonly ClientSnapshotApplier _snapshotApplier = new();
        private PlayerEntity _localPlayerEntity;

        public void SetDependencies(
            PlayerSpawner playerSpawner,
            EggViewManager eggViewManager,
            EggCollectRequestController eggCollectRequests)
        {
            _playerSpawner = playerSpawner;
            _eggViewManager = eggViewManager;
            _eggCollectRequests = eggCollectRequests;
        }

        public void AttachTransport(IGameTransport transport, GameManager gameManager, MatchTimer matchTimer)
        {
            DetachTransport();
            _transport = transport;
            _gameManager = gameManager;
            _matchTimer = matchTimer;
            if (_transport != null)
                _transport.ClientMessageReceived += OnClientMessage;
        }

        private void OnDestroy() => DetachTransport();

        private void DetachTransport()
        {
            if (_transport != null)
                _transport.ClientMessageReceived -= OnClientMessage;
            _transport = null;
        }

        private void OnClientMessage(GameMessage message)
        {
            switch (message)
            {
                case MatchStartedMessage m:
                    HandleMatchStarted(m);
                    break;
                case GameStateSnapshotMessage s:
                    HandleGameStateSnapshot(s);
                    break;
                case MatchEndedMessage e:
                    HandleMatchEnded(e);
                    break;
            }
        }

        private void HandleMatchStarted(MatchStartedMessage message)
        {
            matchPlayers.Clear();
            _playersById.Clear();
            _localPlayerEntity = null;
            var initialization = ClientMatchInitializer.Initialize(message, _playerSpawner, _eggViewManager, _gameManager);
            matchPlayers.AddRange(initialization.Players);
            foreach (var player in initialization.PlayersById)
                _playersById[player.Key] = player.Value;
            _localPlayerEntity = initialization.LocalPlayer;
        }

        private void HandleGameStateSnapshot(GameStateSnapshotMessage message)
        {
            _snapshotApplier.ApplySnapshot(
                message,
                _matchTimer,
                _eggViewManager,
                _playerSpawner,
                _eggCollectRequests,
                _playersById,
                _localPlayerEntity);
        }

        private void HandleMatchEnded(MatchEndedMessage message)
        {
            if (message == null || _matchTimer == null)
                return;
            _matchTimer.SetRemainingSecondsFromNetwork(0f);
            _snapshotApplier.ApplyFinalScores(message, _eggCollectRequests);
            _gameManager?.NotifyMatchEndedFromServer(message.winnerPlayerIds);
        }
    }
}
