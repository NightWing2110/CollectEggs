namespace CollectEggs.Shared.Messages
{
    public sealed class PlayerInputMessage : GameMessage
    {
        public string PlayerId;
        public float MoveX;
        public float MoveZ;
        public int InputSequence;
    }
}
