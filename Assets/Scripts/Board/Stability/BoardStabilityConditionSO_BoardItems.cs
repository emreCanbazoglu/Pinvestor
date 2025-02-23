using System.Linq;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    [CreateAssetMenu(menuName = "MMGame/Game/Stability/Logic/Game Board Items Stability Condition SO")]
    public class BoardStabilityConditionSO_BoardItems : BoardStabilityConditionSOBase
    {
        public override bool IsSatisfied(bool isDebugEnabled = false)
        {
            return BoardManager
                .Instance
                .Board
                .BoardItems
                .All(val => val.CheckIsStable(isDebugEnabled));
        }
    }
}