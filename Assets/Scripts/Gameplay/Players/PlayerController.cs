using CollectEggs.Core;
using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    [RequireComponent(typeof(PlayerMovement), typeof(PlayerEntity))]
    public class PlayerController : MonoBehaviour
    {
        private PlayerMovement _movement;
        private PlayerEntity _entity;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _entity = GetComponent<PlayerEntity>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsMatchRunning)
                return;
            var input = ReadInput4Way();
            if (input == Vector2.zero)
                return;
            _movement.Move(input);
        }

        private static Vector2 ReadInput4Way()
        {
            var hx = Input.GetAxisRaw("Horizontal");
            var hz = Input.GetAxisRaw("Vertical");
            var ax = Mathf.Abs(hx);
            var az = Mathf.Abs(hz);
            if (ax > 0.01f && az > 0.01f)
            {
                if (ax >= az)
                    hz = 0f;
                else
                    hx = 0f;
            }

            var x = Mathf.Abs(hx) > 0.01f ? Mathf.Sign(hx) : 0f;
            var z = Mathf.Abs(hz) > 0.01f ? Mathf.Sign(hz) : 0f;
            return new Vector2(x, z);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null)
                return;
            if (!hit.collider.CompareTag("Egg"))
                return;
            if (GameManager.Instance == null)
                return;
            GameManager.Instance.CollectEgg(_entity, hit.collider.gameObject);
        }
    }
}
