using System.Collections.Generic;
using UnityEngine;

public class LatheMeshBuilder
{
    private readonly MeshBuilder meshBuilder;
    private readonly int numSplines;

    private Matrix4x4 previousRotated;

    public LatheMeshBuilder(int numSplines)
    {
        this.numSplines = numSplines;
        
        meshBuilder = new MeshBuilder();
        previousRotated = Matrix4x4.identity;
    }

    public Mesh CreateMesh() => meshBuilder.CreateMesh();
    public void Reset()      => meshBuilder.Reset();
    
    //this method updates the mesh if needed
    public void Add(IList<Vector2> sideVertices, Vector3 rotationPerUnitHeightEuler = new Vector3())
    {        
        int offset = meshBuilder.vertexCount;

        Matrix4x4 segmentLocalToModelspace = Matrix4x4.identity;
        
        //go through all vertices (all vertices per spline)
        for (int vertexIndex = 0; vertexIndex < sideVertices.Count; ++vertexIndex)
        {
            Vector3 sideVertex = sideVertices[vertexIndex];

            segmentLocalToModelspace = GetTransformSegmentLocalToModel(sideVertex.y, rotationPerUnitHeightEuler);
            
            //go through all splines (vertical lines around mesh)
            for (int splineIndex = 0; splineIndex < numSplines; ++splineIndex)
            {
                var vertexRotationAroundCenter = Quaternion.Euler(0, splineIndex * 360f / numSplines, 0);
                var transformRotateVertexAroundCenter = Matrix4x4.Rotate(vertexRotationAroundCenter);
                var vertex = (previousRotated * segmentLocalToModelspace * transformRotateVertexAroundCenter).MultiplyPoint(sideVertex);

                //add it to the mesh
                meshBuilder.AddVertex(vertex);
            }
        }

        previousRotated = segmentLocalToModelspace;
        
        //start at 1, because we need to access vertex at vertexIndex-1
        for (int vertexIndex = 1; vertexIndex < sideVertices.Count; ++vertexIndex)
        {                
            AddTrianglesRing(vertexIndex, offset);
        }
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
        for (int splineIndex = 1; splineIndex < numSplines; ++splineIndex)
        {
            meshBuilder.AddQuad(
                GetIndex(splineIndex - 1, vertexIndex - 1, offset),
                GetIndex(splineIndex    , vertexIndex - 1, offset),
                GetIndex(splineIndex - 1, vertexIndex    , offset),
                GetIndex(splineIndex    , vertexIndex    , offset)
            );
        }
            
        meshBuilder.AddQuad(
            GetIndex(numSplines - 1, vertexIndex - 1, offset),
            GetIndex(0             , vertexIndex - 1, offset),
            GetIndex(numSplines - 1, vertexIndex    , offset),
            GetIndex(0             , vertexIndex    , offset)
        );
    }
    
    //helper function to map the x,y location of a vertex to an index in a 1D array
    private int GetIndex(int splineIndex, int vertexIndex, int offset = 0)
    {
        return offset + splineIndex + vertexIndex * numSplines;
    }
}