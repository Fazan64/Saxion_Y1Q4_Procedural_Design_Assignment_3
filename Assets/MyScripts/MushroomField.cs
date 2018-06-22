using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using DG.Tweening;
using Random = UnityEngine.Random;

[RequireComponent(typeof(RandomSpawner))]
public class MushroomField : MonoBehaviour
{
    [SerializeField] float mutationRate = 0.05f;
    [SerializeField] float spawnInterval = 1f;

    private RandomSpawner spawner;
    private readonly List<ProceduralMushroom> mushrooms = new List<ProceduralMushroom>();
        
    void Start()
    {
        spawner = GetComponent<RandomSpawner>();

        while (MakeMushroom(useGenetic: false) != null) {}
        
        StartCoroutine(SpawnMushroomCoroutine());
    }

    void Update()
    {
        DestroyMushroomOnClick();
    }

    IEnumerator SpawnMushroomCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            MakeMushroom();
            
            mushrooms.RemoveAll(m => !m);
            if (mushrooms.Count > 2)
            {
                var mushroom = mushrooms.Aggregate((currentMin, m) => (currentMin == null || (m != null && m.GetGenes()[0] < currentMin.GetGenes()[0])) ? m : currentMin);
                Destroy(mushroom.gameObject);
            }
        }
    }

    private ProceduralMushroom MakeMushroom(bool useGenetic = true)
    {
        var go = spawner.Make();
        if (go == null) return null;

        var mushroom = go.GetComponent<ProceduralMushroom>();
        Assert.IsNotNull(mushroom);
       
        SetParameters(mushroom, useGenetic);
        mushroom.transform.DOScale(Vector3.zero, 0.5f).From().SetEase(Ease.InExpo);
        
        mushrooms.Add(mushroom);

        return mushroom;
    }

    private void SetParameters(ProceduralMushroom mushroom, bool useGenetic = true)
    {
        mushrooms.RemoveAll(m => !m);

        if (!useGenetic || mushrooms.Count < 2)
        {
            mushroom.Randomize();
            return;
        }

        // TODO Avoid possible duplicates
        ProceduralMushroom parentA = mushrooms[Random.Range(0, mushrooms.Count)];
        ProceduralMushroom parentB = mushrooms[Random.Range(0, mushrooms.Count)];

        float[] genes = Crossover(
            parentA.GetGenes(),
            parentB.GetGenes()
        );
        Mutate(genes, mutationRate);
        mushroom.SetGenes(genes);
    }

    private void DestroyMushroomOnClick()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        
        Transform cameraTransform = Camera.main.transform;
        var ray = new Ray(cameraTransform.position, cameraTransform.forward.normalized);
        RaycastHit hit;
        if (!Physics.SphereCast(ray, 0.5f, out hit)) return;

        GameObject collidee = hit.collider.gameObject;
        var mushroom = collidee.GetComponent<ProceduralMushroom>();
        if (mushroom == null) return;
        
        Destroy(collidee);
    }

    private static T[] Crossover<T>(T[] parentA, T[] parentB)
    {
        Assert.AreEqual(parentA.Length, parentB.Length);

        T[] child = new T[parentA.Length];

        int crossoverIndex = Random.Range(0, parentA.Length + 1);
        Array.Copy(parentA, 0             , child, 0             , crossoverIndex               );
        Array.Copy(parentB, crossoverIndex, child, crossoverIndex, child.Length - crossoverIndex);

        return child;
    }

    private static void Mutate(float[] genes, float mutationRate)
    {
        for (int i = 0; i < genes.Length; i++)
        {
            if (Random.value < mutationRate)
            {
                genes[i] *= Random.Range(0.9f, 1.1f);
            }
        }
    }
}