using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 6f;

        [SerializeField]
        private float gravityMultiplier = 1f;

        private CharacterController _characterController;

        private void Awake() => _characterController = GetComponent<CharacterController>();

        public void Move(Vector2 direction)
        {
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();
            var delta = moveSpeed * Time.deltaTime;
            var motion = new Vector3(direction.x * delta, 0f, direction.y * delta);
            if (_characterController != null)
            {
                motion.y = Physics.gravity.y * gravityMultiplier * Time.deltaTime;
                _characterController.Move(motion);
                return;
            }

            transform.position += motion;
        }
    }
}
