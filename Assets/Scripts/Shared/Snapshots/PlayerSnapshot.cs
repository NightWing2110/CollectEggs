using System;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public sealed class PlayerSnapshot
    {
        public string playerId;
        public Vector3 position;
        public int lastProcessedInputSequence;
    }
}
