using System.Collections.Generic;
using Pinvestor.GameConfigSystem;
using UnityEngine;

namespace Pinvestor.BoardSystem.Base
{
    public class BoardItem_Company : BoardItemBase
    {
        public BoardItemData_Company CompanyData => (BoardItemData_Company) BoardItemData;

        public CompanyConfigModel CompanyConfig { get; private set; }

        protected override void InitCore(
            BoardItemDataBase data)
        {
            if (GameConfigManager.Instance != null
                && GameConfigManager.Instance.IsInitialized
                && GameConfigManager.Instance.TryGetService(out CompanyConfigService configService))
            {
                if (!configService.TryGetCompanyConfig(CompanyData.RefCardId, out var config))
                {
                    Debug.LogError($"[BoardItem_Company] No CompanyConfigModel found for company ID '{CompanyData.RefCardId}'.");
                }
                else
                {
                    CompanyConfig = config;
                }
            }
            else
            {
                Debug.LogError($"[BoardItem_Company] GameConfigManager not available when initializing company '{CompanyData.RefCardId}'.");
            }

            base.InitCore(data);
        }

        protected override List<BoardItemPieceBase> CreatePieces(
            bool isPlaceholder = false)
        {
            List<BoardItemPieceBase> pieces = new List<BoardItemPieceBase>();

            BoardItemPiece_Company piece = new BoardItemPiece_Company(this);
            pieces.Add(piece);

            return pieces;
        }
    }

    public class BoardItemPiece_Company : BoardItemPieceBase
    {
        public BoardItemPiece_Company(BoardItemBase boardItem)
            : base(boardItem)
        {
        }
    }
}
