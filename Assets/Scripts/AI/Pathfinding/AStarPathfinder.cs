using System.Collections.Generic;
using UnityEngine;

namespace CollectEggs.AI.Pathfinding
{
    public static class AStarPathfinder
    {
        private sealed class Node
        {
            public int X;
            public int Y;
            public float G;
            public float H;
            public float F => G + H;
            public Node Parent;
        }

        private static readonly (int x, int y, bool diagonal)[] Directions =
        {
            (1, 0, false), (-1, 0, false), (0, 1, false), (0, -1, false),
            (1, 1, true), (1, -1, true), (-1, 1, true), (-1, -1, true)
        };

        public static bool FindPath(GridMap map, Vector3 startWorld, Vector3 endWorld, List<Vector3> result, int nearestWalkableRadius = 5)
        {
            result.Clear();
            if (map == null)
                return false;
            if (!map.GetNearestWalkable(startWorld, nearestWalkableRadius, out var start))
                return false;
            if (!map.GetNearestWalkable(endWorld, nearestWalkableRadius, out var end))
                return false;
            if (start.X == end.X && start.Y == end.Y)
            {
                result.Add(map.CellToWorld(end.X, end.Y));
                return true;
            }

            var open = new List<Node>(128);
            var closed = new bool[map.Width, map.Height];
            var nodes = new Node[map.Width, map.Height];

            var startNode = new Node { X = start.X, Y = start.Y, G = 0f, H = Heuristic(start.X, start.Y, end.X, end.Y, map) };
            open.Add(startNode);
            nodes[start.X, start.Y] = startNode;

            while (open.Count > 0)
            {
                var currentIndex = 0;
                for (var i = 1; i < open.Count; i++)
                {
                    if (open[i].F < open[currentIndex].F)
                        currentIndex = i;
                }

                var current = open[currentIndex];
                open.RemoveAt(currentIndex);
                if (closed[current.X, current.Y])
                    continue;
                closed[current.X, current.Y] = true;

                if (current.X == end.X && current.Y == end.Y)
                {
                    BuildPath(current, map, result);
                    return result.Count > 0;
                }

                foreach (var direction in Directions)
                {
                    var nx = current.X + direction.x;
                    var ny = current.Y + direction.y;
                    if (!map.IsWalkable(nx, ny))
                        continue;
                    if (closed[nx, ny])
                        continue;
                    if (direction.diagonal)
                    {
                        var sideA = map.IsWalkable(current.X + direction.x, current.Y);
                        var sideB = map.IsWalkable(current.X, current.Y + direction.y);
                        if (!sideA || !sideB)
                            continue;
                    }

                    var step = direction.diagonal ? map.DiagonalCost : map.StraightCost;
                    var candidateG = current.G + step;
                    var node = nodes[nx, ny];
                    if (node == null)
                    {
                        node = new Node
                        {
                            X = nx,
                            Y = ny,
                            G = candidateG,
                            H = Heuristic(nx, ny, end.X, end.Y, map),
                            Parent = current
                        };
                        nodes[nx, ny] = node;
                        open.Add(node);
                        continue;
                    }

                    if (candidateG >= node.G)
                        continue;
                    node.G = candidateG;
                    node.Parent = current;
                    if (!open.Contains(node))
                        open.Add(node);
                }
            }

            return false;
        }

        private static float Heuristic(int x, int y, int tx, int ty, GridMap map)
        {
            var dx = Mathf.Abs(tx - x);
            var dy = Mathf.Abs(ty - y);
            var diagonal = Mathf.Min(dx, dy);
            var straight = Mathf.Abs(dx - dy);
            return diagonal * map.DiagonalCost + straight * map.StraightCost;
        }

        private static void BuildPath(Node endNode, GridMap map, List<Vector3> result)
        {
            var cursor = endNode;
            while (cursor != null)
            {
                result.Add(map.CellToWorld(cursor.X, cursor.Y));
                cursor = cursor.Parent;
            }

            result.Reverse();
            SmoothPath(result);
        }

        private static void SmoothPath(List<Vector3> path)
        {
            if (path.Count < 3)
                return;
            var write = 1;
            var prevDir = FlatDirection(path[1] - path[0]);
            for (var i = 2; i < path.Count; i++)
            {
                var dir = FlatDirection(path[i] - path[i - 1]);
                if (Vector3.Dot(prevDir, dir) < 0.999f)
                {
                    path[write] = path[i - 1];
                    write++;
                }

                prevDir = dir;
            }

            path[write] = path[^1];
            write++;
            if (write < path.Count)
                path.RemoveRange(write, path.Count - write);
        }

        private static Vector3 FlatDirection(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude <= Mathf.Epsilon ? Vector3.zero : value.normalized;
        }
    }
}
