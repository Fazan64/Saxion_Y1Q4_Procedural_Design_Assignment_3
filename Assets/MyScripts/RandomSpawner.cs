using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

[ExecuteInEditMode]
public class RandomSpawner : MonoBehaviour
{
    private const float RaycastMaxDistance = 200f;
    
    [SerializeField] GameObject prefab;
    [SerializeField] float maxHeight = 100f;
    [SerializeField] [Range(0f, 1000f)] float range = 100f;
    [SerializeField] float cellSize = 30f;
    [SerializeField] Vector3 offset = Vector3.zero;
    [SerializeField] LayerMask terrainCheckLayerMask = Physics.DefaultRaycastLayers;

    private int numCellsSide;
    private int numCells;
    private List<int> freeCellIndices;

    void Awake()
    {
        Reset();
    }
    
    void Start()
    {
        Assert.IsNotNull(prefab);
    }

    public void Reset()
    {
        Clear();
        
        numCellsSide = Mathf.FloorToInt(range / cellSize);
        if (numCellsSide <= 0) numCellsSide = 1;

        numCells = numCellsSide * numCellsSide;
        
        freeCellIndices = Enumerable.Range(0, numCells).ToList();
    }

    public GameObject Make()
    {
        if (freeCellIndices.Count == 0)
        {
            Debug.LogWarning("No free cells left.");
            return null;
        }

        int i = Random.Range(0, freeCellIndices.Count);
        int cellIndex = freeCellIndices[i];
        freeCellIndices.RemoveAt(i);

        int x = cellIndex % numCellsSide;
        int y = cellIndex / numCellsSide;
            
        var position = transform.position + new Vector3(
            (x - numCellsSide / 2) * cellSize,
            maxHeight,
            (y - numCellsSide / 2) * cellSize
        );

        return Generate(cellIndex, position);
    }

    public void Clear()
    {
        IEnumerable<Transform> children = Enumerable
            .Range(0, transform.childCount)
            .Select(i => transform.GetChild(i));
        
        foreach (Transform child in children)
        {
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    public void FreeCell(int cellIndex)
    {
        int i = freeCellIndices.BinarySearch(cellIndex);
        if (i <= -1 || i >= freeCellIndices.Count)
        {
            freeCellIndices.Add(cellIndex);
            return;
        }
        freeCellIndices.Insert(i, cellIndex);
    }

    private GameObject Generate(int cellIndex, Vector3 position)
    {
        RaycastHit hit;
        if (!Physics.Raycast(position, Vector3.down, out hit, RaycastMaxDistance, terrainCheckLayerMask))
        {
            return null;
        }

        var rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
        var go = Instantiate(prefab, hit.point + offset, rotation, transform);
        
        go.AddComponent<GridCell>().Initialize(this, cellIndex);
        
        return go;
    }
}