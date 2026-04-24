using CollectEggs.Shared.Messages;

namespace CollectEggs.Networking.Transport
{
    public sealed class QueuedMessage
    {
        public GameMessage Message { get; }
        public float DeliverAtUnityTime { get; }

        public QueuedMessage(GameMessage message, float deliverAtUnityTime)
        {
            Message = message;
            DeliverAtUnityTime = deliverAtUnityTime;
        }
    }
}
