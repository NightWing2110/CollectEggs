using UnityEngine;

namespace CollectEggs.Shared.Messages
{
    public sealed class EggCollectRequestMessage : GameMessage
    {
        public string PlayerId;
        public string EggId;
        public Vector3 PlayerPosition;
    }
}
