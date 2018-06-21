using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GridCell : MonoBehaviour
{
    private RandomSpawner spawner;
    private int cellIndex;

    private bool isInitialized;

    public void Initialize(RandomSpawner spawner, int cellIndex)
    {
        Assert.IsFalse(isInitialized, "The grid cell is already initialized.");
        
        this.spawner   = spawner;
        this.cellIndex = cellIndex;

        isInitialized = true;
    }
    
    private void OnDestroy()
    {        
        Assert.IsTrue(isInitialized, this + "has not been initialized.");

        if (!spawner) return;
        spawner.FreeCell(cellIndex);
    }
}