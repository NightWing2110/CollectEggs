using System;
using System.Collections.Generic;
using CollectEggs.Networking.Transport;
using CollectEggs.Server.State;
using CollectEggs.Shared.Data;
using CollectEggs.Shared.Messages;
using CollectEggs.Shared.Snapshots;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public sealed class ServerSimulator
    {
        private readonly ServerMatchConfig _config;
        private readonly ISpawnPointProvider _spawnPointProvider;
        private readonly IGameTransport _transport;
        private System.Random _rng;
        private int _eggSpawnSequence;
        private ServerGameState _state;
        private float _nextSnapshotAt;

        public ServerSimulator(ServerMatchConfig config, ISpawnPointProvider spawnPointProvider, IGameTransport transport)
        {
            _config = config;
            _spawnPointProvider = spawnPointProvider;
            _transport = transport;
        }

        public void StartMatch()
        {
            _rng = _config.serverRandomSeed == 0
                ? new System.Random()
                : new System.Random(_config.serverRandomSeed);
            _eggSpawnSequence = 0;
            _state = new ServerGameState
            {
                ServerTime = 0f,
                RemainingTime = _config.matchDurationSeconds
            };

            var count = _config.playerCount;
            var spawnPositions = _spawnPointProvider.CreatePlayerSpawnPositions(count, _rng);
            var players = new List<PlayerSpawnData>();
            for (var i = 0; i < count; i++)
            {
                var pd = new PlayerSpawnData
                {
                    PlayerId = $"player_{i}",
                    DisplayName = i == 0 ? "You" : $"Bot {i}",
                    PlayerType = i == 0 ? PlayerType.Local : PlayerType.Bot,
                    IsLocalPlayer = i == 0,
                    SpawnPosition = spawnPositions[i],
                    MoveSpeed = _config.playerMoveSpeed
                };
                players.Add(pd);
                _state.Players[pd.PlayerId] = new ServerPlayerState
                {
                    PlayerId = pd.PlayerId,
                    DisplayName = pd.DisplayName,
                    PlayerType = pd.PlayerType,
                    IsLocalPlayer = pd.IsLocalPlayer,
                    Position = pd.SpawnPosition,
                    MoveSpeed = pd.MoveSpeed,
                    Score = 0
                };
            }

            var playerPositions = new List<Vector3>(players.Count);
            foreach (var p in players)
                playerPositions.Add(p.SpawnPosition);

            var eggs = new List<EggSpawnData>();
            var occupiedEggPositions = new List<Vector3>();
            var eggCount = _config.initialEggCount;
            for (var i = 0; i < eggCount; i++)
            {
                if (!_spawnPointProvider.GetEggSpawnPosition(i, _rng, playerPositions, occupiedEggPositions, out var pos))
                    continue;
                occupiedEggPositions.Add(pos);
                var ed = new EggSpawnData
                {
                    EggId = $"egg_{_eggSpawnSequence++}",
                    Position = pos,
                    Color = NextEggColor(),
                    ScoreValue = _config.eggScoreValue
                };
                eggs.Add(ed);
                _state.Eggs[ed.EggId] = new ServerEggState
                {
                    EggId = ed.EggId,
                    Position = ed.Position,
                    Color = ed.Color,
                    ScoreValue = ed.ScoreValue,
                    IsActive = true
                };
            }

            var started = new MatchStartedMessage
            {
                ServerTime = _state.ServerTime,
                Rules = new GameRulesSnapshot
                {
                    MatchDuration = _config.matchDurationSeconds,
                    PlayerMoveSpeed = _config.playerMoveSpeed,
                    EggCollectRadius = _config.eggCollectRadius,
                    PlayerCount = count,
                    InitialEggCount = eggCount
                },
                Players = players,
                Eggs = eggs
            };
            _transport.SendToClient(started);
            _nextSnapshotAt = _state.ServerTime + SampleSnapshotIntervalSeconds();
        }

        public void Tick(float deltaTime)
        {
            if (_state == null)
                return;
            _state.ServerTime += deltaTime;
            _state.RemainingTime = Mathf.Max(0f, _state.RemainingTime - deltaTime);
            if (_state.ServerTime < _nextSnapshotAt)
                return;
            var snapshot = ServerSnapshotBuilder.Build(_state);
            snapshot.ServerTime = _state.ServerTime;
            _transport.SendToClient(snapshot);
            _nextSnapshotAt = _state.ServerTime + SampleSnapshotIntervalSeconds();
        }

        public void RequestRespawnEggAfterCollect(string collectedEggId,
            IReadOnlyList<Vector3> livePlayerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions)
        {
            if (_state == null || _transport == null) return;
            if (!string.IsNullOrEmpty(collectedEggId) && _state.Eggs.TryGetValue(collectedEggId, out var prev))
                prev.IsActive = false;
            if (!TryCreateNextEggInternal(livePlayerWorldPositions, occupiedEggWorldPositions, out var data)) return;
            _state.Eggs[data.EggId] = new ServerEggState
            {
                EggId = data.EggId,
                Position = data.Position,
                Color = data.Color,
                ScoreValue = data.ScoreValue,
                IsActive = true
            };
            var msg = new EggSpawnedMessage { ServerTime = _state.ServerTime, Egg = data };
            _transport.SendToClient(msg);
        }

        private bool TryCreateNextEggInternal(
            IReadOnlyList<Vector3> livePlayerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions,
            out EggSpawnData egg)
        {
            if (!_spawnPointProvider.GetEggSpawnPosition(
                    _eggSpawnSequence,
                    _rng,
                    livePlayerWorldPositions,
                    occupiedEggWorldPositions,
                    out var pos))
            {
                egg = default;
                return false;
            }

            egg = new EggSpawnData
            {
                EggId = $"egg_{_eggSpawnSequence++}",
                Position = pos,
                Color = NextEggColor(),
                ScoreValue = _config.eggScoreValue
            };
            return true;
        }

        private float SampleSnapshotIntervalSeconds()
        {
            var lo = _config.snapshotIntervalMinSeconds;
            var hi = _config.snapshotIntervalMaxSeconds;
            return lo + (float)_rng.NextDouble() * (hi - lo);
        }

        private Color NextEggColor()
        {
            var h = (float)_rng.NextDouble();
            var s = 0.55f + 0.35f * (float)_rng.NextDouble();
            var v = 0.85f + 0.1f * (float)_rng.NextDouble();
            return Color.HSVToRGB(h, s, v);
        }
    }
}
