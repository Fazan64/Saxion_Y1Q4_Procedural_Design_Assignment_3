using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Handout
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class Extrude : MonoBehaviour
    {
        [SerializeField] float height = 1;

        [SerializeField] Vector2[] Polygon =
        {
            new Vector2(-1, -1),
            new Vector2(0, 1),
            new Vector2(1, -1)
        };

        bool isDirty = true;

        void OnValidate()
        {
            isDirty = true;
        }

        void Update()
        {
            if (!isDirty) return;
            
            UpdateMesh();
            isDirty = false;
        }

        void UpdateMesh()
        {
            // Copy the inspector array to a list that's going to be modified:
            List<Vector2> polygon = new List<Vector2>(Polygon);
            // Create a list of indices 0..n-1:
            List<int> indices = new List<int>(polygon.Count);
            for (int i = 0; i < polygon.Count; i++)
            {
                indices.Add(i);
            }

            // This list is going to contain the vertex indices of the triangles: (3 integers per triangle)
            List<int> triangles = new List<int>();

            // Compute the triangulation of [polygon], store it in [triangles]:
            TriangulatePolygon(triangles, polygon, indices);

            MeshBuilder builder = new MeshBuilder();

            int n = Polygon.Length;
            // Add front face:
            for (int i = 0; i < n; i++)
            {
                builder.AddVertex(new Vector3(Polygon[i].x, Polygon[i].y, 0));
            }

            for (int t = 0; t < triangles.Count; t += 3)
            {
                builder.AddTriangle(triangles[t], triangles[t + 1], triangles[t + 2]);
                //Debug.Log ("Adding triangle " + triangles [t] + "," + triangles [t + 1] + "," + triangles [t + 2]);
            }

            // Add back face:
            for (int i = 0; i < n; i++)
            {
                builder.AddVertex(new Vector3(Polygon[i].x, Polygon[i].y, height));
            }

            for (int t = 0; t < triangles.Count; t += 3)
            {
                builder.AddTriangle(n + triangles[t + 2], n + triangles[t + 1], n + triangles[t]);
            }

            // Add sides:
            for (int i = 0; i < Polygon.Length; i++)
            {
                int j = (i + 1) % Polygon.Length; // the next vertex index
                // front vertices:
                int v1 = builder.AddVertex(new Vector3(Polygon[i].x, Polygon[i].y, 0));
                int v2 = builder.AddVertex(new Vector3(Polygon[j].x, Polygon[j].y, 0));
                // back vertices:
                int v3 = builder.AddVertex(new Vector3(Polygon[i].x, Polygon[i].y, height));
                int v4 = builder.AddVertex(new Vector3(Polygon[j].x, Polygon[j].y, height));
                // Add quad:
                builder.AddTriangle(v1, v3, v2);
                builder.AddTriangle(v2, v3, v4);
            }

            GetComponent<MeshFilter>().mesh = builder.CreateMesh();
        }

        // *IF* [polygon] respresents a simple polygon (no crossing edges), given in clockwise order, then 
        // this method will return in [triangles] a triangulation of the polygon, using the vertex indices from [indices]
        // If the assumption is not satisfied, the output is undefined or an exception is thrown.
        // TODO: fix the code such that it only adds correct triangles (that are *inside* the polygon)
        void TriangulatePolygon(List<int> triangles, List<Vector2> points, List<int> indices)
        {
            if (points.Count < 2)
            {
                throw new Exception("Cannot triangulate polygons with less than 3 vertices");
            }

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
                triangles.Add(indices[i]);
                triangles.Add(indices[i2]);
                triangles.Add(indices[i3]);
            
                points. RemoveAt(i2);
                indices.RemoveAt(i2);
                if (points.Count < 3) return;
            
                // continue with a smaller polygon, so restart the for loop:
                i = -1;
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
        private bool InsideTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector3 testPoint)
        {
            if (Clockwise(p1, p2, p3))
                return !Clockwise(p2, p1, testPoint) && !Clockwise(p3, p2, testPoint) && !Clockwise(p1, p3, testPoint);
            else
                return !Clockwise(p1, p2, testPoint) && !Clockwise(p2, p3, testPoint) && !Clockwise(p3, p1, testPoint);
        }
    }
}