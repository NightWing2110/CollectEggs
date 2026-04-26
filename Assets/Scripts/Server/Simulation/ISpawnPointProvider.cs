using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public interface IServerWorldQuery
    {
        float MoveCheckRadius { get; }
        float EggSpawnCheckRadius { get; }
        float MinHorizontalDistanceFromPlayer { get; }

        bool GetEggSpawnBounds(out Vector3 min, out Vector3 max);
        bool SnapEggSpawnToGround(Vector3 position, out Vector3 snappedPosition);
        bool GetBlockingOverlap(Vector3 position, float radius);
        bool HasEggOverlap(Vector3 position, float radius);
    }

    public interface ISpawnPointProvider
    {
        IReadOnlyList<Vector3> CreatePlayerSpawnPositions(int playerCount, System.Random rng);
        bool ResolvePlayerMove(
            Vector3 currentPosition,
            Vector2 input,
            float speed,
            float deltaTime,
            out Vector3 resolvedPosition);

        bool GetEggSpawnPosition(System.Random rng,
            IReadOnlyList<Vector3> playerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions,
            out Vector3 position);
    }
}
