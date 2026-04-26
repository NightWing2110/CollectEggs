using UnityEngine;

namespace CollectEggs.Bots
{
    public partial class BotController
    {
        private void CheckStuck()
        {
            if (!IsFollowingPartialPath())
                return;

            AdvanceStuckTimer();
            if (!IsStuckSampleDue())
                return;

            EvaluateStuckRecovery();
        }

        private bool IsFollowingPartialPath() => _path.Count > 0 && _pathIndex < _path.Count;

        private void AdvanceStuckTimer() => _stuckTimer -= Time.deltaTime;

        private bool IsStuckSampleDue() => _stuckTimer <= 0f;

        private void EvaluateStuckRecovery()
        {
            _stuckTimer = stuckCheckInterval;
            var moved = (transform.position - _lastStuckPosition).magnitude;
            _lastStuckPosition = transform.position;

            if (moved >= stuckDistanceThreshold)
            {
                _stuckChecks = 0;
                return;
            }

            _stuckChecks++;
            if (_stuckChecks < maxStuckChecksBeforeRecover)
                return;

            _stuckChecks = 0;
            RefreshPath(true);
            if (!IsEggValid(CurrentTargetEgg) || _path.Count == 0 || _pathIndex >= _path.Count)
            {
                _state = BotState.Idle;
                return;
            }

            _pathIndex = Mathf.Min(_pathIndex + 1, _path.Count);
            _recoverDirection = Random.insideUnitCircle.normalized;
            _recoverTimer = recoverMoveDuration;
            _state = BotState.Recovering;
            if (!IsEggValid(CurrentTargetEgg)) return;
            _stuckRecoveriesOnTarget++;
            if (_stuckRecoveriesOnTarget < stuckRecoveriesBeforeEggIgnore) return;
            IgnoreEggTemporarily(CurrentTargetEgg, eggIgnoreSecondsAfterStuck);
            _stuckRecoveriesOnTarget = 0;
        }
    }
}
