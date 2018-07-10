using System;
using System.CodeDom.Compiler;
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
    [SerializeField] bool automaticSelection;

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

            if (automaticSelection)
            {
                DestroyLeastFitMushroom();
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

    private void DestroyLeastFitMushroom()
    {
        mushrooms.RemoveAll(m => !m);
        if (mushrooms.Count <= 2) return;

        Func<ProceduralMushroom, float> fitnessFunction = m =>
        {
            float[] genes = m.GetGenes();

            return -(Mathf.Abs(genes[5]) + Mathf.Abs(genes[6]) + Mathf.Abs(genes[7]));
        };
        
        var mushroom = mushrooms.ArgMin(fitnessFunction);
        Destroy(mushroom.gameObject);
    }
    
    private void SetParameters(ProceduralMushroom mushroom, bool useGenetic = true)
    {
        mushrooms.RemoveAll(m => !m);

        if (!useGenetic || mushrooms.Count < 2)
        {
            mushroom.Randomize();
            return;
        }

        List<ProceduralMushroom> candidates = mushrooms.ToList();

        int parentAIndex = Random.Range(0, candidates.Count);
        ProceduralMushroom parentA = candidates[parentAIndex];
        candidates.RemoveAt(parentAIndex);

        int parentBIndex = Random.Range(0, candidates.Count);
        ProceduralMushroom parentB = candidates[parentBIndex];
        
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

        mushrooms.Remove(mushroom);
       
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
                genes[i] *= Random.Range(0.5f, 1.5f);
            }
        }
    }
}