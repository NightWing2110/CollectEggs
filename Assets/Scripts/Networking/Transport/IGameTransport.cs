using System;
using CollectEggs.Shared.Messages;

namespace CollectEggs.Networking.Transport
{
    public interface IGameTransport
    {
        void SendToClient(GameMessage message);

        void SendToServer(GameMessage message);

        event Action<GameMessage> ClientMessageReceived;

        event Action<GameMessage> ServerMessageReceived;

        void Tick(float deltaTime);
    }
}
