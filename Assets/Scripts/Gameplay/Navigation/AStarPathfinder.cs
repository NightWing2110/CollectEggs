using System.Collections.Generic;
using System.Text;
using CollectEggs.Gameplay;
using UnityEngine;

namespace CollectEggs.Gameplay.Navigation
{
    public static class AStarPathfinder
    {
        private static readonly (int x, int y, bool diagonal)[] Directions =
        {
            (1, 0, false), (-1, 0, false), (0, 1, false), (0, -1, false),
            (1, 1, true), (1, -1, true), (-1, 1, true), (-1, -1, true)
        };

        private static int _sSearchId;
        private static int _sMapCells;
        private static int[] _sTagG;
        private static int[] _sTagClosed;
        private static float[] _sG;
        private static int[] _sParentIdx;
        private static int[] _sHeap;
        private static float[] _sHeapKey;
        private static int _sHeapCount;

        private static readonly List<GridCoord> SChain = new(128);
        private static readonly List<Vector3> SFindUpBuffer = new(128);
        private static readonly List<GridCoord> SFindUpFirstCells = new(128);
        private static readonly List<GridCoord> SFindUpAltCells = new(128);

        public static bool FindPath(GridMap map, Vector3 startWorld, Vector3 endWorld, List<Vector3> result, int nearestWalkableRadius = 5, HashSet<GridCoord> blockedCells = null, List<GridCoord> cellPathOut = null, bool exactGoalCell = false)
        {
            result.Clear();
            cellPathOut?.Clear();
            if (map == null)
                return false;
            if (!map.GetNearestWalkable(startWorld, nearestWalkableRadius, out var start))
                return false;
            GridCoord end;
            if (exactGoalCell)
            {
                if (!map.WorldToCell(endWorld, out end))
                    return false;
                if (!map.IsWalkable(end.X, end.Y))
                    return false;
            }
            else if (!map.GetNearestWalkable(endWorld, nearestWalkableRadius, out end))
                return false;

            if (blockedCells != null && blockedCells.Contains(start))
                return false;
            if (blockedCells != null && blockedCells.Contains(end))
                return false;
            if (start.X == end.X && start.Y == end.Y)
            {
                result.Add(map.CellToWorld(end.X, end.Y));
                cellPathOut?.Add(end);
                return true;
            }

            var w = map.Width;
            EnsureBuffers(w * map.Height);
            _sSearchId++;
            if (_sSearchId >= int.MaxValue - 8)
            {
                System.Array.Clear(_sTagG, 0, _sMapCells);
                System.Array.Clear(_sTagClosed, 0, _sMapCells);
                _sSearchId = 1;
            }

            var sid = _sSearchId;
            var endX = end.X;
            var endY = end.Y;

            _sHeapCount = 0;
            var startIdx = start.X + start.Y * w;
            var endIdx = endX + endY * w;
            _sG[startIdx] = 0f;
            _sTagG[startIdx] = sid;
            _sParentIdx[startIdx] = -1;
            HeapPush(startIdx, Heuristic(start.X, start.Y, endX, endY, map));

            while (_sHeapCount > 0)
            {
                var currentIdx = HeapPop(out var keyF);
                if (_sTagClosed[currentIdx] == sid)
                    continue;
                if (_sTagG[currentIdx] != sid)
                    continue;
                var cx = currentIdx % w;
                var cy = currentIdx / w;
                var gCur = _sG[currentIdx];
                var hCur = Heuristic(cx, cy, endX, endY, map);
                if (keyF > gCur + hCur + 0.001f)
                    continue;
                _sTagClosed[currentIdx] = sid;

                if (currentIdx == endIdx)
                {
                    BuildPathFromParents(currentIdx, w, map, result, cellPathOut);
                    return result.Count > 0;
                }

                foreach (var direction in Directions)
                {
                    var nx = cx + direction.x;
                    var ny = cy + direction.y;
                    if (!map.IsWalkable(nx, ny))
                        continue;
                    var nIdx = nx + ny * w;
                    if (blockedCells != null && blockedCells.Contains(new GridCoord(nx, ny)))
                        continue;
                    if (_sTagClosed[nIdx] == sid)
                        continue;
                    if (direction.diagonal)
                    {
                        if (!map.IsWalkable(cx + direction.x, cy) || !map.IsWalkable(cx, cy + direction.y))
                            continue;
                    }

                    var step = direction.diagonal ? map.DiagonalCost : map.StraightCost;
                    var candidateG = gCur + step;
                    if (_sTagG[nIdx] == sid)
                    {
                        if (candidateG >= _sG[nIdx] - 1e-6f)
                            continue;
                    }

                    _sG[nIdx] = candidateG;
                    _sTagG[nIdx] = sid;
                    _sParentIdx[nIdx] = currentIdx;
                    var hf = Heuristic(nx, ny, endX, endY, map);
                    HeapPush(nIdx, candidateG + hf);
                }
            }

            return false;
        }

        private static void EnsureBuffers(int cells)
        {
            if (cells <= _sMapCells && _sTagG != null)
                return;
            _sMapCells = cells;
            _sTagG = new int[cells];
            _sTagClosed = new int[cells];
            _sG = new float[cells];
            _sParentIdx = new int[cells];
            _sHeap = new int[cells + 8];
            _sHeapKey = new float[cells + 8];
        }

        private static void HeapPush(int idx, float key)
        {
            var pos = _sHeapCount++;
            _sHeap[pos] = idx;
            _sHeapKey[pos] = key;
            HeapSiftUp(pos);
        }

        private static int HeapPop(out float key)
        {
            var root = _sHeap[0];
            key = _sHeapKey[0];
            _sHeapCount--;
            if (_sHeapCount <= 0) return root;
            _sHeap[0] = _sHeap[_sHeapCount];
            _sHeapKey[0] = _sHeapKey[_sHeapCount];
            HeapSiftDown(0);

            return root;
        }

        private static void HeapSiftUp(int pos)
        {
            while (pos > 0)
            {
                var parent = (pos - 1) >> 1;
                if (HeapBetter(parent, pos))
                    break;
                HeapSwap(parent, pos);
                pos = parent;
            }
        }

        private static void HeapSiftDown(int pos)
        {
            for (;;)
            {
                var left = (pos << 1) + 1;
                if (left >= _sHeapCount)
                    break;
                var right = left + 1;
                var best = left;
                if (right < _sHeapCount && HeapBetter(right, left))
                    best = right;
                if (HeapBetter(pos, best))
                    break;
                HeapSwap(pos, best);
                pos = best;
            }
        }

        private static bool HeapBetter(int a, int b)
        {
            var ka = _sHeapKey[a];
            var kb = _sHeapKey[b];
            if (ka < kb - 1e-6f)
                return true;
            if (ka > kb + 1e-6f)
                return false;
            return _sHeap[a] < _sHeap[b];
        }

        private static void HeapSwap(int a, int b)
        {
            (_sHeap[a], _sHeap[b]) = (_sHeap[b], _sHeap[a]);
            (_sHeapKey[a], _sHeapKey[b]) = (_sHeapKey[b], _sHeapKey[a]);
        }

        private static float Heuristic(int x, int y, int tx, int ty, GridMap map)
        {
            var dx = Mathf.Abs(tx - x);
            var dy = Mathf.Abs(ty - y);
            var diagonal = Mathf.Min(dx, dy);
            var straight = Mathf.Abs(dx - dy);
            return diagonal * map.DiagonalCost + straight * map.StraightCost;
        }

        private static void BuildPathFromParents(int endIdx, int w, GridMap map, List<Vector3> result, List<GridCoord> cellPathOut)
        {
            SChain.Clear();
            var cur = endIdx;
            while (cur >= 0)
            {
                var x = cur % w;
                var y = cur / w;
                SChain.Add(new GridCoord(x, y));
                cur = _sParentIdx[cur];
            }

            SChain.Reverse();
            cellPathOut?.AddRange(SChain);
            result.Clear();
            for (var i = 0; i < SChain.Count; i++)
                result.Add(map.CellToWorld(SChain[i].X, SChain[i].Y));
            SmoothPath(result);
        }

        public static int FindUpToKCandidatePaths(GridMap map, Vector3 startWorld, Vector3 endWorld, List<List<Vector3>> output, int maxPaths = 5, int nearestWalkableRadius = 5, bool exactGoalCell = false)
        {
            output.Clear();
            if (map == null || maxPaths <= 0)
                return 0;
            SFindUpBuffer.Clear();
            SFindUpFirstCells.Clear();
            if (!FindPath(map, startWorld, endWorld, SFindUpBuffer, nearestWalkableRadius, null, SFindUpFirstCells, exactGoalCell))
                return 0;
            output.Add(ClonePath(SFindUpBuffer));
            if (output.Count >= maxPaths || SFindUpFirstCells.Count < 3)
                return output.Count;
            var blocked = new HashSet<GridCoord>();
            var seen = new HashSet<string> { CellPathKey(SFindUpFirstCells) };
            var interior = SFindUpFirstCells.Count - 2;
            var step = interior <= 0 ? 1 : Mathf.Max(1, interior / 28);
            var probes = 0;
            for (var i = 1; i < SFindUpFirstCells.Count - 1 && output.Count < maxPaths && probes < 36; i += step, probes++)
            {
                blocked.Clear();
                blocked.Add(SFindUpFirstCells[i]);
                SFindUpAltCells.Clear();
                if (!FindPath(map, startWorld, endWorld, SFindUpBuffer, nearestWalkableRadius, blocked, SFindUpAltCells, exactGoalCell))
                    continue;
                var key = CellPathKey(SFindUpAltCells);
                if (!seen.Add(key))
                    continue;
                output.Add(ClonePath(SFindUpBuffer));
            }

            return output.Count;
        }

        private static List<Vector3> ClonePath(List<Vector3> source)
        {
            var copy = new List<Vector3>(source.Count);
            copy.AddRange(source);
            return copy;
        }

        private static string CellPathKey(List<GridCoord> cells)
        {
            if (cells == null || cells.Count == 0)
                return string.Empty;
            var sb = new StringBuilder(cells.Count * 12);
            for (var i = 0; i < cells.Count; i++)
            {
                if (i > 0)
                    sb.Append(';');
                sb.Append(cells[i].X).Append(',').Append(cells[i].Y);
            }

            return sb.ToString();
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
