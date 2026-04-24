using System.Collections.Generic;

namespace CollectEggs.Server.State
{
    public sealed class ServerGameState
    {
        public float ServerTime;
        public float RemainingTime;
        public Dictionary<string, ServerPlayerState> Players { get; } = new();
        public Dictionary<string, ServerEggState> Eggs { get; } = new();
    }
}
