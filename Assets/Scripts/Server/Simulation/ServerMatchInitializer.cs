using System.Collections.Generic;
using System.Linq;
using CollectEggs.Server.State;
using CollectEggs.Shared.Data;
using CollectEggs.Shared.Messages;
using CollectEggs.Shared.Snapshots;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public readonly struct ServerMatchInitialization
    {
        public readonly ServerGameState State;
        public readonly MatchStartedMessage Message;

        public ServerMatchInitialization(ServerGameState state, MatchStartedMessage message)
        {
            State = state;
            Message = message;
        }
    }

    public sealed class ServerMatchInitializer
    {
        private readonly ServerConfig _config;
        private readonly ISpawnPointProvider _spawnPointProvider;
        private readonly ServerEggSystem _eggSystem;

        public ServerMatchInitializer(ServerConfig config, ISpawnPointProvider spawnPointProvider, ServerEggSystem eggSystem)
        {
            _config = config;
            _spawnPointProvider = spawnPointProvider;
            _eggSystem = eggSystem;
        }

        public ServerMatchInitialization CreateInitialMatch(System.Random rng, ref int eggSpawnSequence)
        {
            var state = new ServerGameState
            {
                ServerTime = 0f,
                RemainingTime = _config.matchDurationSeconds
            };
            const int count = ServerConfig.PlayerCount;
            var spawnPositions = _spawnPointProvider.CreatePlayerSpawnPositions(count, rng);
            var players = CreatePlayers(state, spawnPositions, count);
            var playerPositions = players.Select(p => p.spawnPosition).ToList();
            var eggs = _eggSystem.CreateInitialEggs(state, playerPositions, rng, ref eggSpawnSequence);
            return new ServerMatchInitialization(state, new MatchStartedMessage
            {
                ServerTime = state.ServerTime,
                rules = new GameRulesSnapshot
                {
                    matchDuration = _config.matchDurationSeconds,
                    eggCollectRadius = _config.eggCollectRadius
                },
                players = players,
                eggs = eggs
            });
        }

        private List<PlayerSpawnData> CreatePlayers(
            ServerGameState state,
            IReadOnlyList<Vector3> spawnPositions,
            int count)
        {
            var players = new List<PlayerSpawnData>(count);
            for (var i = 0; i < count; i++)
            {
                var data = new PlayerSpawnData
                {
                    playerId = $"player_{i}",
                    displayName = i == 0 ? "You" : $"Bot {i}",
                    playerType = i == 0 ? PlayerType.Local : PlayerType.Bot,
                    isLocalClientPlayer = i == 0,
                    spawnPosition = spawnPositions[i],
                    moveSpeed = _config.playerMoveSpeed
                };
                players.Add(data);
                state.Players[data.playerId] = new ServerPlayerState
                {
                    PlayerId = data.playerId,
                    IsLocalClientPlayer = data.isLocalClientPlayer,
                    Position = data.spawnPosition,
                    MoveSpeed = data.moveSpeed,
                    Score = 0
                };
            }

            return players;
        }
    }
}
