using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
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
    [SerializeField] float capOverhang = 0.05f;
    [SerializeField] [Range(2, 20)] int numCapSegments = 5;
    [SerializeField] [Range(0f, 1f)] float capShape;

    [Header("Texture")] 
    [SerializeField] private Color color;

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

        Mesh mesh = lathe.CreateMesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void Randomize()
    {
        stemHeight = Random.Range(0.1f, 5f);
        stemRadius = Random.Range(0.05f, 0.5f);
        
        capHeight   = Random.Range(0.5f, 3f);
        capOverhang = Random.Range(0.05f, 0.5f);
        capShape    = Random.Range(0f, 1f);
        
        rotationPerHeightUnitEuler = Random.onUnitSphere * Random.Range(0f, 30f);

        isDirty = true;
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

        return Mathf.Lerp(variantA, variantB, capShape) * (stemRadius + capOverhang);
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

    public float[] GetGenes()
    {
        return new[]
        {
            stemHeight,
            stemRadius,
            capHeight,
            capOverhang,
            capShape,
            rotationPerHeightUnitEuler.x,
            rotationPerHeightUnitEuler.y,
            rotationPerHeightUnitEuler.z
        };
        
        /*
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(stemHeight);
                writer.Write(stemRadius);
                writer.Write(capHeight);
                writer.Write(capOverhang);
                writer.Write(capShape);

                writer.Write(rotationPerHeightUnitEuler.x);
                writer.Write(rotationPerHeightUnitEuler.y);
                writer.Write(rotationPerHeightUnitEuler.z);
            }

            using (var reader = new BinaryReader(stream))
            {
                return Enumerable
                    .Range(0, (int)stream.Length * sizeof(byte) / sizeof(float))
                    .Select(i => reader.ReadSingle())
                    .ToArray();
            }
        }*/
    }

    public void SetGenes(float[] genes)
    {
        Assert.AreEqual(8, genes.Length);
        
        stemHeight   = genes[0];
        stemRadius   = genes[1];
        capHeight    = genes[2];
        capOverhang  = genes[3];
        capShape     = genes[4];
        rotationPerHeightUnitEuler.x = genes[5];
        rotationPerHeightUnitEuler.y = genes[6];
        rotationPerHeightUnitEuler.z = genes[7];
        
        /*using (var stream = new MemoryStream(genes))
        using (var reader = new BinaryReader(stream))
        {
            stemHeight = reader.ReadSingle();
            stemRadius = reader.ReadSingle();
            capHeight  = reader.ReadSingle();
            capRadius  = reader.ReadSingle();
            capShape   = reader.ReadSingle();
            
            rotationPerHeightUnitEuler = new Vector3(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle()
            );
        }*/
    }
    
    // NOTES
    // IGenetic<float>
}
