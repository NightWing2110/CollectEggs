using System.Collections.Generic;
using CollectEggs.Core;
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

        public IReadOnlyList<PlayerEntity> LastPlayers => _lastPlayers;
        public PlayerEntity LocalPlayerEntity => _localPlayerEntity;

        private readonly List<PlayerEntity> _lastPlayers = new();
        private PlayerEntity _localPlayerEntity;

        public void Wire(PlayerSpawner playerSpawner, EggViewManager eggViewManager)
        {
            _playerSpawner = playerSpawner;
            _eggViewManager = eggViewManager;
        }

        public void AttachTransport(IGameTransport transport, GameManager gameManager)
        {
            DetachTransport();
            _transport = transport;
            _gameManager = gameManager;
            if (gameManager != null)
                _matchTimer = gameManager.GetComponent<MatchTimer>();
            if (_transport != null)
                _transport.ClientMessageReceived += OnClientMessage;
        }

        private void OnDestroy()
        {
            DetachTransport();
        }

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
                case EggSpawnedMessage e:
                    HandleEggSpawned(e);
                    break;
            }
        }

        private void HandleMatchStarted(MatchStartedMessage message)
        {
            _lastPlayers.Clear();
            _localPlayerEntity = null;
            if (message == null || _playerSpawner == null || _eggViewManager == null)
                return;
            _playerSpawner.RebuildSpawnParents();
            var botVisualIndex = 0;
            foreach (var player in message.Players)
            {
                var entity = _playerSpawner.SpawnFromServerData(player, botVisualIndex);
                if (entity == null)
                    continue;
                _lastPlayers.Add(entity);
                if (player.IsLocalPlayer)
                    _localPlayerEntity = entity;
                else
                    botVisualIndex++;
            }

            var root = _playerSpawner.EggsRoot;
            _eggViewManager.ClearTrackedEggsForNewMatch();
            foreach (var egg in message.Eggs)
                _eggViewManager.SpawnFromServerData(egg, root);
            var gm = GameManager.Instance;
            var boot = gm != null ? gm.GetComponent<GameBootstrapper>() : null;
            if (gm != null && boot != null)
                gm.ApplyMatchStarted(message, _lastPlayers, _localPlayerEntity, boot);
        }

        private void HandleGameStateSnapshot(GameStateSnapshotMessage message)
        {
            if (message == null || _matchTimer == null)
                return;
            _matchTimer.SetRemainingSecondsFromNetwork(message.RemainingTime);
            if (message.RemainingTime <= 0f)
                _gameManager?.NotifyMatchExpiredFromNetwork();
        }

        private void HandleEggSpawned(EggSpawnedMessage message)
        {
            if (message == null || _eggViewManager == null || _playerSpawner == null)
                return;
            _eggViewManager.SpawnFromServerData(message.Egg, _playerSpawner.EggsRoot);
        }
    }
}
