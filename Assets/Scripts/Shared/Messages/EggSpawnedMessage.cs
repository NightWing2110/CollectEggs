using CollectEggs.Shared.Snapshots;

namespace CollectEggs.Shared.Messages
{
    public sealed class EggSpawnedMessage : GameMessage
    {
        public EggSpawnData Egg;
    }
}
