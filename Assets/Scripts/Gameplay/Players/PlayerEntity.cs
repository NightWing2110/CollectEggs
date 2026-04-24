using CollectEggs.Gameplay.Movement;
using CollectEggs.Shared.Data;
using CollectEggs.Shared.Snapshots;
using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    public class PlayerEntity : MonoBehaviour
    {
        [SerializeField]
        private string playerId = "local";

        [SerializeField]
        private string displayName = "Local Player";

        [SerializeField]
        private bool isLocal = true;

        [SerializeField]
        private PlayerType playerType = PlayerType.Local;

        [SerializeField]
        private ActorMovement movement;

        [SerializeField]
        private PlayerController controller;

        public string PlayerId => playerId;
        public string DisplayName => displayName;
        public bool IsLocal => isLocal;
        public PlayerType Type => playerType;
        public ActorMovement Movement => movement;
        public PlayerController Controller => controller;

        public void ConfigureFromServer(PlayerSpawnData data, ActorMovement actorMovement, PlayerController playerController)
        {
            playerId = data.PlayerId;
            displayName = data.DisplayName;
            isLocal = data.IsLocalPlayer;
            playerType = data.PlayerType;
            movement = actorMovement;
            controller = playerController;
            if (movement != null)
                movement.SetMoveSpeedFromAuthority(data.MoveSpeed);
        }
    }
}
