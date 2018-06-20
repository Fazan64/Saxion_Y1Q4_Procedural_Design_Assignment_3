using UnityEngine;

namespace Handout
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class LatheSpline : MonoBehaviour
    {
        [SerializeField] [Range(3, 30)] int numSplines = 10;  //number of segments around the mesh

        [SerializeField] Vector3 rotationPerUnitHeightEuler = new Vector3(5f, 0f, 0f);
        
        //array to store a 2D spline
        [SerializeField] Vector2[] sideVertices = 
        {
            new Vector2(1, -1),
            new Vector2(2, 0),
            new Vector2(1, 1)
        };

        bool isDirty = true;

        void OnValidate()
        {
            isDirty = true;
        }

        void Update()
        {
            if (!isDirty) return;
            
            UpdateMesh();
            isDirty = false;
        }

        //this method updates the mesh if needed
        public void UpdateMesh()
        {
            //nothing changed
            if (isDirty == false) return;

            //start building mesh
            var lathe = new LatheMeshBuilder(numSplines);
            
            lathe.Add(sideVertices, rotationPerUnitHeightEuler);

            //generate mesh and apply it to meshfilter
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.sharedMesh = lathe.CreateMesh();
        }
    }
}