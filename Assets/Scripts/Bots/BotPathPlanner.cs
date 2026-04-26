using System.Collections.Generic;
using CollectEggs.Gameplay.Eggs;
using CollectEggs.Gameplay.Navigation;
using UnityEngine;

namespace CollectEggs.Bots
{
    internal readonly struct BotPathPlanRequest
    {
        public readonly GridMap GridMap;
        public readonly Vector3 BotPosition;
        public readonly CharacterController Controller;
        public readonly float CollectReach;
        public readonly float ApproachMargin;
        public readonly int EarlyExitPathCellCount;
        public readonly float EarlyExitPathCostMultiplier;
        public readonly EggEntity Egg;
        public readonly List<Vector3> PathOut;
        public readonly List<Vector3> AstarBuffer;

        public BotPathPlanRequest(
            GridMap gridMap,
            Vector3 botPosition,
            CharacterController controller,
            float collectReach,
            float approachMargin,
            int earlyExitPathCellCount,
            float earlyExitPathCostMultiplier,
            EggEntity egg,
            List<Vector3> pathOut,
            List<Vector3> astarBuffer)
        {
            GridMap = gridMap;
            BotPosition = botPosition;
            Controller = controller;
            CollectReach = collectReach;
            ApproachMargin = approachMargin;
            EarlyExitPathCellCount = earlyExitPathCellCount;
            EarlyExitPathCostMultiplier = earlyExitPathCostMultiplier;
            Egg = egg;
            PathOut = pathOut;
            AstarBuffer = astarBuffer;
        }
    }

    internal static class BotPathPlanner
    {
        public static bool PlanPathToEgg(BotPathPlanRequest request, out ApproachPathResult plan)
        {
            var botRadius = request.Controller != null ? request.Controller.radius : 0.5f;
            var settings = new EggApproachPlannerSettings(
                botRadius,
                request.CollectReach,
                request.ApproachMargin,
                request.EarlyExitPathCellCount,
                request.EarlyExitPathCostMultiplier);
            return EggApproachPathPlanner.FindBestApproachPath(
                request.GridMap,
                request.BotPosition,
                settings,
                request.Egg,
                request.PathOut,
                request.AstarBuffer,
                out plan);
        }

        public static float EstimateCost(Vector3 botPosition, List<Vector3> path) => EggApproachPathPlanner.EstimatePathCostFrom(botPosition, path);
    }
}
