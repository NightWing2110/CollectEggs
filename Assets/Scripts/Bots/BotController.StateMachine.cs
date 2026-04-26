using UnityEngine;

namespace CollectEggs.Bots
{
    public partial class BotController
    {
        private void TickState()
        {
            switch (_state)
            {
                case BotState.Recovering:
                    TickRecovering();
                    return;
                case BotState.Idle:
                {
                    if (_targetEgg != null && _path.Count > 0 && _pathIndex < _path.Count)
                        _state = BotState.Chasing;
                    else
                        _movement.Move(Vector2.zero);
                    return;
                }
                case BotState.Chasing:
                default:
                    TickChasing();
                    break;
            }
        }

        private void TickChasing()
        {
            FollowPath();
            CheckStuck();
            var pathDone = _path.Count > 0 && _pathIndex >= _path.Count;
            if (pathDone && IsEggValid(_targetEgg))
            {
                var refXZ = ResolveHorizontalReference(transform, _cc);
                var e = _targetEgg.transform.position;
                var dx = e.x - refXZ.x;
                var dz = e.z - refXZ.y;
                var d = Mathf.Sqrt(dx * dx + dz * dz);
                var pickupHorizon = _authorityCollectReach + collectProximitySlack + (_cc != null ? _cc.radius * 0.4f : 0f);
                if (d > pickupHorizon + 0.18f)
                    IgnoreEggTemporarily(_targetEgg, eggIgnoreSecondsAfterPathFail);
            }

            if (!IsEggValid(_targetEgg) || _path.Count == 0 || pathDone)
                _state = BotState.Idle;
        }

        private void TickRecovering()
        {
            if (_recoverTimer > 0f)
            {
                _recoverTimer -= Time.deltaTime;
                _movement.Move(_recoverDirection);
                return;
            }

            _state = BotState.Chasing;
        }
    }
}
