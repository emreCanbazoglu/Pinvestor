using System.Collections;
using System.Collections.Generic;
using GameplayTag.Authoring;
using UnityEngine;


namespace GameplayTag
{
    public class GameplayObject : MonoBehaviour
    {
        [SerializeField] private List<GameplayTagScriptableObject> _grantedTags
            = new List<GameplayTagScriptableObject>();

        public bool HasAllTags(params GameplayTagScriptableObject[] tags)
        {
            for (var iAbilityTag = 0; iAbilityTag < tags.Length; iAbilityTag++)
            {
                var abilityTag = tags[iAbilityTag];

                bool requirementPassed = false;
                
                for (var iAscTag = 0; iAscTag < _grantedTags.Count; iAscTag++)
                {
                    if (_grantedTags[iAscTag] == abilityTag)
                        requirementPassed = true;
                }
                
                if (!requirementPassed) 
                    return false;
            }
            return true;
        }

        public bool HasNoneTags(params GameplayTagScriptableObject[] tags)
        {
            for (var iAbilityTag = 0; iAbilityTag < tags.Length; iAbilityTag++)
            {
                var abilityTag = tags[iAbilityTag];

                bool requirementPassed = true;
                
                for (var iAscTag = 0; iAscTag < _grantedTags.Count; iAscTag++)
                {
                    if (_grantedTags[iAscTag] == abilityTag)
                        requirementPassed = false;
                }
                
                if (!requirementPassed) 
                    return false;
            }
            return true;
        }

        public bool TryAddTag(GameplayTagScriptableObject tag)
        {
            if (_grantedTags.Contains(tag))
                return false;
            
            _grantedTags.Add(tag);

            return true;
        }

        public bool TryRemoveTag(GameplayTagScriptableObject tag)
        {
            return _grantedTags.Remove(tag);
        }
    }
}
