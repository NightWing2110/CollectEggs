namespace CollectEggs.Shared.Messages
{
    public sealed class EggCollectedMessage : GameMessage
    {
        public string CollectorPlayerId;
        public string EggId;
    }
}
