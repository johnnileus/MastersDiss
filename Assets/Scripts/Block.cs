using UnityEngine;

using System.Collections.Generic;
using System.Linq;

public class Block{
    public Face originalFace;

    public Mesh structureMesh = new Mesh();
    public List<Vector3> insetPoints = new List<Vector3>();

    public Block(Face face){
        originalFace = face;
    }

    public void GenerateInset(){
        List<Vector2> polygon = new List<Vector2>();
        foreach (var node in originalFace.nodes) {
            polygon.Add(new Vector2(node.pos.x, node.pos.z));
        }

        List<Vector2> newPolygon = PolygonUtility.InsetPolygon(polygon, 4f);

        foreach (var point in newPolygon) {
            insetPoints.Add(new Vector3(point.x, 0, point.y));
        }

    }
    
    public GameObject GenerateBlock(Material blockMaterial){
        GameObject meshObject = new GameObject();
        meshObject.AddComponent<MeshFilter>();
        meshObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();

        float height = (Random.value + 1f) * 3f; 
        
        meshFilter.mesh = PolygonUtility.GeneratePrismMesh(insetPoints, height);
        meshObject.GetComponent<MeshRenderer>().material = blockMaterial;
        
        return meshObject;
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

        meshFilter.mesh = PolygonUtility.GenerateRoad(originalPoints, insetPoints);
        meshObject.GetComponent<MeshRenderer>().material = roadMaterial;

        return meshObject;
    }
    

    public void DrawInset(){
        for (int i = 0; i < insetPoints.Count - 1; i++) {
            Debug.DrawLine(insetPoints[i], insetPoints[i+1], Color.green, 99f);
        }
        Debug.DrawLine(insetPoints[^1], insetPoints[0], Color.green, 99f);

    }
}