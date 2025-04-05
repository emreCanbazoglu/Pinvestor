using UnityEngine;
using UnityEngine.EventSystems;
using Logger = MMFramework.Logging.Logger;

namespace MildMania.PuzzleGameTemplate.Event
{
    public class EventSystemHelper : MonoBehaviour
    {
        private void Awake()
        {
            EnsureEventSystemExists();
        }
        
        public bool EnsureEventSystemExists()
        {
            if (EventSystem.current != null)
            {
                Logger.Log("[EventSystemHelper] EventSystem.current exists.");
                
                return false;
            }

            EventSystem eventSystem = FindObjectOfType<EventSystem>();

            // Check if EventSystem is in the scene but not registered yet
            if (eventSystem != null && eventSystem.gameObject.activeSelf && eventSystem.enabled)
            {
                Logger.Log("[EventSystemHelper] EventSystem found in scene: " + eventSystem.gameObject.name);
                
                return false;
            }

            Logger.LogWarning("[EventSystemHelper] No EventSystem found in scene - creating a default one.");

            CreateDefaultEventSystem();
            
            return true;
        }

        private void CreateDefaultEventSystem()
        {
            GameObject go = new GameObject("EventSystem (Created by EventSystemHelper)");
            
            go.AddComponent<EventSystem>();

            AddLegacyInputSystem(go);
        }
        
        private static void AddLegacyInputSystem(GameObject go)
        {
            go.AddComponent<StandaloneInputModule>();
        }
    }
}