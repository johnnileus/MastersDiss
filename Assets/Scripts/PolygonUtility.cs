using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PolygonUtility{


    public static Mesh GeneratePrismMesh(List<Vector3> points, float height)
    {
        Mesh mesh = new Mesh();
        int pointCount = points.Count;



        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 p0 = points[i];
            Vector3 p1 = points[(i + 1) % pointCount];
            Vector3 p2 = p1 + Vector3.up * height;
            Vector3 p3 = p0 + Vector3.up * height;

            int baseIndex = vertices.Count;
            vertices.AddRange(new[] { p0, p1, p2, p3 });

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }

        int topStartIndex = vertices.Count;
        for (int i = 0; i < pointCount; i++)
        {
            vertices.Add(points[i] + Vector3.up * height);
        }
        for (int i = 1; i < pointCount - 1; i++)
        {
            triangles.Add(topStartIndex);
            triangles.Add(topStartIndex + i);
            triangles.Add(topStartIndex + i + 1);
        }
        
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static Mesh GenerateRoad(List<Vector3> original, List<Vector3> inset){
        Mesh mesh = new Mesh();
        int pointCount = original.Count;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < pointCount; i++) {
            vertices.Add(original[i]);
            vertices.Add(inset[i]);
            Debug.Log($"{pointCount} {original.Count} {inset.Count}, {2*i} {2*(i+1)}, {2*(i+1)+1}");
            if (i == pointCount - 1) {
                triangles.AddRange(new[] {2*i, 0, 1});
                triangles.AddRange(new[] {2*i, 1, 2*i + 1});
            }
            else {
                triangles.AddRange(new[] {2*i, 2*(i+1), 2*(i+1)+1});
                triangles.AddRange(new[] {2*i, 2*(i+1)+1, 2*i+1});
            }
            
        }
        Debug.Log($"{vertices.Count}");
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    public static List<Vector2> InsetPolygon(List<Vector2> polygon, float insetDistance){
        int corners = polygon.Count;
        if (corners < 3) {
            return polygon;
        }

        var newPoints = new List<Vector2>(corners);

        for (int i = 0; i < corners; i++) {
            Vector2 prevPoint = polygon[(i + corners - 1) % corners];
            Vector2 currentPoint = polygon[i];
            Vector2 nextPoint = polygon[(i + 1) % corners];

            if (TryInsetCorner(prevPoint, currentPoint, nextPoint, insetDistance, out Vector2 insetPoint)) {
                newPoints.Add(insetPoint);
            }
            else {
                // If inset fails (e.g., for a 180-degree corner),
                // fall back to using the original point.
                newPoints.Add(currentPoint);
            }
        }
        
        return newPoints;
    }


    private static bool TryInsetCorner(Vector2 p1, Vector2 p2, Vector2 p3, float insetDistance, out Vector2 newP2){
        newP2 = p2;

        Vector2 v1 = p2 - p1;
        Vector2 v2 = p3 - p2;

        float dist1 = v1.magnitude;
        float dist2 = v2.magnitude;

        // Exit if either segment is zero-length.
        if (dist1 < 1e-6f || dist2 < 1e-6f) {
            return false;
        }

        Vector2 normal1 = new Vector2(v1.y, -v1.x).normalized;
        Vector2 normal2 = new Vector2(v2.y, -v2.x).normalized;

        Vector2 p1_inset = p1 + normal1 * insetDistance;
        Vector2 p2_inset1 = p2 + normal1 * insetDistance;
        Vector2 p2_inset2 = p2 + normal2 * insetDistance;
        Vector2 p3_inset = p3 + normal2 * insetDistance;

        return LineIntersection(p1_inset, p2_inset1, p2_inset2, p3_inset, out newP2);
    }

    private static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection){
        intersection = Vector2.zero;

        float bx = p2.x - p1.x;
        float by = p2.y - p1.y;
        float dx = p4.x - p3.x;
        float dy = p4.y - p3.y;

        float b_dot_d_perp = bx * dy - by * dx;

        // If b_dot_d_perp is zero, the lines are parallel and have no intersection.
        if (Mathf.Approximately(b_dot_d_perp, 0)) {
            return false;
        }

        float cx = p3.x - p1.x;
        float cy = p3.y - p1.y;

        float t = (cx * dy - cy * dx) / b_dot_d_perp;

        intersection = new Vector2(p1.x + t * bx, p1.y + t * by);
        return true;
    }
}
    
    
