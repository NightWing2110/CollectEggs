using CollectEggs.Gameplay.Movement;
using CollectEggs.Shared.Data;
using CollectEggs.Shared.Snapshots;
using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    public class PlayerEntity : MonoBehaviour
    {
        [SerializeField]
        private string playerId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField]
        private bool isLocal;

        [SerializeField]
        private PlayerType playerType = PlayerType.Local;

        [SerializeField]
        private ActorMovement movement;

        [SerializeField]
        private PlayerController controller;

        public string PlayerId => playerId;
        public string DisplayName => displayName;
        public PlayerController Controller => controller;

        public void ConfigureFromServer(PlayerSpawnData data, ActorMovement actorMovement, PlayerController playerController)
        {
            playerId = data.playerId;
            displayName = data.displayName;
            isLocal = data.isLocalClientPlayer;
            playerType = data.playerType;
            movement = actorMovement;
            controller = playerController;
            if (movement != null)
                movement.SetMoveSpeedFromServerRules(data.moveSpeed);
        }
    }
}
