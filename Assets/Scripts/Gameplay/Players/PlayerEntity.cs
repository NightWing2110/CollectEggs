using CollectEggs.Gameplay.Movement;
using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    public enum PlayerType
    {
        Local = 0,
        Bot = 1,
        Remote = 2
    }

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

        public void Configure(string id, string name, bool local, PlayerType type, ActorMovement actorMovement, PlayerController playerController)
        {
            playerId = id;
            displayName = name;
            isLocal = local;
            playerType = type;
            movement = actorMovement;
            controller = playerController;
        }
    }
}
