using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Netherlands3D.SelectionTools
{
    public static class Interface
    {
        private static int defaultUILayer = LayerMask.NameToLayer("UI"); 

        /// <summary>
        /// Checks if the pointer is over a GameObject that is on the UI layer
        /// </summary>
        /// <param name="pointerId">Pointer or touch ID</param>
        /// <param name="layer">Blocking layer ( defaults to 'UI' layer ). Use LayerMask.NameToLayer("layername") to get layer id.</param>
        /// <returns>If pointer is over a GameObject with the UI layer</returns>
        public static bool PointerIsOverUI(int pointerId = 0, int layer = -1)
        {
            int blockingLayer = defaultUILayer;
            if(layer != -1)
                blockingLayer = layer;

            InputSystemUIInputModule inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
        
            var getLastRaycastResult = inputModule.GetLastRaycastResult(pointerId);
            if(!getLastRaycastResult.gameObject)
                return false;

            var gameObjectIsOnUILayer = getLastRaycastResult.gameObject.layer == blockingLayer;
            return gameObjectIsOnUILayer;
        }

        /// <summary>
        /// Returns the last raycast result from the InputSystemUIInputModule
        /// </summary>
        /// <returns>A GameObject or null</returns>
        public static GameObject GetLastRaycastResult(int pointerId = 0)
        {
            InputSystemUIInputModule inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            var getLastRaycastResult = inputModule.GetLastRaycastResult(pointerId);
            return getLastRaycastResult.gameObject;
        }

        /// <summary>
        /// Shortcut method that returns the click InputAction from the InputSystemUIInputModule
        /// To listen to the click you can do this:
        /// action.performed += ctx => { Debug.log("Click"); }; 
        /// </summary>
        /// <returns>InputAction for the Click action</returns>
        public static InputAction DefaultClickAction()
        {
             InputSystemUIInputModule inputModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
             var action = inputModule.actionsAsset.FindActionMap("UI").FindAction("Click");
            
             return action;
        }

        /// <summary>
        /// Method to check if current input module is the InputSystemUIInputModule, and compatible with these utility methods
        /// </summary>
        public static bool CurrentInputModuleIsInputSystemUIInputModule()
        {
            if(!EventSystem.current)
                return false;

            var inputSystem = EventSystem.current.currentInputModule is InputSystemUIInputModule;
            return inputSystem;
        }

    }
}
