using System.Collections.Generic;
using System.Linq;
using CollectEggs.Networking.Transport;
using CollectEggs.Server.State;
using CollectEggs.Shared.Messages;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public sealed class ServerSimulationController
    {
        private sealed class PlayerInputState
        {
            public Vector2 Direction;
            public int Sequence;
        }

        private readonly ServerConfig _config;
        private readonly IGameTransport _transport;
        private readonly ServerMatchInitializer _matchInitializer;
        private readonly ServerMovementSystem _movementSystem;
        private readonly ServerEggSystem _eggSystem;
        private readonly Dictionary<string, PlayerInputState> _inputByPlayer = new();
        private System.Random _rng;
        private int _eggSpawnSequence;
        private ServerGameState _state;
        private float _nextSnapshotAt;
        private bool _serverMessageSubscribed;
        private bool _matchEndedSent;

        public ServerSimulationController(ServerConfig config, ISpawnPointProvider spawnPointProvider, IGameTransport transport)
        {
            _config = config;
            _transport = transport;
            _eggSystem = new ServerEggSystem(config, spawnPointProvider);
            _matchInitializer = new ServerMatchInitializer(config, spawnPointProvider, _eggSystem);
            _movementSystem = new ServerMovementSystem(spawnPointProvider);
        }

        public void StartMatch()
        {
            if (!_serverMessageSubscribed && _transport != null)
            {
                _transport.ServerMessageReceived += OnServerMessage;
                _serverMessageSubscribed = true;
            }
            _rng = new System.Random();
            _eggSpawnSequence = 0;
            _matchEndedSent = false;
            _inputByPlayer.Clear();
            var match = _matchInitializer.CreateInitialMatch(_rng, ref _eggSpawnSequence);
            _state = match.State;
            _transport?.SendToClient(match.Message);
            _nextSnapshotAt = _state.ServerTime + SampleSnapshotIntervalSeconds();
        }

        public void Tick(float deltaTime)
        {
            if (_state == null)
                return;
            var wasRunning = _state.RemainingTime > 0f;
            if (wasRunning)
                ApplyLocalPlayerMovement(deltaTime);
            _state.ServerTime += deltaTime;
            _state.RemainingTime = Mathf.Max(0f, _state.RemainingTime - deltaTime);
            if (wasRunning && _state.RemainingTime <= 0f)
                SendMatchEnded();
            if (_state.ServerTime < _nextSnapshotAt)
                return;
            var snapshot = ServerSnapshotBuilder.Build(_state);
            snapshot.ServerTime = _state.ServerTime;
            _transport.SendToClient(snapshot);
            _nextSnapshotAt = _state.ServerTime + SampleSnapshotIntervalSeconds();
        }

        private void OnServerMessage(GameMessage message)
        {
            switch (message)
            {
                case PlayerInputMessage input:
                    HandlePlayerInput(input);
                    break;
                case EggCollectRequestMessage eggCollect:
                    _eggSystem.HandleCollectRequest(_state, eggCollect, _rng, ref _eggSpawnSequence);
                    break;
            }
        }

        private void HandlePlayerInput(PlayerInputMessage input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.PlayerId))
                return;
            var dir = new Vector2(input.MoveX, input.MoveZ);
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();
            if (!_inputByPlayer.TryGetValue(input.PlayerId, out var state))
            {
                state = new PlayerInputState();
                _inputByPlayer[input.PlayerId] = state;
            }

            state.Direction = dir;
            state.Sequence = input.InputSequence;
        }

        private void ApplyLocalPlayerMovement(float deltaTime)
        {
            if (_state == null)
                return;
            var localPlayer = _state.Players.Values.FirstOrDefault(player => player.IsLocalClientPlayer);
            if (localPlayer == null)
                return;
            if (!_inputByPlayer.TryGetValue(localPlayer.PlayerId, out var inputState))
                return;
            _movementSystem.ApplyLocalPlayerMovement(
                _state,
                localPlayer.PlayerId,
                inputState.Direction,
                inputState.Sequence,
                deltaTime);
        }

        private float SampleSnapshotIntervalSeconds()
        {
            var lo = _config.snapshotIntervalMinSeconds;
            var hi = _config.snapshotIntervalMaxSeconds;
            return lo + (float)_rng.NextDouble() * (hi - lo);
        }

        private void SendMatchEnded()
        {
            if (_matchEndedSent)
                return;
            _matchEndedSent = true;
            _transport?.SendToClient(ServerSnapshotBuilder.BuildMatchEnded(_state));
        }
    }
}
