using System;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public struct EggSpawnData
    {
        public string EggId;
        public Vector3 Position;
        public Color Color;
        public int ScoreValue;
    }
}
