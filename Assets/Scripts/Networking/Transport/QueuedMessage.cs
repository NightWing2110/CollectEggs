using CollectEggs.Shared.Messages;

namespace CollectEggs.Networking.Transport
{
    public sealed class QueuedMessage
    {
        public GameMessage Message { get; }
        public float DeliverAtTime { get; }

        public QueuedMessage(GameMessage message, float deliverAtTime)
        {
            Message = message;
            DeliverAtTime = deliverAtTime;
        }
    }
}
