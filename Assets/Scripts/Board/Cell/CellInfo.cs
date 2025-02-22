using System;
using UnityEngine;

namespace MildMania.MMGame.Game
{
    [Serializable]
    public class CellInfo
    {
        [field: SerializeField] public CellLayerInfoSO[] CellLayerInfoColl { get; private set; }
            = new CellLayerInfoSO[] { };
        
        [field: SerializeField] public GameObject CellVisualPrefab { get; private set; }
        
        [field: SerializeField] public Vector2 CellSize = new Vector2(1f, 1f); 
    }
}