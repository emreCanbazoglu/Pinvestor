using System;
using System.Collections.Generic;
using System.Reflection;
using AbilitySystem;
using AbilitySystem.Authoring;
using AbilitySystem.ModifierMagnitude;
using AttributeSystem.Authoring;
using AttributeSystem.Components;
using NUnit.Framework;
using UnityEngine;

public class AbilitySystemCharacterModifierRecompositionTests
{
    private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();
    private static readonly MethodInfo UpdateMethod = typeof(AbilitySystemCharacter)
        .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

    [TearDown]
    public void TearDown()
    {
        for (int i = _createdObjects.Count - 1; i >= 0; i--)
        {
            if (_createdObjects[i] != null)
                UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
        }

        _createdObjects.Clear();
    }

    [Test]
    public void NonPeriodicEffect_AppliesOnNextFrame_WithoutAccumulating()
    {
        AbilitySystemCharacter character = CreateCharacterWithSingleAttribute(100f, out AttributeScriptableObject attribute);
        GameplayEffectScriptableObject addEffect = CreateDurationalEffect(
            attribute,
            EDurationPolicy.Infinite,
            periodIsPeriodic: false,
            modifierOperator: EAttributeModifier.Add,
            modifierMagnitude: 10f,
            durationMagnitude: 1f);

        GameplayEffectSpec addSpec = GameplayEffectSpec.CreateNew(addEffect, character, null, 1f);
        character.ApplyGameplayEffectSpecToSelf(addSpec);

        AssertCurrentValue(character.AttributeSystem, attribute, 100f);

        SimulateFrame(character);
        AssertCurrentValue(character.AttributeSystem, attribute, 110f);

        SimulateFrame(character);
        AssertCurrentValue(character.AttributeSystem, attribute, 110f);
    }

    [Test]
    public void RemovingNonPeriodicEffect_RecomposesOnNextFrame_WithDeterministicResult()
    {
        AbilitySystemCharacter character = CreateCharacterWithSingleAttribute(100f, out AttributeScriptableObject attribute);
        GameplayEffectScriptableObject addEffect = CreateDurationalEffect(
            attribute,
            EDurationPolicy.Infinite,
            periodIsPeriodic: false,
            modifierOperator: EAttributeModifier.Add,
            modifierMagnitude: 10f,
            durationMagnitude: 1f);
        GameplayEffectScriptableObject multiplyEffect = CreateDurationalEffect(
            attribute,
            EDurationPolicy.Infinite,
            periodIsPeriodic: false,
            modifierOperator: EAttributeModifier.Multiply,
            modifierMagnitude: 0.5f,
            durationMagnitude: 1f);

        GameplayEffectContainer addContainer = character.ApplyGameplayEffectSpecToSelf(
            GameplayEffectSpec.CreateNew(addEffect, character, null, 1f));
        character.ApplyGameplayEffectSpecToSelf(
            GameplayEffectSpec.CreateNew(multiplyEffect, character, null, 1f));

        SimulateFrame(character);
        AssertCurrentValue(character.AttributeSystem, attribute, 165f);

        character.RemoveGameplayEffectSpecFromSelf(addContainer);
        AssertCurrentValue(character.AttributeSystem, attribute, 165f);

        SimulateFrame(character);
        AssertCurrentValue(character.AttributeSystem, attribute, 150f);
    }

    [Test]
    public void ExpiredDurationEffect_IsRemovedInClean_AndAffectsNextFrame()
    {
        AbilitySystemCharacter character = CreateCharacterWithSingleAttribute(100f, out AttributeScriptableObject attribute);
        GameplayEffectScriptableObject expiringEffect = CreateDurationalEffect(
            attribute,
            EDurationPolicy.HasDuration,
            periodIsPeriodic: false,
            modifierOperator: EAttributeModifier.Add,
            modifierMagnitude: 20f,
            durationMagnitude: 1f);

        GameplayEffectSpec expiredAtApplySpec = GameplayEffectSpec.CreateNew(
            expiringEffect,
            character,
            null,
            level: 1f,
            passedDuration: 2f);

        character.ApplyGameplayEffectSpecToSelf(expiredAtApplySpec);

        SimulateFrame(character);
        AssertCurrentValue(character.AttributeSystem, attribute, 120f);

        SimulateFrame(character);
        AssertCurrentValue(character.AttributeSystem, attribute, 100f);
    }

    private AbilitySystemCharacter CreateCharacterWithSingleAttribute(
        float baseValue,
        out AttributeScriptableObject attribute)
    {
        attribute = Track(ScriptableObject.CreateInstance<AttributeScriptableObject>());
        attribute.Name = "TestAttribute";

        var baseValueModifier = Track(ScriptableObject.CreateInstance<ConstantBaseValueModifier>());
        baseValueModifier.Value = baseValue;

        AttributeDefinition definition = new AttributeDefinition();
        SetAutoProperty(definition, "Attribute", attribute);
        SetAutoProperty(definition, "IsPrimaryAttribute", true);
        SetAutoProperty(definition, "Multiplier", 1f);
        SetAutoProperty(definition, "BaseValueModifier", baseValueModifier);
        SetAutoProperty(definition, "UseModifierObjectProviderForAttributeSet", false);
        SetAutoProperty(definition, "ModifierObjectProvider", null);

        var attributeSet = Track(ScriptableObject.CreateInstance<AttributeSetScriptableObject>());
        SetAutoProperty(attributeSet, "AttributeDefinitions", new[] { definition });

        GameObject go = Track(new GameObject("AbilitySystemCharacter_Test"));
        var attributeSystem = go.AddComponent<AttributeSystemComponent>();
        var character = go.AddComponent<AbilitySystemCharacter>();
        character.AttributeSystem = attributeSystem;

        // Lock tests to the legacy recalculation behavior to match current runtime defaults.
        FieldInfo modeField = typeof(AttributeSystemComponent)
            .GetField("_recalculationMode", BindingFlags.Instance | BindingFlags.NonPublic);
        object legacyMode = Enum.Parse(
            typeof(AttributeSystemComponent.EAttributeRecalculationMode),
            "LegacyEveryLateUpdate");
        modeField.SetValue(attributeSystem, legacyMode);

        attributeSystem.Initialize(attributeSet);
        attributeSystem.UpdateAttributeCurrentValues();

        return character;
    }

    private GameplayEffectScriptableObject CreateDurationalEffect(
        AttributeScriptableObject attribute,
        EDurationPolicy durationPolicy,
        bool periodIsPeriodic,
        EAttributeModifier modifierOperator,
        float modifierMagnitude,
        float durationMagnitude)
    {
        var magnitude = Track(ScriptableObject.CreateInstance<ConstantMagnitudeModifier>());
        magnitude.Value = modifierMagnitude;

        var duration = Track(ScriptableObject.CreateInstance<ConstantMagnitudeModifier>());
        duration.Value = durationMagnitude;

        var effect = Track(ScriptableObject.CreateInstance<GameplayEffectScriptableObject>());
        effect.gameplayEffect = new GameplayEffectDefinitionContainer
        {
            DurationPolicy = durationPolicy,
            DurationModifier = duration,
            DurationMultiplier = 1f,
            Modifiers = new[]
            {
                new GameplayEffectModifier
                {
                    Attribute = attribute,
                    ModifierOperator = modifierOperator,
                    ModifierMagnitude = magnitude,
                    Multiplier = 1f,
                }
            }
        };
        effect.Period = new GameplayEffectPeriod
        {
            IsPeriodic = periodIsPeriodic,
            Period = 1f,
            ExecuteOnApplication = false,
        };
        effect.gameplayEffectTags = new GameplayEffectTags
        {
            GrantedTags = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
            RemoveGameplayEffectsWithTag = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
            OngoingTagRequirements = new GameplayTagRequireIgnoreContainer
            {
                RequireTags = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
                IgnoreTags = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
            },
            ApplicationTagRequirements = new GameplayTagRequireIgnoreContainer
            {
                RequireTags = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
                IgnoreTags = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
            },
            RemovalTagRequirements = new GameplayTagRequireIgnoreContainer
            {
                RequireTags = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
                IgnoreTags = Array.Empty<GameplayTag.Authoring.GameplayTagScriptableObject>(),
            }
        };
        effect.ModifierAppliedHandlers = Array.Empty<GameplayEffectModifierAppliedHandlerScriptableObject>();

        return effect;
    }

    private static void SimulateFrame(AbilitySystemCharacter character)
    {
        UpdateMethod.Invoke(character, null);
        character.AttributeSystem.UpdateAttributeCurrentValues();
    }

    private static void AssertCurrentValue(
        AttributeSystemComponent attributeSystem,
        AttributeScriptableObject attribute,
        float expected)
    {
        Assert.IsTrue(attributeSystem.TryGetAttributeValue(attribute, out AttributeValue value));
        Assert.That(value.CurrentValue, Is.EqualTo(expected).Within(0.001f));
    }

    private T Track<T>(T obj) where T : UnityEngine.Object
    {
        _createdObjects.Add(obj);
        return obj;
    }

    private static void SetAutoProperty<TTarget>(
        TTarget target,
        string propertyName,
        object value)
    {
        FieldInfo field = typeof(TTarget).GetField(
            $"<{propertyName}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Backing field for property '{propertyName}' was not found on {typeof(TTarget).Name}.");
        field.SetValue(target, value);
    }

    private sealed class ConstantMagnitudeModifier : ModifierMagnitudeScriptableObject
    {
        public float Value;

        public override float? CalculateMagnitude(GameplayEffectSpec spec)
        {
            return Value;
        }
    }

    private sealed class ConstantBaseValueModifier : AttributeBaseValueModifierScriptableObject
    {
        public float Value;

        public override float CalculateBaseValue(
            IAttributeValueProvider attributeValueProvider,
            object modifierObject = null)
        {
            return Value;
        }
    }
}
