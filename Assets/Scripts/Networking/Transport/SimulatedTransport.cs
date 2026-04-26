using System;
using System.Collections.Generic;
using CollectEggs.Shared.Messages;
using UnityEngine;

namespace CollectEggs.Networking.Transport
{
    public sealed class SimulatedTransport : IGameTransport
    {
        private readonly LatencyProfile _latency;
        private readonly List<QueuedMessage> _serverToClient = new();
        private readonly List<QueuedMessage> _clientToServer = new();

        public SimulatedTransport(LatencyProfile latency) => _latency = latency ?? new LatencyProfile(0f, 0f);

        public event Action<GameMessage> ClientMessageReceived;
        public event Action<GameMessage> ServerMessageReceived;

        public void SendToClient(GameMessage message)
        {
            if (message == null)
                return;
            var deliverAt = Time.time + _latency.SampleDelaySeconds();
            _serverToClient.Add(new QueuedMessage(message, deliverAt));
        }

        public void SendToServer(GameMessage message)
        {
            if (message == null)
                return;
            var deliverAt = Time.time + _latency.SampleDelaySeconds();
            _clientToServer.Add(new QueuedMessage(message, deliverAt));
        }

        public void Tick(float deltaTime)
        {
            var t = Time.time;
            DrainQueue(_serverToClient, t, ClientMessageReceived);
            DrainQueue(_clientToServer, t, ServerMessageReceived);
        }

        private static void DrainQueue(List<QueuedMessage> queue, float now, Action<GameMessage> dispatch)
        {
            if (dispatch == null)
                return;
            for (var i = 0; i < queue.Count;)
            {
                var q = queue[i];
                if (q.DeliverAtTime <= now)
                {
                    dispatch(q.Message);
                    queue.RemoveAt(i);
                }
                else
                    i++;
            }
        }
    }
}
