using System.Collections.Generic;
using UnityEngine;

public class LatheMeshBuilder
{
    private readonly MeshBuilder meshBuilder;
    private readonly int numSplines;

    private int numExistingSegments;
    private Matrix4x4 previousRotated;

    public LatheMeshBuilder(int numSplines)
    {
        this.numSplines = numSplines;

        meshBuilder = new MeshBuilder();
        previousRotated = Matrix4x4.identity;
    }

    public Mesh CreateMesh() => meshBuilder.CreateMesh(shouldRecalculateNormals: false);
    public void Reset()      => meshBuilder.Reset();
    
    //this method updates the mesh if needed
    public void Add(IList<Vector2> sideVertices, Vector3 rotationPerUnitHeightEuler = new Vector3())
    {
        Matrix4x4 segmentLocalToModelspace = Matrix4x4.identity;
        
        //go through all vertices (all vertices per spline)
        for (int segmentIndex = 0; segmentIndex < sideVertices.Count; ++segmentIndex)
        {
            Vector2 sideVertex = sideVertices[segmentIndex];
            
            segmentLocalToModelspace = previousRotated * GetTransformSegmentLocalToModel(sideVertex.y, rotationPerUnitHeightEuler);
            
            //go through all splines (vertical lines around mesh)
            for (int splineIndex = 0; splineIndex <= numSplines; ++splineIndex)
            {
                float rotationProgress = splineIndex / (float)numSplines;
                var vertexRotationAroundCenter = Quaternion.Euler(0, -360f * rotationProgress, 0);
                var transformRotateVertexAroundCenter = Matrix4x4.Rotate(vertexRotationAroundCenter);

                Vector3 rotatedVertex = transformRotateVertexAroundCenter.MultiplyPoint(sideVertex);
                                
                Vector3 vertex = segmentLocalToModelspace.MultiplyPoint(rotatedVertex);
                Vector3 uv = new Vector2(rotationProgress, rotatedVertex.y);

                Vector3 normal;
                if (segmentIndex - 1 <= 0)
                {
                    normal = Vector2.Perpendicular(sideVertex - sideVertices[segmentIndex + 1]);
                }
                else if (segmentIndex + 1 >= sideVertices.Count)
                {
                    normal = Vector2.Perpendicular(sideVertices[segmentIndex - 1] - sideVertex);
                }
                else
                {
                    normal = 0.5f * (
                        Vector2.Perpendicular(sideVertex - sideVertices[segmentIndex + 1]) +
                        Vector2.Perpendicular(sideVertices[segmentIndex - 1] - sideVertex)
                    );
                }
                
                normal = (segmentLocalToModelspace * transformRotateVertexAroundCenter).MultiplyVector(normal); 
                
                //add it to the mesh
                meshBuilder.AddVertex(vertex, uv, normal);
            }
        }
                
        if (numExistingSegments > 0) AddTrianglesRing(numExistingSegments - 1);
        for (int vertexIndex = numExistingSegments; vertexIndex <= numExistingSegments + sideVertices.Count - 2; ++vertexIndex)
        {
            AddTrianglesRing(vertexIndex);
        }
        
        previousRotated = segmentLocalToModelspace;
        numExistingSegments += sideVertices.Count;
    }

    private Matrix4x4 GetTransformSegmentLocalToModel(float segmentY, Vector3 rotationPerUnitHeightEuler)
    {
        Vector3 segmentLocalCenter = Vector3.up * segmentY;
            
        // TODO Calculate the right orientation for a point along a spiral.
        var segmentRotation = Quaternion.Euler(rotationPerUnitHeightEuler * segmentY); 
        var rotationRelativeToOrigin = Quaternion.Euler(rotationPerUnitHeightEuler * segmentY);

        return
            Matrix4x4.Translate(Matrix4x4.Rotate(rotationRelativeToOrigin).MultiplyPoint(segmentLocalCenter)) *
            Matrix4x4.Rotate(segmentRotation) * 
            Matrix4x4.Translate(-segmentLocalCenter);
    }

    private void AddTrianglesRing(int vertexIndex, int offset = 0)
    {
        for (int splineIndex = 0; splineIndex < numSplines; ++splineIndex)
        {
            meshBuilder.AddQuad(
                GetIndex(splineIndex    , vertexIndex    , offset),
                GetIndex(splineIndex    , vertexIndex + 1, offset),
                GetIndex(splineIndex + 1, vertexIndex    , offset),
                GetIndex(splineIndex + 1, vertexIndex + 1, offset)
            );
        }
    }
    
    //helper function to map the x,y location of a vertex to an index in a 1D array
    private int GetIndex(int splineIndex, int vertexIndex, int offset = 0)
    {
        return offset + splineIndex + vertexIndex * (numSplines + 1);
    }
}