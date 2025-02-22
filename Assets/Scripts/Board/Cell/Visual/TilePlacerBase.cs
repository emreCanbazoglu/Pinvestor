using System;
using System.Collections.Generic;
using UnityEngine;
using ENeighbor = MildMania.PuzzleLevelEditor.BoardExtensions.ENeighbor;

namespace Pinvestor.BoardSystem
{
    [Serializable]
    public abstract class TilePlacerBase<TVisual>
    {
        [Serializable]
        private class TileSetting
        {
            [SerializeField] private ETileVisualType _visualType;
            public ETileVisualType VisualType => _visualType;
            
            [SerializeField] private TVisual _visual;
            public TVisual Visual => _visual;
        }

        [Serializable]
        private class TilePatchSetting
        {
            [SerializeField] private ETileVisualPatchType _visualType;
            public ETileVisualPatchType VisualType => _visualType;
            
            [SerializeField] private TVisual _visual;
            public TVisual Visual => _visual;
        }

        [SerializeField] TileSetting[] _tileSettings = new TileSetting[0];

        [SerializeField] TilePatchSetting[] _patchSettings = new TilePatchSetting[0];

        public enum ETileVisualType
        {
            None = -1,
            Down = 0,
            Down_End = 1,
            Right = 2,
            Right_Down_Corner = 3,
            Right_Up_Corner = 4,
            Right_End = 5,
            Left = 6,
            Left_Down_Corner = 7,
            Left_Up_Corner = 8,
            Left_End = 9,
            Up = 10,
            Up_End = 11,
            Left_Right = 12,
            Up_Down = 13,
            Full_Cornered = 14,
            Middle = 15,
        }

        public enum ETileVisualPatchType
        {
            None = -1,
            Down_Left = 0,
            Up_Left = 1,
            Down_Right = 2,
            Up_Right = 3,
        }

        protected abstract void ToggleTileVisual(Vector2 tilePosition, TVisual visual, ETileVisualType visualType, bool isActive);
        protected abstract void ToggleTileVisualPatch(Vector2 tilePosition, TVisual visual, ETileVisualPatchType patchType, bool isActive);

        public bool TryGetTileSetting(ETileVisualType tileVisualType, out TVisual visual)
        {
            visual = default;
            
            foreach (TileSetting setting in _tileSettings)
            {
                if (setting.VisualType == tileVisualType)
                {
                    visual = setting.Visual;

                    return true;
                }
            }

            return false;
        }
        
        public void UpdateTile(Vector2 tilePosition, Func<Vector2, ENeighbor, bool> hasTileDel)
        {
            ETileVisualType tileVisualType = GetTileVisualType(tilePosition, hasTileDel);

            foreach (TileSetting setting in _tileSettings)
            {
                bool isActive = setting.VisualType == tileVisualType;

                ToggleTileVisual(tilePosition, setting.Visual, tileVisualType, isActive);
            }

            UpdateTilePatch(tilePosition, hasTileDel);
        }

        private void UpdateTilePatch(Vector2 tilePosition, Func<Vector2, ENeighbor, bool> hasTileDel)
        {
            List<ETileVisualPatchType> patches = GetTileVisualPatches(tilePosition, hasTileDel);

            foreach (TilePatchSetting setting in _patchSettings)
            {
                bool isActive = patches.Contains(setting.VisualType);
                
                ToggleTileVisualPatch(tilePosition, setting.Visual, setting.VisualType, isActive);
            }
        }

        private Dictionary<ETileVisualType, Func<bool, bool, bool, bool, bool>> _visualSettingMap =
            new Dictionary<ETileVisualType, Func<bool, bool, bool, bool, bool>>()
            {
                {ETileVisualType.Down, (up, down, left, right) => up && !down && left && right},
                {ETileVisualType.Left, (up, down, left, right) => up && !left && right && down},
                {ETileVisualType.Middle, (up, down, left, right) => up && left && right && down},
                {ETileVisualType.Right, (up, down, left, right) => up && left && !right && down},
                {ETileVisualType.Up, (up, down, left, right) => right && left && !up && down},
                {ETileVisualType.Down_End, (up, down, left, right) => up && !left && !right && !down},
                {ETileVisualType.Full_Cornered, (up, down, left, right) => !right && !left && !up && !down},
                {ETileVisualType.Left_End, (up, down, left, right) => !up && !left && right && !down},
                {ETileVisualType.Left_Right, (up, down, left, right) => !right && !left && up && down},
                {ETileVisualType.Right_End, (up, down, left, right) => !up && left && !right && !down},
                {ETileVisualType.Up_Down, (up, down, left, right) => right && left && !up && !down},
                {ETileVisualType.Up_End, (up, down, left, right) => !up && !left && !right && down},
                {ETileVisualType.Left_Down_Corner, (up, down, left, right) => up && !left && right && !down},
                {ETileVisualType.Left_Up_Corner, (up, down, left, right) => !up && !left && right && down},
                {ETileVisualType.Right_Down_Corner, (up, down, left, right) => up && left && !right && !down},
                {ETileVisualType.Right_Up_Corner, (up, down, left, right) => !up && left && !right && down},
            };
        
        private Dictionary<ETileVisualPatchType, Func<bool, bool, bool, bool, bool, bool, bool, bool, bool>> _patchSettingMap =
            new Dictionary<ETileVisualPatchType, Func<bool, bool, bool, bool, bool, bool, bool, bool, bool>>()
            {
                {
                    ETileVisualPatchType.Down_Left,
                    (up, down, left, right, upLeft, upRight, downLeft, downRight) => 
                        down && left && !downLeft
                },
                {
                    ETileVisualPatchType.Down_Right,
                    (up, down, left, right, upLeft, upRight, downLeft, downRight) => 
                        down && right && !downRight
                },
                {
                    ETileVisualPatchType.Up_Left,
                    (up, down, left, right, upLeft, upRight, downLeft, downRight) => 
                        up && left && !upLeft
                },
                {
                    ETileVisualPatchType.Up_Right,
                    (up, down, left, right, upLeft, upRight, downLeft, downRight) => 
                        up && right && !upRight
                },
            };

        private void GetNeighbors(
            Vector2 tilePosition,
            Func<Vector2, ENeighbor, bool> hasTileDel,
            out bool up,
            out bool down,
            out bool left,
            out bool right,
            out bool downLeft,
            out bool downRight,
            out bool upLeft,
            out bool upRight)
        {
            up = hasTileDel(tilePosition, ENeighbor.Up);
            down = hasTileDel(tilePosition, ENeighbor.Down);
            left = hasTileDel(tilePosition, ENeighbor.Left);
            right = hasTileDel(tilePosition, ENeighbor.Right);
            upRight = hasTileDel(tilePosition, ENeighbor.Up_Right);
            upLeft = hasTileDel(tilePosition, ENeighbor.Up_Left);
            downRight = hasTileDel(tilePosition, ENeighbor.Down_Right);
            downLeft = hasTileDel(tilePosition, ENeighbor.Down_Left);
        }
        
        private ETileVisualType GetTileVisualType(Vector2 tile, Func<Vector2, ENeighbor, bool> hasTileDel)
        {
            GetNeighbors(
                tile, 
                hasTileDel,
                out bool up,
                out bool down,
                out bool left,
                out bool right,
                out bool downLeft,
                out bool downRight,
                out bool upLeft,
                out bool upRight);

            foreach (KeyValuePair<ETileVisualType, Func<bool, bool, bool, bool, bool>> kvp in _visualSettingMap)
            {
                if (kvp.Value(up, down, left, right))
                {
                    return kvp.Key;
                }
            }
            
            return ETileVisualType.Middle;
        }

        private List<ETileVisualPatchType> GetTileVisualPatches(Vector2 tilePosition, Func<Vector2, ENeighbor, bool> hasTileDel)
        {
            GetNeighbors(
                tilePosition, 
                hasTileDel,
                out bool up,
                out bool down,
                out bool left,
                out bool right,
                out bool downLeft,
                out bool downRight,
                out bool upLeft,
                out bool upRight);
            
            List<ETileVisualPatchType> boardVisualPatchTypes = new List<ETileVisualPatchType>();

            foreach (KeyValuePair<ETileVisualPatchType, Func<bool, bool, bool, bool, bool, bool, bool, bool, bool>> kvp in _patchSettingMap)
            {
                if (kvp.Value(up, down, left, right, upLeft, upRight, downLeft, downRight))
                {
                    boardVisualPatchTypes.Add(kvp.Key);
                }
            }

            return boardVisualPatchTypes;
        }
    }
}
