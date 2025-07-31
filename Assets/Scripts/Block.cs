using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VisualScripting;

public class Block{
    public Face originalFace;
    
    public List<Vector2> insetPointsVec2 = new List<Vector2>();
    public List<Vector3> insetPoints = new List<Vector3>();

    public GameObject BuildingObject;
    public GameObject RoadObject;

    public Block(Face face){
        originalFace = face;
    }

    public void GenerateInset(){
        List<Vector2> polygon = new List<Vector2>();
        foreach (var node in originalFace.nodes) {
            polygon.Add(new Vector2(node.pos.x, node.pos.z));
        }

        List<Vector2> newPolygon = PolyUtil.InsetPolygon(polygon, 3f);

        foreach (var point in newPolygon) {
            insetPoints.Add(new Vector3(point.x, 0, point.y));
            insetPointsVec2.Add(point);
        }
    }

    private Vector2 GenerateNormal(List<Vector2> polygon)
    {
        // Return a default if the polygon is just a line or a point.
        if (polygon == null || polygon.Count < 2)
        {
            return Vector2.up; // Default perpendicular cut direction
        }

        float maxSqrDistance = 0f;
        Vector2 point1 = Vector2.zero;
        Vector2 point2 = Vector2.zero;

        // Find the two vertices that are farthest apart.
        for (int i = 0; i < polygon.Count; i++)
        {
            for (int j = i + 1; j < polygon.Count; j++)
            {
                float sqrDist = (polygon[j] - polygon[i]).sqrMagnitude;
                if (sqrDist > maxSqrDistance)
                {
                    maxSqrDistance = sqrDist;
                    point1 = polygon[i];
                    point2 = polygon[j];
                }
            }
        }

        // This vector represents the longest axis.
        Vector2 longestAxis = point2 - point1;


        // Return the normalized perpendicular direction.
        return longestAxis.normalized;
    }
    
    private void SplitBlock(GameObject buildingObject, Material blockMaterial){
        List<List<Vector2>> polygons = new List<List<Vector2>>();
        polygons.Add(insetPointsVec2);
        
        
        for (int i = 0; i < 3; i++) {
            List<List<Vector2>> newPolygons = new List<List<Vector2>>();
            foreach (var polygon in polygons) {
                Vector2 normal = GenerateNormal(polygon);
                newPolygons.Add(PolyUtil.CutPolygon(polygon, PolyUtil.GetCenterAverage(polygon), normal));
                newPolygons.Add(PolyUtil.CutPolygon(polygon, PolyUtil.GetCenterAverage(polygon), -normal));
            }
        
            polygons = newPolygons;
        }
        
        foreach (var polygon in polygons) {
            GameObject obj = new GameObject();
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            meshFilter.mesh = PolyUtil.GeneratePrismMesh(polygon, Random.value * 15f);
            obj.GetComponent<MeshRenderer>().material = blockMaterial;
            
            obj.transform.parent = buildingObject.transform;
        
        }
        
    }
    
    public GameObject GenerateBlock(Material blockMaterial){
        GameObject meshObject = new GameObject();
        
        SplitBlock(meshObject, blockMaterial);
        
        BuildingObject = meshObject;
        return BuildingObject;
    }

    public GameObject GenerateRoad(Material roadMaterial){
        GameObject meshObject = new GameObject();
        meshObject.AddComponent<MeshFilter>();
        meshObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
        
        List<Vector3> originalPoints = new List<Vector3>();
        foreach (var node in originalFace.nodes) {
            originalPoints.Add(node.pos);
        }

        meshFilter.mesh = PolyUtil.GenerateRoad(originalPoints, insetPoints);
        meshObject.GetComponent<MeshRenderer>().material = roadMaterial;
        RoadObject = meshObject;
        return RoadObject;
    }

    public GameObject GetBuildingObject(){
        return BuildingObject;
    }

    public GameObject GetRoadObject(){
        return RoadObject;
    }
    
    public void DrawInset(){
        for (int i = 0; i < insetPoints.Count - 1; i++) {
            Debug.DrawLine(insetPoints[i], insetPoints[i+1], Color.green, 99f);
        }
        Debug.DrawLine(insetPoints[^1], insetPoints[0], Color.green, 99f);

    }
}