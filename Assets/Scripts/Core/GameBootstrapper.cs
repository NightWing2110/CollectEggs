using CollectEggs.Client;
using CollectEggs.Client.View;
using CollectEggs.Gameplay.Players;
using CollectEggs.Networking.Transport;
using CollectEggs.Server.Adapters;
using CollectEggs.Server.Simulation;
using UnityEngine;

namespace CollectEggs.Core
{
    [DefaultExecutionOrder(-90)]
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField]
        private ServerConfig localServerConfig = new();

        private ServerSimulationController _serverSimulationController;
        private IGameTransport _clientServerTransport;
        private ClientGameController _client;
        private EggViewManager _eggView;
        private PlayerSpawner _playerSpawner;

        public IGameTransport ClientServerTransport => _clientServerTransport;

        private void Start()
        {
            var gm = GetComponent<GameManager>();
            var context = gm != null ? gm.SceneContext : null;
            if (gm == null || context == null || !context.IsValid)
                return;
            var eggSpawner = context.EggSpawner;
            _playerSpawner = context.PlayerSpawner;
            localServerConfig.Normalize();
            _eggView = context.EggViewManager;
            if (eggSpawner.EggPrefab != null)
                _eggView.SetEggPrefab(eggSpawner.EggPrefab);
            _client = context.ClientGameController;
            _client.SetDependencies(_playerSpawner, _eggView, context.EggCollectRequestController);
            var latency = new LatencyProfile(
                localServerConfig.simulatedTransportLatencyMinSeconds,
                localServerConfig.simulatedTransportLatencyMaxSeconds);
            _clientServerTransport = new SimulatedTransport(latency);
            _client.AttachTransport(_clientServerTransport, gm, context.MatchTimer);
            var worldQuery = new PhysicsServerWorldQuery(eggSpawner);
            var provider = new ServerSpawnPointProvider(localServerConfig, worldQuery);
            _serverSimulationController = new ServerSimulationController(localServerConfig, provider, _clientServerTransport);
            _serverSimulationController.StartMatch();
        }

        private void Update()
        {
            _clientServerTransport?.Tick(Time.deltaTime);
            _serverSimulationController?.Tick(Time.deltaTime);
        }
    }
}
