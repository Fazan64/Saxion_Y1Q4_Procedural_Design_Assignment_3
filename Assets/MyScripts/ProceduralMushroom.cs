using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ProceduralMushroom : MonoBehaviour
{
    [SerializeField] [Range(3, 30)] private int numSplines = 10;
    [Space]
    
    [Header("Stem")]
    [SerializeField] float stemHeight = 1f;
    [SerializeField] [Range(2, 20)] int numStemSegments = 10;
    [SerializeField] float stemRadius = 0.2f;
    [SerializeField] Vector3 rotationPerHeightUnitEuler = new Vector3(5f, 0f, 0f);

    [Header("Cap")] 
    [SerializeField] float capHeight = 1f;
    [SerializeField] float capRadius = 1f;
    [SerializeField] [Range(2, 20)] int numCapSegments = 5;
    [SerializeField] [Range(0f, 1f)] float capShape;

    [Header("Debug")] 
    [SerializeField] private bool drawNormals;
    
    private bool isDirty = true;

    void OnValidate()
    {
        isDirty = true;
    }

    void Update()
    {
        if (isDirty)
        {
            UpdateMesh();
            isDirty = false;
        }

        if (drawNormals)
        {
            DrawNormals();
        }
    }
    
    public void UpdateMesh()
    {
        var lathe = new LatheMeshBuilder(numSplines);

        AddStem(lathe);
        AddCap(lathe);

        GetComponent<MeshFilter>().sharedMesh = lathe.CreateMesh();
    }

    private void AddStem(LatheMeshBuilder lathe)
    {
        var sideVertices = new Vector2[numStemSegments];
        float stemSegmentHeight = stemHeight / (numStemSegments - 1);
        for (int i = 0; i < numStemSegments; ++i)
        {
            sideVertices[i] = new Vector2(stemRadius, i * stemSegmentHeight);
        }
        
        lathe.Add(sideVertices, rotationPerHeightUnitEuler);
    }
    
    private void AddCap(LatheMeshBuilder lathe)
    {
        var sideVertices = new Vector2[numCapSegments];
        for (int i = 0; i < numCapSegments; ++i)
        {
            float t = (float)i / (numCapSegments - 1);
            float height = stemHeight + Mathf.Lerp(0f, capHeight, t);

            float radius = GetCapRadius(t);
            
            sideVertices[i] = new Vector2(radius, height);
        }
        
        lathe.Add(sideVertices);
    }

    private float GetCapRadius(float t)
    {
        float variantA = Mathf.Sqrt(-t + 1f);
        float variantB = 2f / (t + 1f) - 1f;

        return Mathf.Lerp(variantA, variantB, capShape) * capRadius;
    }

    private void DrawNormals()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        if (mesh == null) return;
        
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < vertices.Length; ++i)
        {
            Debug.DrawRay(transform.position + vertices[i], normals[i]);
        }
    }
}
