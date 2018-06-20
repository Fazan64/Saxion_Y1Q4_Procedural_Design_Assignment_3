using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Handout
{
    enum CombinationType
    {
        Branch,
        Alternate
    };

    enum StackType
    {
        Shrink,
        Rotate,
        Tree,
        Custom
    }

    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class CreateCubes : MonoBehaviour
    {
        [SerializeField] bool AlwaysUpdate = true;
        [SerializeField] bool UseSceneTransforms = true;
        [SerializeField] CombinationType combinationType;
        [SerializeField] StackType stackType;
        [SerializeField] int numberOfSteps = 5;
        [SerializeField] Transform[] inputTransform;

        bool _dirty = true;

        void OnValidate()
        {
            _dirty = true;
        }

        void Update()
        {
            if (_dirty || AlwaysUpdate)
            {
                UpdateMesh();
                _dirty = false;
            }
        }

        void UpdateMesh(bool useGameObjects = false)
        {
            MeshBuilder builder = new MeshBuilder();

            List<Matrix4x4> transformationSteps = new List<Matrix4x4>();
            if (UseSceneTransforms)
            {
                foreach (Transform trans in inputTransform)
                {
                    transformationSteps.Add(trans.localToWorldMatrix);
                }
            }
            else
            {
                // default values:
                Vector3 translation = new Vector3(0, 1, 0);
                Quaternion rotation = Quaternion.identity;
                Vector3 scale = new Vector3(1, 1, 1);

                switch (stackType)
                {
                    case StackType.Shrink:
                        // A stack, decreasing scale:
                        scale *= 0.9f;
                        translation = new Vector3(0, 0.95f, 0);
                        transformationSteps.Add(Matrix4x4.TRS(translation, rotation, scale));
                        break;
                    case StackType.Rotate:
                        // A stack, rotating around the y-axis:
                        rotation = Quaternion.Euler(0, 10, 0);
                        transformationSteps.Add(Matrix4x4.TRS(translation, rotation, scale));
                        break;
                    case StackType.Tree:
                        // A tree - different translations:
                        scale *= 0.5f;
                        translation = new Vector3(-0.5f, 0.75f, 0);
                        transformationSteps.Add(Matrix4x4.TRS(translation, rotation, scale));
                        translation = new Vector3(0.5f, 0.75f, 0);
                        transformationSteps.Add(Matrix4x4.TRS(translation, rotation, scale));
                        break;
                    case StackType.Custom:
                        // Add your own recipe here
                        transformationSteps.Add(Matrix4x4.TRS(translation, rotation, scale));
                        break;
                }
            }

            if (combinationType == CombinationType.Branch)
            {
                AddCubeTree(builder, Matrix4x4.identity, transformationSteps, numberOfSteps);
            }
            else
            {
                AddCubeSeries(builder, Matrix4x4.identity, transformationSteps, numberOfSteps);
            }

            GetComponent<MeshFilter>().mesh = builder.CreateMesh();
        }

        void AddCubeSeries(MeshBuilder builder, Matrix4x4 currentTransformation,
            List<Matrix4x4> nextTransformationSteps, int steps)
        {
            for (int currentStep = 0; currentStep < steps; currentStep++)
            {
                AddCube(builder, currentTransformation);
                currentTransformation = currentTransformation *
                                        nextTransformationSteps[currentStep % nextTransformationSteps.Count];
            }
        }

        void AddCubeTree(MeshBuilder builder, Matrix4x4 currentTransformation, List<Matrix4x4> nextTransformationSteps,
            int steps)
        {
            AddCube(builder, currentTransformation);
            if (steps == 0) return;
            
            foreach (Matrix4x4 transf in nextTransformationSteps)
            {
                AddCubeTree(builder, currentTransformation * transf, nextTransformationSteps, steps - 1);
            }
        }

        // A bad AddCube method (Bad normals, no UVs, no parameters) - feel free to improve this for Assignment 2!
        // Adds a cube with side lengths 1 to the given MeshBuilder, with the position / rotation / scale given by [transformation]
        void AddCube(MeshBuilder builder, Matrix4x4 transformation)
        {
            int v1 = builder.AddVertex(transformation.MultiplyPoint(new Vector3( 1,  1,  1) * 0.5f));
            int v2 = builder.AddVertex(transformation.MultiplyPoint(new Vector3(-1,  1,  1) * 0.5f));
            int v3 = builder.AddVertex(transformation.MultiplyPoint(new Vector3( 1, -1,  1) * 0.5f));
            int v4 = builder.AddVertex(transformation.MultiplyPoint(new Vector3(-1, -1,  1) * 0.5f));
            int v5 = builder.AddVertex(transformation.MultiplyPoint(new Vector3( 1,  1, -1) * 0.5f));
            int v6 = builder.AddVertex(transformation.MultiplyPoint(new Vector3(-1,  1, -1) * 0.5f));
            int v7 = builder.AddVertex(transformation.MultiplyPoint(new Vector3( 1, -1, -1) * 0.5f));
            int v8 = builder.AddVertex(transformation.MultiplyPoint(new Vector3(-1, -1, -1) * 0.5f));

            builder.AddTriangle(v1, v2, v3);
            builder.AddTriangle(v2, v4, v3);
            builder.AddTriangle(v1, v3, v5);
            builder.AddTriangle(v3, v7, v5);
            builder.AddTriangle(v1, v5, v2);
            builder.AddTriangle(v2, v5, v6);

            builder.AddTriangle(v5, v7, v6);
            builder.AddTriangle(v6, v7, v8);
            builder.AddTriangle(v2, v6, v4);
            builder.AddTriangle(v4, v6, v8);
            builder.AddTriangle(v3, v4, v7);
            builder.AddTriangle(v4, v8, v7);
        }
    }
}