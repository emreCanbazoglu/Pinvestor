using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MMFramework.MMUI
{
    [RequireComponent(typeof(GraphicRaycaster))]
    public class WorldInputBlockerCanvas : MonoBehaviour
    {
        private GraphicRaycaster _graphicRaycaster;
        public GraphicRaycaster GraphicRaycaster
        {
            get
            {
                if (_graphicRaycaster == null)
                    _graphicRaycaster = GetComponent<GraphicRaycaster>();
                return _graphicRaycaster;
            }
        }

        private void Update()
        {
            if(Input.touchSupported)
                UpdateTouch();
            else
                UpdateMouse();
        }

        private void OnDestroy()
        {
            if(WorldInputBlockerCanvasController.Instance == null)
                return;
            
            WorldInputBlockerCanvasController.Instance
                .UnregisterBlockRequest(this);
        }

        private void UpdateMouse()
        {
            if (Input.GetMouseButton(0))
            {
                PointerEventData pointerEventData 
                    = new PointerEventData(EventSystem.current);
                
                pointerEventData.position = Input.mousePosition;
                
                var results = new List<RaycastResult>();

                GraphicRaycaster.Raycast(pointerEventData, results);

                if (results.Count == 0)
                {
                    WorldInputBlockerCanvasController.Instance.UnregisterBlockRequest(
                        this, 0);
                    return;
                    
                }
                
                WorldInputBlockerCanvasController.Instance.RegisterBlockRequest(
                    this, 0);
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                WorldInputBlockerCanvasController.Instance.UnregisterBlockRequest(
                    this);
            }
        }

        private void UpdateTouch()
        {
            if (Input.touchCount == 0)
            {
                WorldInputBlockerCanvasController.Instance.UnregisterBlockRequest(
                    this);
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    PointerEventData pointerEventData 
                        = new PointerEventData(EventSystem.current);
                    
                    pointerEventData.position = touch.position;
                    
                    var results = new List<RaycastResult>();

                    GraphicRaycaster.Raycast(pointerEventData, results);

                    if (results.Count == 0)
                    {
                        WorldInputBlockerCanvasController.Instance.UnregisterBlockRequest(
                            this, touch.fingerId);
                        
                        continue;
                    }
                    
                    WorldInputBlockerCanvasController.Instance.RegisterBlockRequest(
                        this, touch.fingerId);
                }
            }
        }
    }
}
