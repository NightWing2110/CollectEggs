using CollectEggs.Gameplay.Movement;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Core;
using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    [RequireComponent(typeof(ActorMovement), typeof(PlayerEntity))]
    public class PlayerController : MonoBehaviour
    {
        private enum PlayerState
        {
            Disabled = 0,
            Active = 1
        }

        private ActorMovement _movement;
        private PlayerEntity _entity;
        private PlayerState _state = PlayerState.Active;

        private void Awake()
        {
            _movement = GetComponent<ActorMovement>();
            _entity = GetComponent<PlayerEntity>();
        }

        private void Update()
        {
            _state = GameManager.Instance != null && !GameManager.Instance.IsMatchRunning
                ? PlayerState.Disabled
                : PlayerState.Active;
            if (_state == PlayerState.Disabled)
            {
                _movement.Move(Vector2.zero);
                return;
            }

            var input = ReadInput();
            _movement.Move(input);
        }

        private static Vector2 ReadInput()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var z = Input.GetAxisRaw("Vertical");
            var result = new Vector2(x, z);
            return result.sqrMagnitude > 1f ? result.normalized : result;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            EggCollectProximity.CollectFromCollider(_entity, hit.collider);
        }
    }
}
