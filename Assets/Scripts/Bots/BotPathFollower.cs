using System.Collections.Generic;
using CollectEggs.Gameplay.Movement;
using UnityEngine;

namespace CollectEggs.Bots
{
    internal static class BotPathFollower
    {
        public static void Follow(
            Transform transform,
            ActorMovement movement,
            List<Vector3> path,
            ref int pathIndex,
            float waypointReachDistance)
        {
            if (movement == null || path == null || path.Count == 0 || pathIndex >= path.Count)
            {
                movement?.Move(Vector2.zero);
                return;
            }

            var current = transform.position;
            var target = path[pathIndex];
            var toTarget = target - current;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= waypointReachDistance * waypointReachDistance)
            {
                pathIndex++;
                if (pathIndex >= path.Count)
                {
                    movement.Move(Vector2.zero);
                    return;
                }

                target = path[pathIndex];
                toTarget = target - current;
                toTarget.y = 0f;
            }

            var dir = toTarget.sqrMagnitude > Mathf.Epsilon ? toTarget.normalized : Vector3.zero;
            movement.Move(new Vector2(dir.x, dir.z));
        }
    }
}
