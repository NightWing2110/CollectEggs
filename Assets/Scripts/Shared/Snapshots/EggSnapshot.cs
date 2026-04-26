using System;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public sealed class EggSnapshot
    {
        public string eggId;
        public Vector3 position;
        public Color color;
        public int scoreValue;
        public bool isActive;
        public string collectedByPlayerId;
    }
}
