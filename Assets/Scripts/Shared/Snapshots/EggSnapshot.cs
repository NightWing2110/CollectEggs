using System;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public sealed class EggSnapshot
    {
        public string EggId;
        public Vector3 Position;
        public Color Color;
        public int ScoreValue;
        public bool IsActive;
    }
}
