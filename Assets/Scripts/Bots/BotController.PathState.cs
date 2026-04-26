using System.Collections.Generic;
using CollectEggs.Gameplay.Eggs;
using UnityEngine;

namespace CollectEggs.Bots
{
    public partial class BotController
    {
        private void RefreshPath(bool forceWhileCoasting = false)
        {
            if (!IsEggValid(_targetEgg))
            {
                ClearTargetState();
                return;
            }

            if (!forceWhileCoasting && _state == BotState.Chasing && _path.Count > 0 && _pathIndex < _path.Count)
                return;

            if (!PlanPathToEgg(_targetEgg, _bestEggPathScratch, out var plan))
            {
                IgnoreEggTemporarily(_targetEgg, eggIgnoreSecondsAfterPathFail);
                return;
            }

            SetPathAndApproach(_bestEggPathScratch, plan.ApproachWorld);
        }

        private bool PlanPathToEgg(EggEntity egg, List<Vector3> pathOut, out ApproachPathResult plan)
        {
            var request = new BotPathPlanRequest(
                _gridMap,
                transform.position,
                _cc,
                _authorityCollectReach,
                approachMargin,
                earlyExitPathCellCount,
                earlyExitPathCostMultiplier,
                egg,
                pathOut,
                _astarBuffer);
            return BotPathPlanner.PlanPathToEgg(request, out plan);
        }

        private void FollowPath() => BotPathFollower.Follow(transform, _movement, _path, ref _pathIndex, waypointReachDistance);

        private void SetTarget(EggEntity target, List<Vector3> path, Vector3 approachWorld)
        {
            if (_targetEgg != target)
            {
                _stuckRecoveriesOnTarget = 0;
                _coastRetargetSkips = 0;
            }

            _targetEgg = target;
            _pathEggAnchor = target.transform.position;
            _approachGoalWorld = approachWorld;
            _hasApproachGoal = true;
            SetPath(path);
            _state = _path.Count > 0 ? BotState.Chasing : BotState.Idle;
        }

        private void SetPathAndApproach(List<Vector3> path, Vector3 approachWorld)
        {
            _approachGoalWorld = approachWorld;
            _hasApproachGoal = true;
            SetPath(path);
            if (_targetEgg != null)
                _pathEggAnchor = _targetEgg.transform.position;
        }

        private void SetPath(List<Vector3> path)
        {
            _path.Clear();
            if (path != null)
                _path.AddRange(path);
            _pathIndex = 0;
        }

        private void ClearTargetState()
        {
            _targetEgg = null;
            _path.Clear();
            _pathIndex = 0;
            _state = BotState.Idle;
            _hasApproachGoal = false;
            _stuckRecoveriesOnTarget = 0;
            _coastRetargetSkips = 0;
        }

        private void IgnoreEggTemporarily(EggEntity egg, float seconds)
        {
            if (egg == null)
                return;
            var id = egg.GetInstanceID();
            var until = Time.time + seconds;
            if (_eggIgnoreUntil.TryGetValue(id, out var prev))
                until = Mathf.Max(until, prev);
            _eggIgnoreUntil[id] = until;
            if (_targetEgg == egg)
                ClearTargetState();
        }

        private bool IsEggIgnored(EggEntity egg)
        {
            if (egg == null)
                return true;
            return _eggIgnoreUntil.TryGetValue(egg.GetInstanceID(), out var until) && Time.time < until;
        }

        private static bool IsEggValid(EggEntity egg) => egg != null && egg.gameObject.activeInHierarchy;

        private float EstimatePathCost(List<Vector3> path) => BotPathPlanner.EstimateCost(transform.position, path);
    }
}
