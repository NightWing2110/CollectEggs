using System.Collections.Generic;
using CollectEggs.Client;
using CollectEggs.Client.View;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Players;
using CollectEggs.Networking.Transport;
using CollectEggs.Server.Simulation;
using UnityEngine;

namespace CollectEggs.Core
{
    [DefaultExecutionOrder(-90)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField]
        private ServerMatchConfig matchConfig = new();

        private ServerSimulator _simulator;
        private IGameTransport _transport;
        private ClientGameController _client;
        private EggViewManager _eggView;
        private PlayerSpawner _playerSpawner;

        public ServerSimulator Simulator => _simulator;
        public IGameTransport Transport => _transport;

        private void Start()
        {
            var gm = GetComponent<GameManager>();
            var eggSpawner = GetComponent<EggSpawner>();
            if (gm == null || eggSpawner == null)
                return;
            _playerSpawner = gm.GetOrAdd<PlayerSpawner>();
            _playerSpawner.ApplyDefaultPlayerPrefab(gm.PlayerPrefab);
            matchConfig.Normalize();
            _eggView = gm.GetOrAdd<EggViewManager>();
            if (eggSpawner.EggPrefab != null)
                _eggView.SetEggPrefab(eggSpawner.EggPrefab);
            _client = gm.GetOrAdd<ClientGameController>();
            _client.Wire(_playerSpawner, _eggView);
            var latency = new LatencyProfile(
                matchConfig.transportLatencyMinSeconds,
                matchConfig.transportLatencyMaxSeconds);
            _transport = new SimulatedTransport(latency);
            _client.AttachTransport(_transport, gm);
            var provider = new ServerSpawnPointProvider(matchConfig, eggSpawner);
            _simulator = new ServerSimulator(matchConfig, provider, _transport);
            _simulator.StartMatch();
        }

        private void Update()
        {
            _transport?.Tick(Time.deltaTime);
            _simulator?.Tick(Time.deltaTime);
        }

        public void NotifyEggCollectedForRespawn(
            string collectedEggId,
            IReadOnlyList<Vector3> livePlayerWorldPositions,
            IReadOnlyList<Vector3> occupiedEggWorldPositions)
        {
            _simulator?.RequestRespawnEggAfterCollect(collectedEggId, livePlayerWorldPositions, occupiedEggWorldPositions);
        }
    }
}
