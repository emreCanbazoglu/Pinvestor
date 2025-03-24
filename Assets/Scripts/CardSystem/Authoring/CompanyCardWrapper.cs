namespace Pinvestor.CardSystem.Authoring
{
    public class CompanyCardWrapper : CardWrapperBase
    {
        public CompanyCard CompanyCard => (CompanyCard)Card;

        protected override void WrapCardCore()
        {
            gameObject.name = "CompanyCardWrapper_" + CompanyCard.CastedCardDataSo.CompanyId;
        }
    }
}