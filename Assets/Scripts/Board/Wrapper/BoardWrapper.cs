using System.Collections.Generic;
using Pinvestor.BoardSystem.Base;
using UnityEngine;

namespace Pinvestor.BoardSystem.Authoring
{
    /// <summary>
    /// Authoring/visual side of the <see cref="Board"/>.
    ///
    /// The board is a physical surface lying on the wrapper's local XZ plane:
    /// board columns (Cell.Position.x) run along local +X, board rows
    /// (Cell.Position.y) run along local +Z, and local +Y points up out of the
    /// surface. Every layout calculation is done in board-local space and then
    /// converted to world space, so the whole board can be moved, rotated or
    /// tilted as a single transform without breaking cell lookups.
    /// </summary>
    public class BoardWrapper : MonoBehaviour
    {
        [field: SerializeField] public BoardHighlighter Highlighter { get; private set; } = null;

        [Header("Layout")]
        [Tooltip("Board-local offset of the board center. Y is the height of the playable surface.")]
        [SerializeField] private Vector3 _centerPosition = Vector3.zero;

        [SerializeField] private GameObject _cellPrefab = null;
        [SerializeField] private Transform _cellParent = null;

        [Tooltip("Cell footprint on the board plane: x = size along local X (columns), y = size along local Z (rows).")]
        [SerializeField] private Vector2 _cellSize = Vector2.one;

        [Tooltip("Offset applied to each cell's VisualParent on the board plane: x = local X, y = local Z.")]
        [SerializeField] private Vector2 _cellVisualOffset = Vector2.zero;

        [Header("Walls")]
        [SerializeField] private Transform _boardEnterTransform = null;

        [Tooltip("How far the bounding walls rise above the board surface.")]
        [SerializeField] private float _wallHeight = 1f;

        [Tooltip("Thickness of the bounding walls along the board plane.")]
        [SerializeField] private float _wallThickness = 0.5f;

        public Board Board { get; private set; }

        /// <summary>Playfield volume in board-local space. Rests on the surface and rises by <see cref="_wallHeight"/>.</summary>
        public Bounds LocalBounds { get; private set; }

        /// <summary>World-space axis-aligned bounds enclosing <see cref="LocalBounds"/>.</summary>
        public Bounds Bounds { get; private set; }

        /// <summary>World-space center of the board surface.</summary>
        public Vector3 SurfaceCenter => transform.TransformPoint(_centerPosition);

        /// <summary>World-space normal of the board surface.</summary>
        public Vector3 SurfaceNormal => transform.up;

        /// <summary>World-space plane the board surface lies on.</summary>
        public Plane SurfacePlane => new Plane(SurfaceNormal, SurfaceCenter);

        public Vector2 CellSize => _cellSize;

        private BoxCollider[] _colliders;

        /// <summary>Open edge on the near (-Z) side the ball is shot through.</summary>
        public BoxCollider BoardEnterCollider => _colliders[3];

        public Dictionary<Cell, CellWrapper> CellWrappers { get; private set; }
            = new Dictionary<Cell, CellWrapper>();

        public void WrapBoard(
            Board board)
        {
            Board = board;

            CalculateBounds();

            CreateCells();

            CreateBoardCollider();
        }

        private void CreateCells()
        {
            Debug.Log("Create Cells: " + Board.Cells.Count);

            CellWrappers.Clear();

            foreach (var kvp in Board.Cells)
            {
                Vector2Int coordinates = kvp.Key;
                Cell cell = kvp.Value;

                GameObject cellGO
                    = Instantiate(
                        _cellPrefab,
                        _cellParent);

                cellGO.name = $"Cell {coordinates.x} {coordinates.y}";

                CellWrapper cellWrapper = cellGO.GetComponent<CellWrapper>();

                // Cells lie flat on the board plane and inherit the board's
                // orientation, so a moved/rotated board keeps its cells aligned.
                cellGO.transform.SetPositionAndRotation(
                    GetCellWorldPosition(coordinates),
                    transform.rotation);

                Transform visualParent = cellWrapper.VisualParent;

                if (visualParent != null
                    && visualParent != cellGO.transform)
                {
                    visualParent.localPosition
                        = new Vector3(
                            _cellVisualOffset.x,
                            0f,
                            _cellVisualOffset.y);
                }

                cellWrapper.Init(cell);

                CellWrappers.Add(cell, cellWrapper);
            }
        }

        private void CalculateBounds()
        {
            Vector3 size
                = new Vector3(
                    Board.Dimensions.x * _cellSize.x,
                    _wallHeight,
                    Board.Dimensions.y * _cellSize.y);

            // The playfield volume rests on the surface and extends upwards.
            LocalBounds = new Bounds(
                _centerPosition + new Vector3(0f, _wallHeight * 0.5f, 0f),
                size);

            Bounds = CalculateWorldBounds(LocalBounds);
        }

        private Bounds CalculateWorldBounds(
            Bounds localBounds)
        {
            Vector3 extents = localBounds.extents;

            Bounds worldBounds
                = new Bounds(
                    transform.TransformPoint(localBounds.center),
                    Vector3.zero);

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner
                    = localBounds.center
                      + new Vector3(
                          (i & 1) == 0 ? -extents.x : extents.x,
                          (i & 2) == 0 ? -extents.y : extents.y,
                          (i & 4) == 0 ? -extents.z : extents.z);

                worldBounds.Encapsulate(
                    transform.TransformPoint(corner));
            }

            return worldBounds;
        }

        private void CreateBoardCollider()
        {
            // Bounding walls for left (-X), far (+Z), right (+X) and near (-Z).
            // The near wall is the ball entrance and lives on its own transform so
            // it can be treated as an opening instead of a reflector.
            _colliders = new BoxCollider[4];

            float centerY = LocalBounds.center.y;

            // Side walls are extended past the board depth so the corners are sealed.
            Vector3 sideWallSize
                = new Vector3(
                    _wallThickness,
                    _wallHeight,
                    LocalBounds.size.z + _wallThickness * 2f);

            Vector3 endWallSize
                = new Vector3(
                    LocalBounds.size.x + _wallThickness * 2f,
                    _wallHeight,
                    _wallThickness);

            // Left wall (-X)
            _colliders[0] = gameObject.AddComponent<BoxCollider>();
            _colliders[0].size = sideWallSize;
            _colliders[0].center = new Vector3(
                LocalBounds.min.x - _wallThickness * 0.5f,
                centerY,
                LocalBounds.center.z);
            _colliders[0].isTrigger = true;

            // Far wall (+Z)
            _colliders[1] = gameObject.AddComponent<BoxCollider>();
            _colliders[1].size = endWallSize;
            _colliders[1].center = new Vector3(
                LocalBounds.center.x,
                centerY,
                LocalBounds.max.z + _wallThickness * 0.5f);
            _colliders[1].isTrigger = true;

            // Right wall (+X)
            _colliders[2] = gameObject.AddComponent<BoxCollider>();
            _colliders[2].size = sideWallSize;
            _colliders[2].center = new Vector3(
                LocalBounds.max.x + _wallThickness * 0.5f,
                centerY,
                LocalBounds.center.z);
            _colliders[2].isTrigger = true;

            // Near wall (-Z) — the ball entrance.
            Vector3 enterCenterLocal
                = new Vector3(
                    LocalBounds.center.x,
                    centerY,
                    LocalBounds.min.z - _wallThickness * 0.5f);

            _colliders[3] = _boardEnterTransform.gameObject.AddComponent<BoxCollider>();
            _colliders[3].size = endWallSize;
            _colliders[3].center
                = _boardEnterTransform.InverseTransformPoint(
                    transform.TransformPoint(enterCenterLocal));
            _colliders[3].isTrigger = true;
        }

        public bool TryGetCellWrapper(
            Vector2Int coordinates,
            out CellWrapper cellWrapper)
        {
            cellWrapper = default;

            if (!Board.TryGetCellAt(coordinates, out Cell cell))
            {
                return false;
            }

            return CellWrappers.TryGetValue(cell, out cellWrapper);
        }

        public bool TryGetCellWrapper(
            Cell cell,
            out CellWrapper cellWrapper)
        {
            cellWrapper = default;

            return CellWrappers.TryGetValue(cell, out cellWrapper);
        }

        /// <summary>Board-local position of a cell center, on the board surface.</summary>
        public Vector3 GetCellLocalPosition(
            Vector2Int coordinates)
        {
            return GetCellLocalPosition(
                Board.Dimensions,
                coordinates);
        }

        /// <summary>World position of a cell center, on the board surface.</summary>
        public Vector3 GetCellWorldPosition(
            Vector2Int coordinates)
        {
            return transform.TransformPoint(
                GetCellLocalPosition(coordinates));
        }

        private Vector3 GetCellLocalPosition(
            Vector2Int dimensions,
            Vector2Int coordinates)
        {
            return GetNearLeftLocalCorner(dimensions)
                   + new Vector3(
                       coordinates.x * _cellSize.x + _cellSize.x / 2,
                       0f,
                       coordinates.y * _cellSize.y + _cellSize.y / 2);
        }

        /// <summary>Board-local corner of cell (0,0): minimum X, minimum Z, on the surface.</summary>
        private Vector3 GetNearLeftLocalCorner(
            Vector2Int dimensions)
        {
            return _centerPosition
                   - new Vector3(
                       dimensions.x * _cellSize.x / 2,
                       0f,
                       dimensions.y * _cellSize.y / 2);
        }

        /// <summary>
        /// Resolves the cell under a world position. The position is projected onto
        /// the board plane, so any height above or below the surface is ignored.
        /// </summary>
        public bool TryGetCellAt(
            Vector3 worldPosition,
            out Cell cell)
        {
            cell = null;

            if (Board == null)
                return false;

            return TryGetCellAtLocal(
                transform.InverseTransformPoint(worldPosition),
                out cell);
        }

        private bool TryGetCellAtLocal(
            Vector3 localPosition,
            out Cell cell)
        {
            cell = null;

            Vector3 nearLeftPosition = GetNearLeftLocalCorner(Board.Dimensions);

            float xDist = localPosition.x - nearLeftPosition.x;

            if (xDist < 0f)
                return false;

            int xIndex = Mathf.FloorToInt(xDist / _cellSize.x);

            if (xIndex < 0 || xIndex >= Board.Dimensions.x)
                return false;

            float zDist = localPosition.z - nearLeftPosition.z;

            if (zDist < 0f)
                return false;

            int zIndex = Mathf.FloorToInt(zDist / _cellSize.y);

            if (zIndex < 0 || zIndex >= Board.Dimensions.y)
                return false;

            return Board.TryGetCellAt(
                new Vector2Int(xIndex, zIndex),
                out cell);
        }

        /// <summary>
        /// Intersects a ray with the board surface plane. This is the 3D replacement
        /// for the old orthographic <c>Camera.ScreenToWorldPoint</c> usage — under a
        /// perspective camera a screen point only maps to a world point once it is
        /// projected onto a plane.
        /// </summary>
        public bool TryGetSurfacePoint(
            Ray ray,
            out Vector3 worldPosition)
        {
            worldPosition = default;

            if (!SurfacePlane.Raycast(ray, out float distance))
                return false;

            worldPosition = ray.GetPoint(distance);

            return true;
        }

        public bool TryGetSurfacePoint(
            Camera camera,
            Vector2 screenPosition,
            out Vector3 worldPosition)
        {
            worldPosition = default;

            if (camera == null)
                return false;

            return TryGetSurfacePoint(
                camera.ScreenPointToRay(screenPosition),
                out worldPosition);
        }

        public bool TryGetCellAtScreenPoint(
            Camera camera,
            Vector2 screenPosition,
            out Cell cell)
        {
            cell = null;

            if (!TryGetSurfacePoint(
                    camera,
                    screenPosition,
                    out Vector3 worldPosition))
                return false;

            return TryGetCellAt(worldPosition, out cell);
        }

        private void OnDrawGizmos()
        {
            if (Board == null)
                return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(
                LocalBounds.center,
                LocalBounds.size);

            Gizmos.color = Color.green;

            Vector3 nearLeftPosition = GetNearLeftLocalCorner(Board.Dimensions);

            for (int x = 0; x <= Board.Dimensions.x; x++)
            {
                Vector3 start
                    = nearLeftPosition
                      + new Vector3(x * _cellSize.x, 0f, 0f);

                Gizmos.DrawLine(
                    start,
                    start + new Vector3(0f, 0f, Board.Dimensions.y * _cellSize.y));
            }

            for (int y = 0; y <= Board.Dimensions.y; y++)
            {
                Vector3 start
                    = nearLeftPosition
                      + new Vector3(0f, 0f, y * _cellSize.y);

                Gizmos.DrawLine(
                    start,
                    start + new Vector3(Board.Dimensions.x * _cellSize.x, 0f, 0f));
            }

            Gizmos.matrix = previousMatrix;
        }
    }
}
