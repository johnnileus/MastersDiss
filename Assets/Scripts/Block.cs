using UnityEngine;

using System.Collections.Generic;

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
    
    public GameObject GenerateStructure(){
        GameObject meshObject = new GameObject();
        meshObject.AddComponent<MeshFilter>();
        meshObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;

        mesh.vertices = insetPoints.ToArray();
        mesh.triangles = new int[] {
            0, 1, 2,
            0, 2, 3
        };
        
        mesh.RecalculateNormals();
        meshObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        return meshObject;
    }
    

    public void DrawInset(){
        for (int i = 0; i < insetPoints.Count - 1; i++) {
            Debug.DrawLine(insetPoints[i], insetPoints[i+1], Color.green, 99f);
        }
        Debug.DrawLine(insetPoints[^1], insetPoints[0], Color.green, 99f);

    }
}