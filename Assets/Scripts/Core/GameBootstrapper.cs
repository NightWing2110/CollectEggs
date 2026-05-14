using CollectEggs.Client;
using CollectEggs.Client.View;
using CollectEggs.Gameplay.Players;
using CollectEggs.Networking.Transport;
using CollectEggs.Server.Adapters;
using CollectEggs.Server.Simulation;
using UI;
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
        private LatencyProfile _latencyProfile;
        private ClientGameController _client;
        private EggViewManager _eggView;
        private PlayerSpawner _playerSpawner;
        private bool _isReadyToStart;
        private bool _matchStarted;

        public IGameTransport ClientServerTransport => _clientServerTransport;
        public bool CanStartMatch => _isReadyToStart && !_matchStarted;

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
            _latencyProfile = new LatencyProfile(
                localServerConfig.simulatedTransportLatencyMinSeconds,
                localServerConfig.simulatedTransportLatencyMaxSeconds);
            _clientServerTransport = new SimulatedTransport(_latencyProfile);
            _client.AttachTransport(_clientServerTransport, gm, context.MatchTimer);
            var worldQuery = new PhysicsServerWorldQuery(eggSpawner);
            var provider = new ServerSpawnPointProvider(localServerConfig, worldQuery);
            _serverSimulationController = new ServerSimulationController(localServerConfig, provider, _clientServerTransport);
            var startScreen = GetComponent<StartGameScreen>() ?? gameObject.AddComponent<StartGameScreen>();
            startScreen.Initialize(this);
            var debugPanel = GetComponent<NetworkSimulationDebugPanel>() ?? gameObject.AddComponent<NetworkSimulationDebugPanel>();
            debugPanel.Initialize(localServerConfig, _latencyProfile);
            _isReadyToStart = true;
        }

        private void Update()
        {
            _clientServerTransport?.Tick(Time.deltaTime);
            _serverSimulationController?.Tick(Time.deltaTime);
        }

        public void StartLocalMatch()
        {
            if (_matchStarted || _serverSimulationController == null)
                return;
            _matchStarted = true;
            _serverSimulationController.StartMatch();
        }
    }
}
