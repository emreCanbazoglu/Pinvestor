using System;

namespace AbilitySystem.Authoring
{
    public enum EDescriptionTone
    {
        Neutral,
        Positive,
        Negative
    }
    
    [Serializable]
    public struct EffectDescriptionMeta
    {
        public string Verb;                 // e.g., "boosts", "drains", "inflicts"
        public EDescriptionTone Tone;       // Positive, Negative, Neutral
        public string CustomTargetPhrase;  // Optional override for targeting text
    }
}