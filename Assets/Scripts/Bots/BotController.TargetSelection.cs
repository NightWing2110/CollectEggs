using CollectEggs.Gameplay.Eggs;
using UnityEngine;

namespace CollectEggs.Bots
{
    public partial class BotController
    {
        private const int CoastRetargetSkipLimit = 4;
        private const float PathEndpointEpsilonSqr = 0.0025f;

        private readonly struct TargetSelectionPick
        {
            public readonly EggEntity Egg;
            public readonly ApproachPathResult Plan;

            public TargetSelectionPick(EggEntity egg, ApproachPathResult plan)
            {
                Egg = egg;
                Plan = plan;
            }
        }

        private void EvaluateTarget()
        {
            if (HandleNoEggsInMatch())
                return;

            if (!IsEggValid(CurrentTargetEgg))
                ClearTargetState();

            if (ShouldSkipRetarget())
                return;

            CollectCandidateEggs();
            if (_eggCandidates.Count == 0)
                return;

            if (!FindBestTargetAmongCandidates(out var pick))
                return;

            ApplyTargetSelectionPick(in pick);
        }

        private bool HandleNoEggsInMatch()
        {
            if (EggEntity.Active.Count != 0)
                return false;
            ClearTargetState();
            return true;
        }

        private bool ShouldSkipRetarget()
        {
            var driftSq = eggPositionRepathThreshold * eggPositionRepathThreshold;
            if (_state != BotState.Chasing || !IsEggValid(CurrentTargetEgg) || _path.Count == 0 || _pathIndex >= _path.Count)
            {
                _coastRetargetSkips = 0;
                return false;
            }

            var ew = CurrentTargetEgg.transform.position;
            var dx = ew.x - _pathEggAnchor.x;
            var dz = ew.z - _pathEggAnchor.z;
            if (dx * dx + dz * dz > driftSq)
            {
                _coastRetargetSkips = 0;
                return false;
            }

            _coastRetargetSkips++;
            if (_coastRetargetSkips < CoastRetargetSkipLimit)
                return true;
            _coastRetargetSkips = 0;
            return false;
        }

        private void CollectCandidateEggs()
        {
            _eggCandidates.Clear();
            var eggs = EggEntity.Active;
            var p = transform.position;
            foreach (var egg in eggs)
            {
                if (!IsEggValid(egg) || IsEggIgnored(egg))
                    continue;
                var e = egg.transform.position;
                var edx = e.x - p.x;
                var edz = e.z - p.z;
                _eggCandidates.Add(new EggTargetCandidate(egg, edx * edx + edz * edz));
            }

            _eggCandidates.Sort();
        }

        private bool FindBestTargetAmongCandidates(out TargetSelectionPick pick)
        {
            pick = default;
            var pickCount = Mathf.Min(nearestEggsToPathfind, _eggCandidates.Count);
            EggEntity bestEgg = null;
            var bestCost = float.MaxValue;
            ApproachPathResult bestPlan = default;
            _evalBestPathCache.Clear();

            for (var i = 0; i < pickCount; i++)
            {
                var egg = _eggCandidates[i].Egg;
                if (!PlanPathToEgg(egg, _bestEggPathScratch, out var plan) || !plan.Success)
                    continue;
                if (!(plan.PathCost < bestCost))
                    continue;
                bestCost = plan.PathCost;
                bestEgg = egg;
                bestPlan = plan;
                _evalBestPathCache.Clear();
                _evalBestPathCache.AddRange(_bestEggPathScratch);
            }

            if (bestEgg == null)
                return false;
            pick = new TargetSelectionPick(bestEgg, bestPlan);
            return true;
        }

        private void ApplyTargetSelectionPick(in TargetSelectionPick pick)
        {
            if (CurrentTargetEgg == null)
            {
                SetTarget(pick.Egg, _evalBestPathCache, pick.Plan.ApproachWorld);
                return;
            }

            if (CurrentTargetEgg == pick.Egg)
            {
                if (KeepCurrentPathUnchanged(in pick))
                    return;
                SetPathAndApproach(_evalBestPathCache, pick.Plan.ApproachWorld);
                return;
            }

            if (SwitchToCandidateTarget(pick.Plan.PathCost))
                SetTarget(pick.Egg, _evalBestPathCache, pick.Plan.ApproachWorld);
        }

        private bool KeepCurrentPathUnchanged(in TargetSelectionPick pick)
        {
            if (_path.Count == 0 || _evalBestPathCache.Count == 0 || _pathIndex >= _path.Count)
                return false;
            var la = _path[^1] - _evalBestPathCache[^1];
            la.y = 0f;
            var ap = _approachGoalWorld - pick.Plan.ApproachWorld;
            ap.y = 0f;
            return la.sqrMagnitude < PathEndpointEpsilonSqr && ap.sqrMagnitude < PathEndpointEpsilonSqr;
        }

        private bool SwitchToCandidateTarget(float candidatePathCost)
        {
            var currentCost = EstimatePathCost(_path);
            var switchThreshold = currentCost * repathTargetSwitchAdvantage;
            return candidatePathCost <= switchThreshold;
        }
    }
}
