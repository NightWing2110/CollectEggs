using System;
using System.Collections.Generic;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Navigation;
using UnityEngine;

namespace CollectEggs.Bots
{
    internal readonly struct EggTargetCandidate : IComparable<EggTargetCandidate>
    {
        public readonly EggEntity Egg;
        private readonly float _distanceSquared;

        public EggTargetCandidate(EggEntity egg, float distanceSquared)
        {
            Egg = egg;
            _distanceSquared = distanceSquared;
        }

        public int CompareTo(EggTargetCandidate other) => _distanceSquared.CompareTo(other._distanceSquared);
    }

    internal readonly struct ApproachPathResult
    {
        public readonly bool Success;
        public readonly float PathCost;
        public readonly Vector3 ApproachWorld;

        private ApproachPathResult(bool success, float pathCost, Vector3 approachWorld)
        {
            Success = success;
            PathCost = pathCost;
            ApproachWorld = approachWorld;
        }

        public static ApproachPathResult Failed => default;

        public static ApproachPathResult Ok(float pathCost, Vector3 approachWorld) =>
            new(true, pathCost, approachWorld);
    }

    internal readonly struct EggApproachPlannerSettings
    {
        public readonly float BotRadius;
        public readonly float CollectReachRadius;
        public readonly float ApproachMargin;
        public readonly int EarlyExitPathCellCount;
        public readonly float EarlyExitPathCostMultiplier;

        public EggApproachPlannerSettings(
            float botRadius,
            float collectReachRadius,
            float approachMargin,
            int earlyExitPathCellCount,
            float earlyExitPathCostMultiplier)
        {
            BotRadius = botRadius;
            CollectReachRadius = collectReachRadius;
            ApproachMargin = approachMargin;
            EarlyExitPathCellCount = earlyExitPathCellCount;
            EarlyExitPathCostMultiplier = earlyExitPathCostMultiplier;
        }
    }

    internal static class EggApproachPathPlanner
    {
        private const int ApproachDirections = 8;
        private const float MinRingRadius = 0.12f;
        private const float RingDuplicateEpsilon = 0.02f;
        private const float ReachDistanceEpsilon = 0.02f;
        private const float StrictClearanceRadiusScale = 0.35f;
        private const float LinecastDistanceFactor = 2.25f;
        private const float LinecastProbeHeight = 0.45f;
        private const float LooseClearanceRadius = 0f;
        private const int MinEarlyExitCellCount = 3;
        private const int PathNearestWalkableRadius = 5;

        private struct PlannerContext
        {
            public GridMap Grid;
            public Vector3 EggPos;
            public Vector3 BotWorld;
            public EggApproachPlannerSettings Settings;
            public float CellSize;
            public float MaxReach;
            public int EarlyCells;
            public float EarlyCost;
            public List<Vector3> PathOut;
            public List<Vector3> AstarScratch;
            public float RunBestCost;
            public Vector3 RunBestApproach;
            public bool StopAll;
        }

        public static bool FindBestApproachPath(
            GridMap grid,
            Vector3 botWorld,
            EggApproachPlannerSettings settings,
            EggEntity egg,
            List<Vector3> pathOut,
            List<Vector3> astarScratch,
            out ApproachPathResult result)
        {
            result = ApproachPathResult.Failed;
            pathOut.Clear();
            if (egg == null || grid == null)
                return false;

            var ctx = new PlannerContext
            {
                Grid = grid,
                EggPos = egg.transform.position,
                BotWorld = botWorld,
                Settings = settings,
                CellSize = grid.CellSize,
                MaxReach = Mathf.Max(MinRingRadius, settings.CollectReachRadius - 0.03f),
                PathOut = pathOut,
                AstarScratch = astarScratch,
                EarlyCells = Mathf.Max(MinEarlyExitCellCount, settings.EarlyExitPathCellCount),
                RunBestCost = float.MaxValue
            };
            ctx.EarlyCost = ctx.CellSize * Mathf.Max(1f, settings.EarlyExitPathCostMultiplier);

            FindBestApproachPathToEgg(ref ctx);

            if (pathOut.Count == 0)
                return false;
            result = ApproachPathResult.Ok(ctx.RunBestCost, ctx.RunBestApproach);
            return true;
        }

        private static (float primary, float secondary, float tertiary) BuildApproachRings(
            EggApproachPlannerSettings settings,
            float maxReach)
        {
            var specR = settings.CollectReachRadius + settings.BotRadius + settings.ApproachMargin;
            var ringPrimary = Mathf.Min(specR, maxReach);
            var ringSecondary = Mathf.Max(MinRingRadius, Mathf.Min(ringPrimary * 0.82f, maxReach));
            var ringTertiary = Mathf.Max(MinRingRadius, Mathf.Min(settings.CollectReachRadius * 0.52f, maxReach));
            return (ringPrimary, ringSecondary, ringTertiary);
        }

        private static void FindBestApproachPathToEgg(ref PlannerContext ctx)
        {
            var rings = BuildApproachRings(ctx.Settings, ctx.MaxReach);
            EnumerateApproachSamples(ref ctx, rings.primary, true);
            if (ctx.PathOut.Count == 0)
                EnumerateApproachSamples(ref ctx, rings.primary, false);

            if (ctx.PathOut.Count == 0 && !ctx.StopAll && Mathf.Abs(rings.secondary - rings.primary) > RingDuplicateEpsilon)
            {
                EnumerateApproachSamples(ref ctx, rings.secondary, true);
                if (ctx.PathOut.Count == 0)
                    EnumerateApproachSamples(ref ctx, rings.secondary, false);
            }

            if (ctx.PathOut.Count != 0 || ctx.StopAll ||
                !(Mathf.Abs(rings.tertiary - rings.primary) > RingDuplicateEpsilon) ||
                !(Mathf.Abs(rings.tertiary - rings.secondary) > RingDuplicateEpsilon)) return;
            EnumerateApproachSamples(ref ctx, rings.tertiary, true);
            if (ctx.PathOut.Count == 0)
                EnumerateApproachSamples(ref ctx, rings.tertiary, false);
        }

        private static void EnumerateApproachSamples(
            ref PlannerContext ctx,
            float ringR,
            bool strictClearance)
        {
            if (ctx.StopAll || ringR > ctx.MaxReach + RingDuplicateEpsilon)
                return;
            for (var i = 0; i < ApproachDirections; i++)
            {
                if (ctx.StopAll)
                    return;
                if (!SampleApproachDirection(ref ctx, ringR, strictClearance, i, out var cellWorld, out var pathCost))
                    continue;

                if (!(pathCost < ctx.RunBestCost))
                    continue;
                ctx.RunBestCost = pathCost;
                ctx.RunBestApproach = cellWorld;
                ctx.PathOut.Clear();
                ctx.PathOut.AddRange(ctx.AstarScratch);
                if (EarlyExitAfterImprovement(ctx.PathOut.Count, ctx.RunBestCost, ctx.EarlyCells, ctx.EarlyCost))
                    ctx.StopAll = true;
            }
        }

        private static bool EarlyExitAfterImprovement(int pathCount, float runBestCost, int earlyCells, float earlyCost) => pathCount > 0 && (pathCount <= earlyCells || runBestCost < earlyCost);

        private static bool SampleApproachDirection(
            ref PlannerContext ctx,
            float ringR,
            bool strictClearance,
            int directionIndex,
            out Vector3 approachCellWorld,
            out float pathCost)
        {
            approachCellWorld = default;
            pathCost = float.MaxValue;
            var ang = (Mathf.PI * 2f * directionIndex) / ApproachDirections;
            var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            var sample = ctx.EggPos + dir * ringR;
            sample.y = ctx.EggPos.y;
            if (!ResolveWalkableApproachCellWorld(ctx.Grid, ctx.EggPos, sample, ctx.Settings.CollectReachRadius, out approachCellWorld))
                return false;
            if (!ValidateApproachPoint(ctx.Grid, approachCellWorld, ctx.EggPos, ctx.Settings.BotRadius, ctx.CellSize, strictClearance))
                return false;
            return BuildPathToApproach(ctx.Grid, ctx.BotWorld, approachCellWorld, ctx.AstarScratch, out pathCost);
        }

        private static bool ResolveWalkableApproachCellWorld(
            GridMap grid,
            Vector3 eggPos,
            Vector3 sampleWorld,
            float collectReachRadius,
            out Vector3 cellWorld)
        {
            cellWorld = default;
            if (!grid.WorldToCell(sampleWorld, out var cell))
                return false;
            if (!grid.IsWalkable(cell.X, cell.Y))
                return false;
            cellWorld = grid.CellToWorld(cell.X, cell.Y);
            cellWorld.y = sampleWorld.y;
            return FlatDistanceXZ(cellWorld, eggPos) <= collectReachRadius + ReachDistanceEpsilon;
        }

        private static bool ValidateApproachPoint(
            GridMap grid,
            Vector3 cellWorld,
            Vector3 eggPos,
            float botRadius,
            float cellSz,
            bool strictClearance)
        {
            return strictClearance
                ? ValidateStrictApproachPoint(grid, cellWorld, eggPos, botRadius, cellSz)
                : ValidateLooseApproachPoint(grid, cellWorld);
        }

        private static bool ValidateStrictApproachPoint(
            GridMap grid,
            Vector3 cellWorld,
            Vector3 eggPos,
            float botRadius,
            float cellSz)
        {
            if (!grid.HasPhysicsClearance(cellWorld, botRadius * StrictClearanceRadiusScale))
                return false;
            var flatToEgg = FlatDistanceXZ(cellWorld, eggPos);
            if (flatToEgg <= cellSz * LinecastDistanceFactor)
                return true;
            var lineA = cellWorld + Vector3.up * LinecastProbeHeight;
            var lineB = eggPos + Vector3.up * LinecastProbeHeight;
            return !Physics.Linecast(lineA, lineB, grid.ObstacleLayer, QueryTriggerInteraction.Ignore);
        }

        private static bool ValidateLooseApproachPoint(GridMap grid, Vector3 cellWorld) => grid.HasPhysicsClearance(cellWorld, LooseClearanceRadius);

        private static bool BuildPathToApproach(
            GridMap grid,
            Vector3 botWorld,
            Vector3 approachCellWorld,
            List<Vector3> astarScratch,
            out float pathCost)
        {
            pathCost = float.MaxValue;
            if (!AStarPathfinder.FindPath(grid, botWorld, approachCellWorld, astarScratch, PathNearestWalkableRadius, null, null, true))
                return false;
            pathCost = EstimatePathCostFrom(botWorld, astarScratch);
            return true;
        }

        public static float EstimatePathCostFrom(Vector3 origin, List<Vector3> path)
        {
            if (path == null || path.Count == 0)
                return float.MaxValue;
            var total = 0f;
            var prev = origin;
            foreach (var t in path)
            {
                var point = t;
                point.y = prev.y;
                total += Vector3.Distance(prev, point);
                prev = point;
            }

            return total;
        }

        private static float FlatDistanceXZ(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
