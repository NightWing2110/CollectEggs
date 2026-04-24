using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Server.Simulation
{
    public interface ISpawnPointProvider
    {
        IReadOnlyList<Vector3> CreatePlayerSpawnPositions(int playerCount, System.Random rng);

        bool GetEggSpawnPosition(
            int eggIndex,
            System.Random rng,
            IReadOnlyList<Vector3> playerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions,
            out Vector3 position);
    }
}
