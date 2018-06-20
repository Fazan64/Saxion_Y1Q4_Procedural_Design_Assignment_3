using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

public class MeshBuilder
{
    private readonly List<Vector3> vertices;
    private readonly List<Vector2> uvs;
    private readonly List<int> triangles;

    public bool isDoubleSided { get; set; }
    public Vector3 offset     { get; set; }

    public Vector2 textureSizeInUnits { get; set; }
    public Vector3 automaticUvOrigin  { get; set; }
    public Vector2 automaticUvOffset  { get; set; }
    public UvRange? automaticUvRange  { get; set; }
    
    public bool triangleSubdivisionEnabled { get; set; }

    public int vertexCount => vertices.Count;

    private RangeFloat uvRangeY => automaticUvRange?.vertical ?? new RangeFloat(0f, 1f);

    /// <summary>
    /// Initializes a new instance of the <see cref="MeshBuilder"/> class.
    /// </summary>
    public MeshBuilder()
    {
        vertices  = new List<Vector3>();
        uvs       = new List<Vector2>();
        triangles = new List<int>();
        
        textureSizeInUnits = Vector2.one;
    }

    public MeshBuilder(int estimateNumVertices)
    {
        vertices  = new List<Vector3>(capacity: estimateNumVertices);
        uvs       = new List<Vector2>(capacity: estimateNumVertices);
        triangles = new List<int>(capacity: estimateNumVertices * 3);
        
        textureSizeInUnits = Vector2.one;
    }

    /// <summary>
    /// Clear all internal lists. You can create a new mesh definition after this.
    /// </summary>
    public void Reset()
    {
        vertices.Clear();
        uvs.Clear();
        triangles.Clear();

        isDoubleSided = false;
        offset = Vector3.zero;
        textureSizeInUnits = Vector2.one;
        automaticUvOffset  = Vector2.zero;
        automaticUvOrigin  = Vector2.zero;
        automaticUvRange   = null;
        triangleSubdivisionEnabled = false;
    }
    
    /// <summary>
    /// Adds a vertex to the list, based on a Vector3f position and an optional Vector2f uv set
    /// </summary>
    /// <returns>The vertex index.</returns>
    public int AddVertex(Vector3 position, Vector2 uv = new Vector2())
    {
        int newVertexIndex = vertices.Count;

        vertices.Add(position + offset);

        uv.y = uvRangeY.Lerp(uv.y);
        uvs.Add(uv);

        return newVertexIndex;
    }

    public int Clone(int vertexIndex)
    {
        return AddVertex(vertices[vertexIndex], uvs[vertexIndex]);
    }

    /// <summary>
    /// Adds a triangle to the list, based on three vertex indices.
    /// </summary>
    /// <param name="v0">Vertex 1 index.</param>
    /// <param name="v1">Vertex 2 index.</param>
    /// <param name="v2">Vertex 3 index.</param>
    public void AddTriangle(int v0, int v1, int v2)
    {        
        AddTriangleInternal(v0, v1, v2);

        if (!isDoubleSided) return;
        
        AddTriangleInternal(Clone(v0), Clone(v2), Clone(v1));
    }

    /// <summary>
    /// Creates the mesh. Note: this will not reset any of the internal lists. (Use Clear to do that)
    /// </summary>
    public Mesh CreateMesh(bool shouldRecalculateNormals = true)
    {
        var mesh = new Mesh();
        ApplyToMesh(mesh);
        return mesh;
    }

    public Mesh ApplyToMesh(Mesh mesh, bool shouldRecalculateNormals = true)
    {        
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, submesh: 0);
        
        if (shouldRecalculateNormals) mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    #region Helpers

    public void AddTriangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    { 
        Vector3 normal = GetNormal(vertex1, vertex2, vertex3);
        Vector3 projectedOrigin = automaticUvOrigin - Vector3.Project(automaticUvOrigin - vertex1, normal);
        
        var rotateGlobalToLocal = GetGlobalToFaceLocalRotation(vertex1, vertex2, vertex3, normal);
        Vector3 vertex1Local = rotateGlobalToLocal * (vertex1 - projectedOrigin);
        Vector3 vertex2Local = rotateGlobalToLocal * (vertex2 - projectedOrigin);
        Vector3 vertex3Local = rotateGlobalToLocal * (vertex3 - projectedOrigin);
        
        Vector2 scale = new Vector2(1f / textureSizeInUnits.x, 1f / textureSizeInUnits.y);
        Vector2 uv1 = automaticUvOffset + Vector2.Scale(vertex1Local, scale);
        Vector2 uv2 = automaticUvOffset + Vector2.Scale(vertex2Local, scale);
        Vector2 uv3 = automaticUvOffset + Vector2.Scale(vertex3Local, scale);
        
        AddTriangle(
            AddVertex(vertex1, uv1),
            AddVertex(vertex2, uv2),
            AddVertex(vertex3, uv3)
        );
    }
    
    public void AddTriangle(
        Vector3 vertex1, Vector2 uv1,
        Vector3 vertex2, Vector2 uv2,
        Vector3 vertex3, Vector2 uv3
    )
    {
        AddTriangle(
            AddVertex(vertex1, uv1),
            AddVertex(vertex2, uv2),
            AddVertex(vertex3, uv3)
        );
    }


    //  (10)    (11)
    //  2-------3 
    //  |       |    y
    //  |       |    |
    //  |       |    |
    //  0-------1    +--x 
    //  (00)    (01)
    public void AddQuad(int v00, int v01, int v10, int v11)
    {
        if (automaticUvRange.HasValue)
        {
            ShiftUvsTowardsOrigin(v00, v01, v10, v11);
        }
        
        AddTriangle(
            v00,
            v11,
            v10
        );

        AddTriangle(
            v00,
            v01,
            v11
        );
    }
    
    public void AddQuad(
        Vector3 vertex00, Vector2 uv00,
        Vector3 vertex01, Vector2 uv01,
        Vector3 vertex10, Vector2 uv10,
        Vector3 vertex11, Vector2 uv11
    )
    {
        AddQuad(
            AddVertex(vertex00, uv00),
            AddVertex(vertex01, uv01),
            AddVertex(vertex10, uv10),
            AddVertex(vertex11, uv11)
        );
    }

    public void AddQuad(Vector3 vertex00, Vector3 vertex01, Vector3 vertex10, Vector3 vertex11)
    {
        Vector3 normal = GetNormal(vertex00, vertex01, vertex10);
        Vector3 projectedOrigin = automaticUvOrigin - Vector3.Project(automaticUvOrigin - vertex00, normal);
        
        var rotateGlobalToLocal = GetGlobalToFaceLocalRotation(vertex00, vertex01, vertex10, normal);
        Vector3 vertex1Local = rotateGlobalToLocal * (vertex00 - projectedOrigin);
        Vector3 vertex2Local = rotateGlobalToLocal * (vertex01 - projectedOrigin);
        Vector3 vertex3Local = rotateGlobalToLocal * (vertex10 - projectedOrigin);
        Vector3 vertex4Local = rotateGlobalToLocal * (vertex11 - projectedOrigin);

        Vector2 scale = new Vector2(1f / textureSizeInUnits.x, 1f / textureSizeInUnits.y);
        Vector2 uv00 = automaticUvOffset + Vector2.Scale(vertex1Local, scale);
        Vector2 uv01 = automaticUvOffset + Vector2.Scale(vertex2Local, scale);
        Vector2 uv10 = automaticUvOffset + Vector2.Scale(vertex3Local, scale);
        Vector2 uv11 = automaticUvOffset + Vector2.Scale(vertex4Local, scale);
        
        AddQuad(
            vertex00, uv00,
            vertex01, uv01,
            vertex10, uv10,
            vertex11, uv11
        );
    }
    //   (011)   (111)
    //       3-------7
    // (010)/| (110)/|
    //     2-+-----6 | 
    //     | |     | |       y
    //     | 1-----+-5       | z
    //     |/(001) |/(101)   |/
    //     0-------4         +--x 
    //     (000)   (100)
    /// Vertex order taken from here: http://poita.org/2014/04/27/cube-vertex-numbering.html
    public void AddCuboid(params Vector3[] vertexPositions)
    {
        Assert.AreEqual(8, vertexPositions.Length, "AddCuboid takes 8 vertices.");

        var pos = vertexPositions;
        AddQuad(pos[0], pos[4], pos[2], pos[6]);
        AddQuad(pos[1], pos[0], pos[3], pos[2]);
        AddQuad(pos[4], pos[5], pos[6], pos[7]);
        AddQuad(pos[1], pos[5], pos[7], pos[3]);
        AddQuad(pos[2], pos[6], pos[3], pos[7]);
        AddQuad(pos[1], pos[5], pos[0], pos[4]);
    }

    #endregion
    
    private void ShiftUvsTowardsOrigin(params int[] vertexIndices)
    {
        if (!automaticUvRange.HasValue) return;
        
        // TODO If some are already in range, shift it to bring the other ones closer to it as much as possible without bringing any points out of the range.
       
        if (vertexIndices.All(vi => uvRangeY.Contains(uvs[vi].y, inclusiveMin: true))) return;

        int numTiles = automaticUvRange.Value.numRepeatsVertical;
        int tileIndex = numTiles / 2;
        float target = uvRangeY.Lerp(tileIndex / (float)numTiles);
        
        float range = uvRangeY.range / numTiles;
        
        float currentY = (vertexIndices.Max(vi => uvs[vi].y) + vertexIndices.Min(vi => uvs[vi].y)) * 0.5f;

        float shiftY = 0f;
        //float shiftY = Mathf.Floor((target - currentY) / range) * range;
        if (currentY < target)
        {
            shiftY = Mathf.Floor((target - currentY) / range) * range;
        }
        else if (currentY > target)
        {
            shiftY = -Mathf.Floor((currentY - target) / range) * range;
        }
        
        //Debug.Log(shiftY);
        
        var shift = new Vector2(0f, shiftY);
                
        foreach (int index in vertexIndices)
        {
            uvs[index] += shift;
        }
        
        //Assert.IsTrue(vertexIndices.Select(vi => uvs[vi].y).Any(y => uvRangeY.Contains(y, inclusiveMin: true)));
    }

    private void AddTriangleInternal(int v0, int v1, int v2)
    { 
        if (automaticUvRange.HasValue)
        {
            ShiftUvsTowardsOrigin(v0, v1, v2);
        }
                        
        triangles.Add(v0);
        triangles.Add(v1);
        triangles.Add(v2);
    }

    private static float Cross2D(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private void TriangulatePolygon(IList<Vector2> points, IList<int> indices)
    {
        Assert.IsTrue(points.Count >= 3, "Cannot triangulate polygons with less than 3 vertices");

        for (int i = 0; i < points.Count; i++)
        {
            int i2 = (i + 1) % points.Count;
            int i3 = (i + 2) % points.Count;
            Vector2 u = points[i];
            Vector2 v = points[i2];
            Vector2 w = points[i3];

            if (!Clockwise(u, v, w)) continue;
            
            bool anyInsideTriangle = Enumerable.Range(0, points.Count)
                .Where(j => j != i && j != i2 && j != i3)
                .Any(j => InsideTriangle(u, v, w, points[j]));
            if (anyInsideTriangle) continue;

            // Add a triangle on u,v,w:
            if (automaticUvRange.HasValue)
            {
                ShiftUvsTowardsOrigin(indices[i], indices[i2], indices[i3]);
            }
            AddTriangleInternal(indices[i], indices[i2], indices[i3]);
            
            points. RemoveAt(i2);
            indices.RemoveAt(i2);
            if (points.Count < 3) return;
            
            // continue with a smaller polygon, so restart the for loop:
            i--;
        }

        throw new Exception("No suitable triangulation found - is the polygon simple and clockwise?");
    }

    // Returns true if p1,p2 and p3 form a clockwise triangle (returns false if anticlockwise, or all three on the same line)
    private static bool Clockwise(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Vector2 difference1 = (p2 - p1);
        Vector2 difference2 = (p3 - p2);
        // Take the dot product of the (normal of difference1) and (difference2):
        return (-difference1.y * difference2.x + difference1.x * difference2.y) < 0;
    }

    // Returns true if [testPoint] lies inside, or on the boundary, of the triangle given by the points p1,p2 and p3.
    private static bool InsideTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 testPoint)
    {
        if (Clockwise(p1, p2, p3))
            return !Clockwise(p2, p1, testPoint) && !Clockwise(p3, p2, testPoint) && !Clockwise(p1, p3, testPoint);
        else
            return !Clockwise(p1, p2, testPoint) && !Clockwise(p2, p3, testPoint) && !Clockwise(p3, p1, testPoint);
    }

    private static bool OnTheSameLine(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return Mathf.Approximately(1f, Mathf.Abs(Vector3.Dot(v2 - v1, v3 - v1)));
    }
    
    private static float? IntersectionTestLineSegments(Vector2 startA, Vector2 deltaA, Vector2 startB, Vector2 deltaB, bool treatBAsLine = false)
    {        
        Vector2 aToB = startB - startA;
        float crossA = Cross2D(aToB, deltaA);
        float crossB = Cross2D(aToB, deltaB);
        float crossDelta = Cross2D(deltaA, deltaB);

        if (Mathf.Approximately(crossDelta, 0f))
        {
            if (!Mathf.Approximately(crossA, 0f)) return null;
            
            Debug.Log("Segments are collinear. We don't have a thing in place to check if they overlap or are disjoint yet.");
            return null;
        }
        
        float t = crossA / crossDelta;
        float u = crossB / crossDelta;

        if (t <= 0f || t >= 1f) return null;
        if (!treatBAsLine && (u <= 0f || u >= 1f)) return null;

        Assert.IsFalse(float.IsNaN(t));
        return t;
    }
    
    private static Quaternion GetGlobalToFaceLocalRotation(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 normal = new Vector3())
    {
        if (normal == Vector3.zero)
        {
            normal = GetNormal(vertex1, vertex2, vertex3);
        }

        Vector3 approximateUp = normal == Vector3.up ? Vector3.forward : Vector3.up;
        Vector3 up = Vector3.Cross(normal, Vector3.Cross(approximateUp, normal).normalized).normalized;
        return Quaternion.LookRotation(Vector3.back, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(normal, up));
    }

    private static Vector3 GetNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    {
        Vector3 normal = Vector3.Cross(vertex2 - vertex1, vertex3 - vertex1);
        normal.Normalize();
        return normal;
    }
}