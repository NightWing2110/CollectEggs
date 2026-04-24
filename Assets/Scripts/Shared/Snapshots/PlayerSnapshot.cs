using System;
using UnityEngine;

namespace CollectEggs.Shared.Snapshots
{
    [Serializable]
    public sealed class PlayerSnapshot
    {
        public string PlayerId;
        public Vector3 Position;
        public int Score;
    }
}
