using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class PolyUtil{

    public static Mesh GeneratePrismMesh(List<Vector2> points, float height){
        List<Vector3> vec3Points = new List<Vector3>();
        foreach (var point in points) {
            vec3Points.Add(PolyUtil.ToVec3(point));
        }

        return GeneratePrismMesh(vec3Points, height);
    }

    public static Mesh GeneratePrismMesh(List<Vector3> points, float height){
        Mesh mesh = new Mesh();
        height = 0f;
        int pointCount = points.Count;

        float temp = Mathf.PerlinNoise(points[0].x / 300.01f, points[0].z / 300.01f);
        Vector3 offset = new Vector3(0, Mathf.Pow(temp * 2, 4) * 100f + Random.value * 5f, 0);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 p0 = points[i];
            Vector3 p1 = points[(i + 1) % pointCount];
            Vector3 p2 = p1 + Vector3.up * height + offset;
            Vector3 p3 = p0 + Vector3.up * height + offset;

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
            
            vertices.Add(points[i] + Vector3.up * height + offset);
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
    
    public static Vector2 GetCenterAverage(List<Vector2> points){
        if (points == null || points.Count == 0) {
            return Vector2.zero;
        }

        Vector2 sum = Vector2.zero;
        foreach (var p in points) {
            sum += p;
        }
        return sum / points.Count;
    }
    
    public static List<Vector2> CutPolygon(List<Vector2> polygon, Vector2 point, Vector2 normal){

        Vector2 start = polygon[^1];

        List<Vector2> clippedPolygon = new List<Vector2>();

        foreach (var end in polygon) {
            bool startIsInside = PolyUtil.IsInside(start, point, normal);
            bool endIsInside = PolyUtil.IsInside(end, point, normal);

            if (endIsInside) {
                if (!startIsInside) {
                    clippedPolygon.Add(PolyUtil.GetIntersection(start, end, point, normal));
                }
                clippedPolygon.Add(end);
            }
            else if (startIsInside) {
                clippedPolygon.Add( PolyUtil.GetIntersection(start, end, point, normal));
            }
            start = end;
        }

        return clippedPolygon;
    }
    
    //generates boundary for a point, given a list of other points
    public static List<Vector2> CalculateVoronoiCell(Vector2 centerPoint, List<Vector2> neighborPoints, List<Vector2> bounds){
        List<Vector2> subjectPolygon = new List<Vector2>(bounds);

        foreach (var neighbor in neighborPoints) {
            Vector2 midPoint = (centerPoint + neighbor) / 2f;
            Vector2 normal = (centerPoint - neighbor).normalized;

            if (subjectPolygon.Count == 0) continue;
            
            subjectPolygon = CutPolygon(subjectPolygon, midPoint, normal);

        }
        return subjectPolygon;
    }
    
    public static Mesh GenerateRoad(List<Vector3> original, List<Vector3> inset){
        Mesh mesh = new Mesh();
        int pointCount = original.Count;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < pointCount; i++) {
            vertices.Add(original[i]);
            vertices.Add(inset[i]);
            if (i == pointCount - 1) {
                triangles.AddRange(new[] {2*i, 0, 1});
                triangles.AddRange(new[] {2*i, 1, 2*i + 1});
            }
            else {
                triangles.AddRange(new[] {2*i, 2*(i+1), 2*(i+1)+1});
                triangles.AddRange(new[] {2*i, 2*(i+1)+1, 2*i+1});
            }
            
        }
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
    
    public static bool IsInside(Vector2 p, Vector2 linePoint, Vector2 normal){
        return Vector2.Dot(p - linePoint, normal) >= 0;
    }


    public static bool IsPointInPolygon(Vector2 point, List<Vector2> polygonPoints, Vector2 centerPoint)
    {
        if (polygonPoints == null || polygonPoints.Count < 3)
        {
            return false;
        }

        for (int i = 0; i < polygonPoints.Count; i++)
        {
            Vector2 p1 = polygonPoints[i];
            Vector2 p2 = polygonPoints[(i + 1) % polygonPoints.Count];

            Vector2 edge = p2 - p1;

            Vector2 normal = new Vector2(-edge.y, edge.x);

            Vector2 vectorToCenter = centerPoint - p1;

            if (Vector2.Dot(vectorToCenter, normal) < 0) {
                normal = -normal;
            }

            if (!IsInside(point, p1, normal)) {
                return false;
            }
        }

        return true;
    }

    private static Vector2 GetIntersection(Vector2 p1, Vector2 p2, Vector2 linePoint, Vector2 normal){
        
        Vector2 lineVec = p2 - p1;
        float dotNumerator = Vector2.Dot(linePoint - p1, normal);
        float dotDenominator = Vector2.Dot(lineVec, normal);

        if (Mathf.Approximately(dotDenominator, 0f)) {
            return p1;
        }

        float t = dotNumerator / dotDenominator;
        return p1 + lineVec * t;
    }

    public static Vector3 ToVec3(Vector2 inp){
        return new Vector3(inp.x, 0,inp.y);
    }
    
}
    
    
