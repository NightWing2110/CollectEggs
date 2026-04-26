using CollectEggs.Server.State;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public sealed class ServerMovementSystem
    {
        private readonly ISpawnPointProvider _spawnPointProvider;

        public ServerMovementSystem(ISpawnPointProvider spawnPointProvider) => _spawnPointProvider = spawnPointProvider;

        public void ApplyLocalPlayerMovement(
            ServerGameState state,
            string playerId,
            Vector2 direction,
            int sequence,
            float deltaTime)
        {
            if (state == null || string.IsNullOrWhiteSpace(playerId) || !state.Players.TryGetValue(playerId, out var player))
                return;
            _spawnPointProvider.ResolvePlayerMove(
                player.Position,
                direction,
                player.MoveSpeed,
                deltaTime,
                out var nextPosition);
            player.Position = nextPosition;
            player.LastProcessedInputSequence = sequence;
        }
    }
}
