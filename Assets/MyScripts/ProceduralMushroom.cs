using System;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(Renderer))]
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
    [SerializeField] MushroomTextureGenerator textureGenerator = new MushroomTextureGenerator();

    [Header("Debug")] 
    [SerializeField] private bool drawNormals;

    private new Renderer renderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    
    private bool isDirty = true;
    private bool wasChangedInInspector;
    private bool didStart;

    private CancellationTokenSource asyncUpdateCancellationTokenSource;

    void Start()
    {
        didStart = true;
        
        renderer = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    void OnValidate()
    {
        isDirty = true;
        wasChangedInInspector = didStart;

        //Debug.Log("Cancelling " + cancellationTokenSource);
        asyncUpdateCancellationTokenSource?.Cancel();
    }

    void Update()
    {
        if (isDirty)
        {
            if (wasChangedInInspector)
            {
                UpdateMeshAndTexture();
            }
            else
            {
                UpdateMeshAndTextureAsync();
            }

            isDirty = false;
            wasChangedInInspector = false;
        }

        if (drawNormals)
        {
            DrawNormals();
        }
    }

    void OnDestroy()
    {
        asyncUpdateCancellationTokenSource?.Cancel();
    }
    
    public void Randomize()
    {
        stemHeight = Random.Range(0.1f, 5f);
        stemRadius = Random.Range(0.05f, 0.5f);
        
        capHeight   = Random.Range(0.5f, 3f);
        capOverhang = Random.Range(0.05f, 0.5f);
        capShape    = Random.Range(0f, 1f);
        
        rotationPerHeightUnitEuler = Random.onUnitSphere * Random.Range(0f, 30f);

        textureGenerator.colorA = RandomColor();
        textureGenerator.colorB = RandomColor();
        textureGenerator.scale  = new Vector2(Random.Range(2f, 32f), Random.Range(2f, 32f));
        textureGenerator.offset = new Vector2(Random.Range(-100f, 100f), Random.Range(-100f, 100f));

        isDirty = true;
    }

    private static Color RandomColor()
    {
        return Random.ColorHSV(
            hueMin:        0f  , hueMax:        1f,
            saturationMin: 0.5f, saturationMax: 1f,
            valueMin:      0.8f, valueMax:      1f
        );
    }

    private void UpdateMeshAndTexture()
    {
        var lathe = new LatheMeshBuilder(numSplines);
        BuildMushroomModel(lathe);
        Mesh mesh = lathe.CreateMesh();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        renderer.material.mainTexture = textureGenerator.GenerateTexture();
    }

    private async void UpdateMeshAndTextureAsync()
    {
        asyncUpdateCancellationTokenSource = new CancellationTokenSource();

        try
        {
            var lathe = new LatheMeshBuilder(numSplines);

            Task meshTask = Task.Run(() => BuildMushroomModel(lathe), asyncUpdateCancellationTokenSource.Token);
            Task<Texture2D> textureTask = textureGenerator.GenerateTextureAsync(asyncUpdateCancellationTokenSource.Token);

            await Task.WhenAll(meshTask, textureTask);

            asyncUpdateCancellationTokenSource.Token.ThrowIfCancellationRequested();
            
            Mesh mesh = lathe.CreateMesh();
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            renderer.material.mainTexture = await textureTask;
        }
        catch (OperationCanceledException ex)
        {
            //Debug.Log(ex);
        }

        asyncUpdateCancellationTokenSource = null;
    }

    private void BuildMushroomModel(LatheMeshBuilder lathe)
    {
        AddStem(lathe);
        AddCap(lathe);
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
            Debug.DrawRay(transform.TransformPoint(vertices[i]), transform.TransformDirection(normals[i]), Color.black);
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
            rotationPerHeightUnitEuler.z,
            
            textureGenerator.colorA.r,
            textureGenerator.colorA.g,
            textureGenerator.colorA.b,
            
            textureGenerator.colorB.r,
            textureGenerator.colorB.g,
            textureGenerator.colorB.b,
            
            textureGenerator.scale.x,
            textureGenerator.scale.y,
            
            textureGenerator.offset.x,
            textureGenerator.offset.y
        };
    }

    public void SetGenes(float[] genes)
    {
        Assert.AreEqual(18, genes.Length);
        
        stemHeight   = genes[0];
        stemRadius   = genes[1];
        capHeight    = genes[2];
        capOverhang  = genes[3];
        capShape     = genes[4];
        
        rotationPerHeightUnitEuler.x = genes[5];
        rotationPerHeightUnitEuler.y = genes[6];
        rotationPerHeightUnitEuler.z = genes[7];
        
        textureGenerator.colorA.r = genes[8];
        textureGenerator.colorA.g = genes[9];
        textureGenerator.colorA.b = genes[10];
        
        textureGenerator.colorB.r = genes[11];
        textureGenerator.colorB.g = genes[12];
        textureGenerator.colorB.b = genes[13];
        
        textureGenerator.scale.x = genes[14];
        textureGenerator.scale.y = genes[15];
        
        textureGenerator.offset.x = genes[16];
        textureGenerator.offset.y = genes[17];

        isDirty = true;
    }
}
