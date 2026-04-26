using System;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public struct EggSpawnData
    {
        public string eggId;
        public Vector3 position;
        public Color color;
        public int scoreValue;
    }
}
