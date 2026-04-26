using UnityEngine;

namespace CollectEggs.Gameplay.Navigation
{
    public class GridMap : MonoBehaviour
    {
        [SerializeField]
        private Vector3 origin = new(-10f, 0f, -10f);

        [SerializeField]
        private int width = 40;

        [SerializeField]
        private int height = 40;

        [SerializeField]
        private float cellSize = 0.5f;

        [SerializeField]
        private LayerMask groundLayer;

        [SerializeField]
        private LayerMask obstacleLayer;

        [SerializeField]
        private float probeHeight = 5f;

        [SerializeField]
        private float obstacleCheckHeight = 1f;

        [SerializeField]
        private float obstacleCheckRadiusScale = 0.4f;

        [SerializeField]
        private float walkabilityAgentInflation = 0.32f;

        [SerializeField]
        private float diagonalCost = 1.4142135f;

        [SerializeField]
        private float straightCost = 1f;

        [SerializeField]
        private bool drawGizmos = true;

        [SerializeField]
        private Color walkableColor = new(0.2f, 0.9f, 0.3f, 0.25f);

        [SerializeField]
        private Color blockedColor = new(0.95f, 0.2f, 0.2f, 0.35f);

        [SerializeField]
        private Color gridBorderColor = new(1f, 1f, 1f, 0.25f);

        [SerializeField]
        private float gizmoHeight = 0.05f;

        private bool[,] _walkable;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public float DiagonalCost => diagonalCost;
        public float StraightCost => straightCost;
        public LayerMask ObstacleLayer => obstacleLayer;

        public void ConfigureFromBounds(Vector3 min, Vector3 max, float preferredCellSize)
        {
            cellSize = Mathf.Max(0.1f, preferredCellSize);
            var minX = Mathf.Min(min.x, max.x);
            var maxX = Mathf.Max(min.x, max.x);
            var minZ = Mathf.Min(min.z, max.z);
            var maxZ = Mathf.Max(min.z, max.z);
            origin = new Vector3(minX, 0f, minZ);
            width = Mathf.Max(1, Mathf.CeilToInt((maxX - minX) / cellSize));
            height = Mathf.Max(1, Mathf.CeilToInt((maxZ - minZ) / cellSize));
        }

        public void ConfigureLayers(int groundLayerIndex, int obstacleLayerIndex)
        {
            if (groundLayerIndex >= 0)
                groundLayer = 1 << groundLayerIndex;
            if (obstacleLayerIndex >= 0)
                obstacleLayer = 1 << obstacleLayerIndex;
        }

        public void Rebuild()
        {
            _walkable = new bool[width, height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    _walkable[x, y] = EvaluateWalkable(x, y);
            }
        }

        private bool IsInside(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public bool IsWalkable(int x, int y) => IsInside(x, y) && _walkable[x, y];

        public Vector3 CellToWorld(int x, int y) => origin + new Vector3((x + 0.5f) * cellSize, 0f, (y + 0.5f) * cellSize);

        public bool GetNearestWalkable(Vector3 world, int maxRadius, out GridCoord coord)
        {
            var local = world - origin;
            var startX = Mathf.FloorToInt(local.x / cellSize);
            var startY = Mathf.FloorToInt(local.z / cellSize);
            startX = Mathf.Clamp(startX, 0, width - 1);
            startY = Mathf.Clamp(startY, 0, height - 1);
            var start = new GridCoord(startX, startY);

            if (IsWalkable(start.X, start.Y))
            {
                coord = start;
                return true;
            }

            for (var radius = 1; radius <= maxRadius; radius++)
            {
                var minX = start.X - radius;
                var maxX = start.X + radius;
                var minY = start.Y - radius;
                var maxY = start.Y + radius;
                for (var y = minY; y <= maxY; y++)
                {
                    if (PickWalkable(minX, y, out coord) || PickWalkable(maxX, y, out coord))
                        return true;
                }

                for (var x = minX + 1; x < maxX; x++)
                {
                    if (PickWalkable(x, minY, out coord) || PickWalkable(x, maxY, out coord))
                        return true;
                }
            }

            coord = default;
            return false;
        }

        private bool PickWalkable(int x, int y, out GridCoord coord)
        {
            if (IsWalkable(x, y))
            {
                coord = new GridCoord(x, y);
                return true;
            }

            coord = default;
            return false;
        }

        public bool WorldToCell(Vector3 world, out GridCoord coord)
        {
            var local = world - origin;
            var x = Mathf.FloorToInt(local.x / cellSize);
            var y = Mathf.FloorToInt(local.z / cellSize);
            if (!IsInside(x, y))
            {
                coord = default;
                return false;
            }

            coord = new GridCoord(x, y);
            return true;
        }

        public bool HasPhysicsClearance(Vector3 worldXZ, float extraHorizontalRadius)
        {
            var checkCenter = new Vector3(worldXZ.x, worldXZ.y + obstacleCheckHeight, worldXZ.z);
            var checkRadius = Mathf.Max(0.05f, cellSize * obstacleCheckRadiusScale + Mathf.Max(0f, extraHorizontalRadius));
            return !Physics.CheckSphere(checkCenter, checkRadius, obstacleLayer, QueryTriggerInteraction.Ignore);
        }

        private bool EvaluateWalkable(int x, int y)
        {
            var world = CellToWorld(x, y);
            var top = world + Vector3.up * probeHeight;
            if (!Physics.Raycast(top, Vector3.down, probeHeight * 2f, groundLayer, QueryTriggerInteraction.Ignore))
                return false;
            var checkCenter = world + Vector3.up * obstacleCheckHeight;
            var checkRadius = Mathf.Max(0.05f, cellSize * obstacleCheckRadiusScale + Mathf.Max(0f, walkabilityAgentInflation));
            var blocked = Physics.CheckSphere(checkCenter, checkRadius, obstacleLayer, QueryTriggerInteraction.Ignore);
            return !blocked;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || width <= 0 || height <= 0 || cellSize <= 0f)
                return;

            var half = Mathf.Max(0.02f, cellSize * 0.5f - 0.02f);
            var cubeSize = new Vector3(half * 2f, 0.02f, half * 2f);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var center = CellToWorld(x, y) + Vector3.up * gizmoHeight;
                    var walkable = Application.isPlaying && _walkable != null && IsInside(x, y) ? _walkable[x, y] : EvaluateWalkable(x, y);
                    Gizmos.color = walkable ? walkableColor : blockedColor;
                    Gizmos.DrawCube(center, cubeSize);
                }
            }

            Gizmos.color = gridBorderColor;
            var size = new Vector3(width * cellSize, 0.02f, height * cellSize);
            var boxCenter = origin + new Vector3(size.x * 0.5f, gizmoHeight, size.z * 0.5f);
            Gizmos.DrawWireCube(boxCenter, size);
        }
    }
}
