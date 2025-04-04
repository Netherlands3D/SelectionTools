using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.SelectionTools
{
    public class PolygonVisualisation : MonoBehaviour, IPointerClickHandler
    {
        private List<List<Vector3>> polygons;
        public List<List<Vector3>> Polygons => polygons; //todo make read only

        [Header("Events")] 
        public UnityEvent<PolygonVisualisation> reselectVisualisedPolygon = new UnityEvent<PolygonVisualisation>();

        [SerializeField] private bool drawMesh = true;

        public bool DrawMesh
        {
            get { return drawMesh; }
            set
            {
                drawMesh = value;
                EnableMeshRenderers(value);
            }
        }

        private float extrusionHeight;
        private bool addBottom;
        private bool createInwardMesh;
        private Vector2 uvCoordinate;

        [SerializeField] private bool activeCollider = true;

        public bool ActiveCollider
        {
            get { return activeCollider; }
            set
            {
                activeCollider = value;
                EnableColliders(value);
            }
        }

        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        [SerializeField] private bool drawLine = true;

        public bool DrawLine
        {
            get { return drawLine; }
            set
            {
                drawLine = value;
                EnableLineRenderers(value);
            }
        }

        private Material lineMaterial;
        private Color lineColor;
        public Mesh PolygonMesh { get; private set; }
        private Renderer renderer;

        public Material VisualisationMaterial
        {
            get => renderer.material;
            set => renderer.material = value;
        }
        
#if UNITY_EDITOR
            private void OnValidate()
        {
            EnableLineRenderers(drawLine);
            EnableMeshRenderers(drawMesh);
            EnableColliders(activeCollider);
        }
#endif

        private void Awake()
        {
            renderer = GetComponent<Renderer>();
        }

        /// <summary>
        /// Sets a reference of the polygon to be visualised
        /// </summary>
        /// <param name="polygon"></param>
        public void Initialize(List<List<Vector3>> sourcePolygons, float extrusionHeight, bool addBottom, bool createInwardMesh, Material lineMaterial, Color lineColor, Vector2 uvCoordinate = new Vector2())
        {
            polygons = sourcePolygons;
            this.extrusionHeight = extrusionHeight;
            this.addBottom = addBottom;
            this.createInwardMesh = createInwardMesh;
            this.uvCoordinate = uvCoordinate;
            this.lineMaterial = lineMaterial;
            this.lineColor = lineColor;

            UpdateVisualisation(sourcePolygons);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ReselectPolygon();
        }

        public void ReselectPolygon()
        {
            reselectVisualisedPolygon.Invoke(this);
        }

        public void UpdateVisualisation(List<Vector3> newPolygon)
        {
            UpdateVisualisation(new List<List<Vector3>>() { newPolygon });
        }

        public void UpdateVisualisation(List<List<Vector3>> newPolygon)
        {
            polygons = newPolygon;

            if (newPolygon.Count > 0)
            {
                var polygon2D = PolygonCalculator.FlattenPolygon(newPolygon[0], new Plane(Vector3.up, 0));
                var clockwise = PolygonCalculator.PolygonIsClockwise(polygon2D);

                if (clockwise == createInwardMesh) // (clockwise && inward) || (!clockwise && !inward)
                {
                    foreach (var contour in newPolygon)
                    {
                        contour.Reverse();
                    }
                }
            }

            DestroyPolygonMesh(); //cleanup to avoid memory leaks

            PolygonMesh = PolygonVisualisationUtility.CreatePolygonMesh(polygons, extrusionHeight, addBottom, uvCoordinate);
            var meshFilter = GetComponent<MeshFilter>();

            meshFilter.sharedMesh = PolygonMesh;

            var mc = GetComponent<MeshCollider>();
            if (mc)
                mc.sharedMesh = PolygonMesh;

            UpdateLineRenderers();

            EnableMeshRenderers(drawMesh);
            EnableLineRenderers(drawLine);
        }

        public void UpdateLineRenderers() //todo: reuse existing line renderers if this is possible and if this is significantly more performant
        {
            DestroyLineRenderers();
            lineRenderers = PolygonVisualisationUtility.CreateLineRenderers(polygons, lineMaterial, lineColor, transform);
        }

        private void DestroyLineRenderers()
        {
            for (int i = lineRenderers.Count - 1; i >= 0; i--)
            {
                Destroy(lineRenderers[i].gameObject);
            }

            lineRenderers = new List<LineRenderer>();
        }

        private void EnableLineRenderers(bool enable) // to set this programatically, set the property DrawLine
        {
            foreach (var line in lineRenderers)
            {
                line.gameObject.SetActive(enable);
            }
        }

        private void EnableMeshRenderers(bool value) // to set this programatically, set the property DrawMesh
        {
            GetComponent<MeshRenderer>().enabled = value;
        }

        private void EnableColliders(bool value)
        {
            var mc = GetComponent<MeshCollider>();
            if (mc)
                mc.enabled = value;
            else
                Debug.LogWarning("This PolygonVisualisation has to collider to enable/disable", gameObject);
        }
        
        private void DestroyPolygonMesh()
        {
            if (PolygonMesh)
            {
                Destroy(PolygonMesh);
                PolygonMesh = null;
            }
        }

        private void OnDestroy()
        {
            DestroyPolygonMesh();
            // VisualisationMaterial is global (for now), so they don't have to be destroyed here
        }
    }
}