/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/3DAmsterdam/blob/master/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Netherlands3D.SelectionTools
{
    public class AreaSelection : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActionAsset;
        private InputActionMap areaSelectionActionMap;
        private MeshRenderer boundsMeshRenderer;

        [Header("Invoke")]
        [SerializeField] private UnityEvent<bool> blockCameraDragging = new();
        [Tooltip("Fires while a new area is being drawn")]
        [SerializeField] private UnityEvent<Bounds> whenDrawingArea = new();
        [FormerlySerializedAs("selectedAreaBounds")]
        [Tooltip("Fires when an area is selected")]
        [SerializeField] private UnityEvent<Bounds> whenAreaIsSelected = new();

        [Header("Settings")]
        [SerializeField] private float gridSize = 100;
        [SerializeField] private float multiplyHighlightScale = 5.0f;
        [SerializeField] private float maxSelectionDistanceFromCamera = 10000;
        [SerializeField] private bool useWorldSpace = false;
        [SerializeField] private bool blockSelectinStartByUI = true;

        private InputAction pointerAction;
        private InputAction tapAction;
        private InputAction clickAction;
        private InputAction modifierAction;

        private Action<InputAction.CallbackContext> tapActionPerformed;
        private Action<InputAction.CallbackContext> clickActionPerformed;
        private Action<InputAction.CallbackContext> clickActionCanceled;

        private Vector3 selectionStartPosition;

        private Plane worldPlane;

        [SerializeField] private GameObject gridHighlight;
        [SerializeField] private GameObject selectionBlock;
        [SerializeField] private Material triplanarGridMaterial;

        private bool drawingArea = false;

        void Awake()
        {
            if(!inputActionAsset)
            {
                Debug.LogWarning("No input asset set. Please add the reference in the inspector.", this.gameObject);
                return;
            }
            if (!selectionBlock)
            {
                Debug.LogWarning("The selection block reference is not set in the inspector. Please make sure to set the reference.", this.gameObject);
                return;
            }
            boundsMeshRenderer = selectionBlock.GetComponent<MeshRenderer>();
            selectionBlock.SetActive(false);

            areaSelectionActionMap = inputActionAsset.FindActionMap("AreaSelection");
            tapAction = areaSelectionActionMap.FindAction("Tap");
            clickAction = areaSelectionActionMap.FindAction("Click");
            pointerAction = areaSelectionActionMap.FindAction("Point");
            modifierAction = areaSelectionActionMap.FindAction("Modifier");

            tapActionPerformed = context => Tap();
            clickActionPerformed = context => StartClick();
            clickActionCanceled = context => Release();

            worldPlane = (useWorldSpace) ? new Plane(Vector3.up, Vector3.zero) : new Plane(this.transform.up, this.transform.position);
        }

        private void OnValidate()
        {
            if(selectionBlock)
                selectionBlock.transform.localScale = Vector3.one * gridSize;

            if(gridHighlight)
                gridHighlight.transform.localScale = Vector3.one * gridSize * multiplyHighlightScale;

            if(triplanarGridMaterial)
               triplanarGridMaterial.SetFloat("GridSize", 1.0f / gridSize);
        }

        private void OnEnable()
        {
            areaSelectionActionMap.Enable();

            tapAction.performed += tapActionPerformed;
            clickAction.performed += clickActionPerformed;
            clickAction.canceled += clickActionCanceled;
        }

        private void OnDisable()
        {
            drawingArea = false;
            selectionBlock.SetActive(false);
            blockCameraDragging.Invoke(false);
            areaSelectionActionMap.Disable();

            tapAction.performed -= tapActionPerformed;
            clickAction.performed -= clickActionPerformed;
            clickAction.canceled -= clickActionCanceled;
        }

        private void Update()
        {
            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            var currentWorldCoordinate = GetGridPosition(worldPosition);
            gridHighlight.transform.position = currentWorldCoordinate;

            if (!drawingArea && clickAction.IsPressed() && modifierAction.IsPressed() )
            {
                if(blockSelectinStartByUI && Interface.PointerIsOverUI())
                    return;

                drawingArea = true;
                blockCameraDragging.Invoke(true);
            }
            else if (drawingArea && !clickAction.IsPressed())
            {
                drawingArea = false;
                blockCameraDragging.Invoke(false);
            }

            if (drawingArea)
            {
                DrawSelectionArea(selectionStartPosition, currentWorldCoordinate);
            }
        }
        
        private void Tap()
        {
            if(blockSelectinStartByUI && Interface.PointerIsOverUI())
                return;

            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            var tappedPosition = GetGridPosition(worldPosition);
            DrawSelectionArea(tappedPosition, tappedPosition);
            MakeSelection();
        }

        private void StartClick()
        {
            if(Interface.PointerIsOverUI())
                return;

            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            selectionStartPosition = GetGridPosition(worldPosition);
        }

        private void Release()
        {
            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            var selectionEndPosition = GetGridPosition(worldPosition);

            if (drawingArea)
            {
                DrawSelectionArea(selectionStartPosition, selectionEndPosition);
                MakeSelection();
            }
        }

        private void MakeSelection()
        {
            var bounds = boundsMeshRenderer.bounds;
            whenAreaIsSelected.Invoke(bounds);
        }

        /// <summary>
        /// Get a rounded position using the grid size
        /// </summary>
        /// <param name="samplePosition">The position to round to grid position</param>
        /// <returns></returns>
        private Vector3Int GetGridPosition(Vector3 samplePosition)
        {
            samplePosition.x += (gridSize * 0.5f);
            samplePosition.z += (gridSize * 0.5f);

            samplePosition.x = (Mathf.Round(samplePosition.x / gridSize) * gridSize) - (gridSize * 0.5f);
            samplePosition.z = (Mathf.Round(samplePosition.z / gridSize) * gridSize) - (gridSize * 0.5f);

            Vector3Int roundedPosition = new Vector3Int
            {
                x = Mathf.RoundToInt(samplePosition.x),
                y = Mathf.RoundToInt(samplePosition.y),
                z = Mathf.RoundToInt(samplePosition.z)
            };

            return roundedPosition;
        }

        /// <summary>
        /// Draw selection area by scaling the block
        /// </summary>
        /// <param name="currentWorldCoordinate">Current pointer position in world</param>
        private void DrawSelectionArea(Vector3 startWorldCoordinate, Vector3 currentWorldCoordinate)
        {
            selectionBlock.SetActive(true);

            var xDifference = (currentWorldCoordinate.x - startWorldCoordinate.x);
            var zDifference = (currentWorldCoordinate.z - startWorldCoordinate.z);

            selectionBlock.transform.position = startWorldCoordinate;
            selectionBlock.transform.Translate(xDifference / 2.0f, 0, zDifference / 2.0f);
            selectionBlock.transform.localScale = new Vector3(
                    (currentWorldCoordinate.x - startWorldCoordinate.x) + ((xDifference < 0) ? -gridSize : gridSize),
                    gridSize,
                    (currentWorldCoordinate.z - startWorldCoordinate.z) + ((zDifference < 0) ? -gridSize : gridSize));
            
            var bounds = boundsMeshRenderer.bounds;
            whenDrawingArea.Invoke(bounds);
        }
    }
}