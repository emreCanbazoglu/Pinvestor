using NUnit.Framework;
using Pinvestor.BoardSystem.Authoring;
using Pinvestor.Game.Health;

public class CashoutModifierPipelineTests
{
    [Test]
    public void Apply_ComposesModifiersInProvidedOrder()
    {
        ICashoutValueModifier[] modifiers =
        {
            new AddModifier(10f),
            new MultiplyModifier(2f),
        };

        float result = CashoutModifierPipeline.Apply(
            modifiers,
            cashingOutCompany: null,
            payoutAmount: 50f);

        Assert.That(result, Is.EqualTo(120f));
    }

    private sealed class AddModifier : ICashoutValueModifier
    {
        private readonly float _amount;

        public AddModifier(float amount)
        {
            _amount = amount;
        }

        public float ModifyCashoutValue(
            BoardItemWrapper_Company cashingOutCompany,
            float currentValue)
        {
            return currentValue + _amount;
        }
    }

    private sealed class MultiplyModifier : ICashoutValueModifier
    {
        private readonly float _multiplier;

        public MultiplyModifier(float multiplier)
        {
            _multiplier = multiplier;
        }

        public float ModifyCashoutValue(
            BoardItemWrapper_Company cashingOutCompany,
            float currentValue)
        {
            return currentValue * _multiplier;
        }
    }
}
