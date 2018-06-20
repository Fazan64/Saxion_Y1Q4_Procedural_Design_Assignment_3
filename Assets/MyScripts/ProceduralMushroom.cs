using System.Linq.Expressions;
using Boo.Lang;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ProceduralMushroom : MonoBehaviour
{
    [Header("Stem")]
    [SerializeField] float stemHeight = 1f;
    [SerializeField] [Range(2, 20)] int numStemSegments = 10;
    [SerializeField] float stemRadius = 0.2f;
    [SerializeField] Vector3 rotationPerHeightUnitEuler =  new Vector3(5f, 0f, 0f);

    [Header("Cap")] 
    [SerializeField] float capHeight = 1f;
    [SerializeField] float capRadius = 1f;
    [SerializeField] [Range(2, 20)] int numCapSegments = 5;

    [Space]
    [SerializeField] [Range(3, 30)] private int numSplines = 10;
    
    private bool isDirty = true;

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
            float radius = Mathf.Sqrt(-t + 1) * capRadius;
            sideVertices[i] = new Vector2(radius, height);
        }
        
        lathe.Add(sideVertices);
    }
}
