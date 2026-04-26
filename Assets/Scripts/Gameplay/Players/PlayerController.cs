using CollectEggs.Gameplay.Movement;
using CollectEggs.Gameplay.Collection;
using CollectEggs.Core;
using CollectEggs.Networking.Transport;
using CollectEggs.Shared.Messages;
using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.Gameplay.Players
{
    [RequireComponent(typeof(ActorMovement), typeof(PlayerEntity))]
    public class PlayerController : MonoBehaviour
    {
        private readonly struct PendingInput
        {
            public readonly int Sequence;
            public readonly Vector2 Direction;
            public readonly float DeltaTime;

            public PendingInput(int sequence, Vector2 direction, float deltaTime)
            {
                Sequence = sequence;
                Direction = direction;
                DeltaTime = deltaTime;
            }
        }

        private enum PlayerState
        {
            Disabled = 0,
            Active = 1
        }

        private ActorMovement _movement;
        private PlayerEntity _entity;
        private IGameTransport _transport;
        private PlayerState _state = PlayerState.Active;
        private int _nextInputSequence;
        private readonly List<PendingInput> _pendingInputs = new();

        [SerializeField] private bool enableClientPrediction;

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
                SendInputToServer(Vector2.zero);
                _movement.Move(Vector2.zero);
                return;
            }

            var input = ReadInput();
            var sentSequence = SendInputToServer(input);
            if (sentSequence > 0)
                _pendingInputs.Add(new PendingInput(sentSequence, input, Time.deltaTime));
            if (enableClientPrediction) _movement.Move(input);
        }

        private static Vector2 ReadInput()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var z = Input.GetAxisRaw("Vertical");
            var result = new Vector2(x, z);
            return result.sqrMagnitude > 1f ? result.normalized : result;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit) => EggCollectProximity.RequestCollectFromEggCollider(_entity, hit.collider);

        public void ReconcileFromServer(Vector3 authoritativePosition, int lastProcessedInputSequence)
        {
            transform.position = authoritativePosition;
            if (_pendingInputs.Count > 0)
                _pendingInputs.RemoveAll(x => x.Sequence <= lastProcessedInputSequence);
            for (var i = 0; i < _pendingInputs.Count; i++)
                transform.position +=
                    _movement.CalculatePlanarMoveDelta(_pendingInputs[i].Direction, _pendingInputs[i].DeltaTime);
        }

        private int SendInputToServer(Vector2 input)
        {
            if (_transport == null)
            {
                var bootstrapper = GameManager.Instance != null
                    ? GameManager.Instance.GetComponent<GameBootstrapper>()
                    : null;
                _transport = bootstrapper != null ? bootstrapper.ClientServerTransport : null;
            }

            if (_transport == null || _entity == null || string.IsNullOrWhiteSpace(_entity.PlayerId))
                return 0;
            var sequence = ++_nextInputSequence;
            _transport.SendToServer(new PlayerInputMessage
            {
                PlayerId = _entity.PlayerId,
                MoveX = input.x,
                MoveZ = input.y,
                InputSequence = sequence
            });
            return sequence;
        }
    }
}