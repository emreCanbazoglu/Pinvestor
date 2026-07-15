using NUnit.Framework;
using Pinvestor.CompanySystem;
using Pinvestor.UI;

namespace Pinvestor.Tests.Editor
{
    public sealed class MvpVisualThemeTests
    {
        [TestCase("CreditKaraoke", "Credit Karaoke")]
        [TestCase("AutoPilotPantry", "Auto Pilot Pantry")]
        [TestCase("RepoReaper", "Repo Reaper")]
        public void HumanizeCompanyName_SplitsPascalCase(string companyId, string expected)
        {
            Assert.That(MvpVisualTheme.HumanizeCompanyName(companyId), Is.EqualTo(expected));
        }

        [TestCase("CreditKaraoke", "CK")]
        [TestCase("RepoReaper", "RR")]
        [TestCase("Clout", "CL")]
        public void GetMonogram_ReturnsStableReadableCode(string companyId, string expected)
        {
            Assert.That(MvpVisualTheme.GetMonogram(companyId), Is.EqualTo(expected));
        }

        [Test]
        public void Categories_HaveBothTextAndDistinctColorEncoding()
        {
            Assert.That(MvpVisualTheme.GetCategoryLabel(ECompanyCategory.FinTech), Is.EqualTo("FINTECH"));
            Assert.That(
                MvpVisualTheme.GetCategoryColor(ECompanyCategory.FinTech),
                Is.Not.EqualTo(MvpVisualTheme.GetCategoryColor(ECompanyCategory.SocialMedia)));
        }
    }
}
