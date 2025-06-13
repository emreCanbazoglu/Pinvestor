using Pinvestor.BoardSystem.Base;
using Pinvestor.CardSystem;
using UnityEngine;

namespace Pinvestor.BoardSystem
{
    [CreateAssetMenu(
        fileName = "BoardItemProperty.CardOwner.Default.asset",
        menuName = "Pinvestor/Game/Board Item/Property/Card Owner/Default")]
    public class BoardItemProperty_CardOwner : BoardItemPropertySOBase
    {
        public override BoardItemPropertySpecBase CreateSpec(
            BoardItemBase owner)
        {
            return new BoardItemPropertySpec_CardOwner(
                this,
                owner);
        }
    }
    
    public class BoardItemPropertySpec_CardOwner : BoardItemPropertySpecBase
    {
        public BoardItemProperty_CardOwner CastedSO { get; private set; }
        
        public CardBase Card { get; private set; }

        public BoardItemPropertySpec_CardOwner(
            BoardItemPropertySOBase propertySO,
            BoardItemBase owner) : base(propertySO, owner)
        {
            CastedSO = (BoardItemProperty_CardOwner)propertySO;
        }
        
        public void SetCard(
            CardBase card)
        {
            Card = card;
        }
    }
}