using System.Collections.Generic;
using System.Linq;
using CollectEggs.Server.State;
using CollectEggs.Shared.Messages;
using CollectEggs.Shared.Snapshots;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public sealed class ServerEggSystem
    {
        private readonly ServerConfig _config;
        private readonly ISpawnPointProvider _spawnPointProvider;

        public ServerEggSystem(ServerConfig config, ISpawnPointProvider spawnPointProvider)
        {
            _config = config;
            _spawnPointProvider = spawnPointProvider;
        }

        public List<EggSpawnData> CreateInitialEggs(
            ServerGameState state,
            IReadOnlyList<Vector3> playerPositions,
            System.Random rng,
            ref int eggSpawnSequence)
        {
            var eggs = new List<EggSpawnData>();
            var occupiedEggPositions = new List<Vector3>();
            for (var i = 0; i < _config.initialEggCount; i++)
            {
                if (!CreateNextEgg(rng, playerPositions, occupiedEggPositions, ref eggSpawnSequence, out var egg))
                    continue;
                occupiedEggPositions.Add(egg.position);
                eggs.Add(egg);
                state.Eggs[egg.eggId] = new ServerEggState
                {
                    EggId = egg.eggId,
                    Position = egg.position,
                    Color = egg.color,
                    ScoreValue = egg.scoreValue,
                    IsActive = true
                };
            }

            return eggs;
        }

        public void HandleCollectRequest(
            ServerGameState state,
            EggCollectRequestMessage message,
            System.Random rng,
            ref int eggSpawnSequence)
        {
            if (state == null ||
                message == null ||
                state.RemainingTime <= 0f ||
                string.IsNullOrWhiteSpace(message.PlayerId) ||
                string.IsNullOrWhiteSpace(message.EggId))
            {
                return;
            }

            if (!state.Players.TryGetValue(message.PlayerId, out var player) ||
                !state.Eggs.TryGetValue(message.EggId, out var egg) ||
                !egg.IsActive)
            {
                return;
            }

            if (!player.IsLocalClientPlayer)
                player.Position = message.PlayerPosition;
            if (!CanCollectEgg(player.Position, egg.Position))
                return;
            player.Score += Mathf.Max(0, egg.ScoreValue);
            egg.IsActive = false;
            egg.CollectedByPlayerId = player.PlayerId;
            if (!CreateNextEgg(rng, GetLivePlayerPositions(state), GetActiveEggPositions(state), ref eggSpawnSequence, out var nextEgg))
                return;
            state.Eggs[nextEgg.eggId] = new ServerEggState
            {
                EggId = nextEgg.eggId,
                Position = nextEgg.position,
                Color = nextEgg.color,
                ScoreValue = nextEgg.scoreValue,
                IsActive = true,
                CollectedByPlayerId = string.Empty
            };
        }

        private bool CanCollectEgg(Vector3 playerPosition, Vector3 eggPosition)
        {
            var dx = playerPosition.x - eggPosition.x;
            var dz = playerPosition.z - eggPosition.z;
            var radius = Mathf.Max(0.01f, _config.eggCollectRadius + _config.eggCollectValidationSlack);
            return dx * dx + dz * dz <= radius * radius;
        }

        private bool CreateNextEgg(
            System.Random rng,
            IReadOnlyList<Vector3> livePlayerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions,
            ref int eggSpawnSequence,
            out EggSpawnData egg)
        {
            if (!_spawnPointProvider.GetEggSpawnPosition(rng, livePlayerWorldPositions, occupiedEggWorldPositions, out var pos))
            {
                egg = default;
                return false;
            }

            egg = new EggSpawnData
            {
                eggId = $"egg_{eggSpawnSequence++}",
                position = pos,
                color = NextEggColor(rng),
                scoreValue = _config.eggScoreValue
            };
            return true;
        }

        private static List<Vector3> GetLivePlayerPositions(ServerGameState state)
        {
            var positions = new List<Vector3>(state.Players.Count);
            positions.AddRange(state.Players.Select(pair => pair.Value.Position));
            return positions;
        }

        private static List<Vector3> GetActiveEggPositions(ServerGameState state)
        {
            var positions = new List<Vector3>(state.Eggs.Count);
            positions.AddRange(from pair in state.Eggs select pair.Value into egg where egg.IsActive select egg.Position);
            return positions;
        }

        private static Color NextEggColor(System.Random rng)
        {
            var h = (float)rng.NextDouble();
            var s = 0.55f + 0.35f * (float)rng.NextDouble();
            var v = 0.85f + 0.1f * (float)rng.NextDouble();
            return Color.HSVToRGB(h, s, v);
        }
    }
}
