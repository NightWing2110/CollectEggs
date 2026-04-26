using CollectEggs.Client;
using CollectEggs.Client.View;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Navigation;
using CollectEggs.Gameplay.Players;
using CollectEggs.Gameplay.Scoring;
using CollectEggs.Gameplay.Timer;
using CollectEggs.UI.Results;
using UI;
using UnityEngine;

namespace CollectEggs.Core
{
    public sealed class GameSceneContext : MonoBehaviour
    {
        [SerializeField] private GameBootstrapper gameBootstrapper;
        [SerializeField] private ClientGameController clientGameController;
        [SerializeField] private PlayerSpawner playerSpawner;
        [SerializeField] private EggViewManager eggViewManager;
        [SerializeField] private MatchTimer matchTimer;
        [SerializeField] private ScoreService scoreService;
        [SerializeField] private EggCollectRequestController eggCollectRequestController;
        [SerializeField] private ProximityEggCollector proximityEggCollector;
        [SerializeField] private MatchHud matchHud;
        [SerializeField] private ResultsPanelView resultsPanelView;
        [SerializeField] private EggSpawner eggSpawner;
        [SerializeField] private GridMap gridMap;

        public GameBootstrapper GameBootstrapper => gameBootstrapper;
        public ClientGameController ClientGameController => clientGameController;
        public PlayerSpawner PlayerSpawner => playerSpawner;
        public EggViewManager EggViewManager => eggViewManager;
        public MatchTimer MatchTimer => matchTimer;
        public ScoreService ScoreService => scoreService;
        public EggCollectRequestController EggCollectRequestController => eggCollectRequestController;
        public ProximityEggCollector ProximityEggCollector => proximityEggCollector;
        public MatchHud MatchHud => matchHud;
        public ResultsPanelView ResultsPanelView => resultsPanelView;
        public EggSpawner EggSpawner => eggSpawner;
        public GridMap GridMap => gridMap;

        public bool IsValid =>
            gameBootstrapper != null &&
            clientGameController != null &&
            playerSpawner != null &&
            eggViewManager != null &&
            matchTimer != null &&
            scoreService != null &&
            eggCollectRequestController != null &&
            proximityEggCollector != null &&
            matchHud != null &&
            resultsPanelView != null &&
            eggSpawner != null &&
            gridMap != null;
    }
}
