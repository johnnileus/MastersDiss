using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using UnityEngine;
using TMPro;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;




public class RoadNode{
    public int ID;
    public Vector3 pos;
    public List<HalfEdge> edges = new List<HalfEdge>();

    public RoadNode(float x, float y){
        pos = new Vector3(x, 0, y);
    }
}

public class Face{
    public List<RoadNode> nodes = new List<RoadNode>();

    public Face(){
        
    }
}

public class HalfEdge{
    public RoadNode to;
    public RoadNode from;
    public HalfEdge next;

    public bool visited = false;

    public HalfEdge(RoadNode target, RoadNode source){
        to = target;
        from = source;
        next = null;
    }

    public void DrawEdge(float t = 999f){
        Debug.DrawLine(from.pos, to.pos, Color.gray, t);
        Debug.DrawLine(from.pos, from.pos + Vector3.up * 50f, Color.cyan, t);
        Debug.DrawLine(to.pos, to.pos + Vector3.up * 50f, Color.red, t);
    }
}

public class Chunk {


    public Vector2 chunkID;

    public Vector2 minPos;
    public Vector2 maxPos;
    public Vector2 center;
    public Vector3 vec3Center;
    public Vector3[] corners;
    public float nodeJitter;

    private float size;

    public List<RoadNode> nodes = new List<RoadNode>();
    public List<Face> faces = new List<Face>();

    public Chunk(int x, int y, float chunkSize, float jitter){
        size = chunkSize;
        nodeJitter = chunkSize * jitter; // convert 0 -> 1 to 0 -> distance to chunkSize;
        chunkID = new Vector2(x, y);

        minPos = new Vector2(x * chunkSize, y * chunkSize);
        maxPos = new Vector2((x + 1) * chunkSize, (y + 1) * chunkSize);
        center = minPos + new Vector2(chunkSize, chunkSize) / 2;
        vec3Center = new Vector3(center.x, 0, center.y);

        corners = new[] {
            new Vector3(minPos.x, 0, minPos.y),
            new Vector3(maxPos.x, 0, minPos.y),
            new Vector3(minPos.x, 0, maxPos.y),
            new Vector3(maxPos.x, 0, maxPos.y)
        };

    }

    private float GetChunkSeed(float x, float y){
        return x * 1619.5125f + y * 31337.65125f;
    }
    
    private float WrapAngleRadian(float ang){
        float output = ang % (2 * Mathf.PI);
        if (output < 0f) {
            return output + 2 * Mathf.PI;
        }

        return output;
    }
    
    public void DrawChunk() {
        Color col = Color.red;
        // Debug.DrawLine(corners[0], corners[1], col);
        // Debug.DrawLine(corners[0], corners[2], col);
        // Debug.DrawLine(corners[1], corners[3], col);
        // Debug.DrawLine(corners[2], corners[3], col);
        foreach (var node in nodes) {
            // Debug.DrawLine(node.pos, node.pos + Vector3.up*50, col);
            foreach (var edge in node.edges) {
                Vector3 offset = new Vector3(Random.value, 0, Random.value) * size * .03f;
                Debug.DrawLine(node.pos, edge.to.pos + offset, new Color(Random.value, Random.value, Random.value));
            }
        }
    }

    private void CreateHalfEdge(int from, int to){
        HalfEdge edge = new HalfEdge(nodes[to], nodes[from]);
        nodes[from].edges.Add(edge);
    }
    
    // w,h = # of inside roads
    public void GenerateRoads(int w, int h){
        Vector2 gap = new Vector2(size / (w + 1), size / (h + 1));
        for (int y = 0; y < h + 2; y++) {
            for (int x = 0; x < w + 2; x++) {
                Vector2 nodePos = new Vector2(x*gap.x + minPos.x, y*gap.y + minPos.y);


                float noiseScale = 159.23f;
                float noiseX = Mathf.PerlinNoise(nodePos.x * noiseScale, nodePos.y * noiseScale);
                float noiseY = Mathf.PerlinNoise(nodePos.x * noiseScale + 1000f, nodePos.y * noiseScale + 1000f);
                
                Vector2 offset = new Vector2(noiseX * 2 - 1, noiseY * 2 - 1) * nodeJitter;
                nodePos += offset;
                
                RoadNode node = new RoadNode(nodePos.x, nodePos.y);
                nodes.Add(node);


                //edges only need one halfedge, as the other is in adjacent chunk
                if (y != 0) {
                    //left edge
                    if (x == 0) {
                        CreateHalfEdge((y-1) * (w + 2), (y) * (w + 2));
                    }
                    
                    else {
                        if (y != h + 1) { // not top edge
                            CreateHalfEdge(y * (w + 2) + x, y * (w + 2) + x - 1);

                        }
                        if (x != w + 1) { // not right edge
                            CreateHalfEdge((y - 1) * (w + 2) + x, y * (w + 2) + x);
                        }
                        CreateHalfEdge(y * (w + 2) + x - 1, y * (w + 2) + x);
                        CreateHalfEdge(y * (w + 2) + x, (y - 1) * (w + 2) + x);
                    }
                }
                else {
                    //bottom edge
                    if (x != 0) {
                        CreateHalfEdge(x, x - 1);
                    }
                }
            }
        }
    }


    private HalfEdge FindNextEdge(HalfEdge edge){
        //angle between edge and x axis
        float baseAngle = WrapAngleRadian(-Mathf.Atan2(edge.from.pos.z - edge.to.pos.z, edge.from.pos.x - edge.to.pos.x ));
        Debug.Log($"pos to: {edge.to.pos}, from: {edge.from.pos}");
        HalfEdge closestEdge = null;
        float smallestAngle = 9999f;

        foreach (var newEdge in edge.to.edges) {
            float angleFromX = WrapAngleRadian(Mathf.Atan2(newEdge.to.pos.z - newEdge.from.pos.z, newEdge.to.pos.x - newEdge.from.pos.x));
            float angleFromBase = WrapAngleRadian(angleFromX + baseAngle);
            Debug.Log($"{baseAngle}, {angleFromX}, {angleFromBase}");
            if (angleFromBase > Mathf.Epsilon && angleFromBase < smallestAngle) {
                smallestAngle = angleFromBase;
                closestEdge = newEdge;
            }
        }
        return closestEdge;
    }
    
    public void GenerateFaces(){
        List<HalfEdge> edgesToCheck = new List<HalfEdge>();
        edgesToCheck.Add(nodes[0].edges[0]);

        while (edgesToCheck.Count > 0) {
            HalfEdge edge = edgesToCheck[0];
            
            HalfEdge closestEdge = FindNextEdge(edge);
            closestEdge.DrawEdge();
            closestEdge = FindNextEdge(closestEdge);
            closestEdge.DrawEdge();
            closestEdge = FindNextEdge(closestEdge);
            closestEdge.DrawEdge();
            closestEdge = FindNextEdge(closestEdge);
            closestEdge.DrawEdge();



            
            
            edge.visited = true;
            edgesToCheck.RemoveAt(0);
        }
        
    }
    
}


public class GridBasedGen : MonoBehaviour{

    [SerializeField] public GameObject TextUI;
    private List<TMP_Text> UITexts;

    [SerializeField] public GameObject playerObj;
    [SerializeField] public int chunkSize;
    [SerializeField] public int renderDistance;
    [SerializeField] public Vector2Int roadPartitions;
    [SerializeField] public float nodeJitter;
    public Dictionary<(int x, int y), Chunk> chunks = new Dictionary<(int x, int y), Chunk>();
    
    
    //creates a new empty chunk, return true if succeeds. exists => false
    public bool CreateChunk(int x, int y) {
        
        if (chunks.ContainsKey((x,y))) {
            return false;
        }

        Chunk newChunk = new Chunk(x, y, chunkSize, nodeJitter);

        newChunk.GenerateRoads(roadPartitions.x, roadPartitions.y);
        newChunk.GenerateFaces();
        
        chunks.Add((x,y), newChunk);

        return true;
    }


    
    void Start(){
        UITexts = new List<TMP_Text>();
        foreach (var text in TextUI.GetComponentsInChildren<TMP_Text>()) {
            UITexts.Add(text);
        }
    }

    void Update(){
        Vector3 plrPos = playerObj.transform.position;
        Vector2Int plrChunk = new Vector2Int(Mathf.FloorToInt(plrPos.x / chunkSize), Mathf.FloorToInt(plrPos.z / chunkSize));

        int chunksCreated = 0;
        int chunksDeleted = 0;
        //generate chunks
        for (int y = -renderDistance; y < renderDistance; y++) {
            for (int x = -renderDistance; x < renderDistance; x++) {
                
                Vector2Int currentChunkCoords = new Vector2Int(plrChunk.x + x, plrChunk.y + y);

                float distToPlr = Vector3.Distance(
                    new Vector3((currentChunkCoords.x + 0.5f) * chunkSize,0, (currentChunkCoords.y + 0.5f) * chunkSize),
                    plrPos
                );
                
                
                if (distToPlr < renderDistance * chunkSize) {

                    if (CreateChunk(currentChunkCoords.x, currentChunkCoords.y)) {
                        chunksCreated++;
                    }
                }
            }
        }
        
        //delete distant chunks
        List<(int, int)> chunksToDelete = new List<(int, int)>();
        foreach (var chunk in chunks) {
            float distToPlr = Vector3.Distance(chunk.Value.vec3Center, plrPos);
            if (distToPlr > renderDistance * chunkSize) {
                chunksToDelete.Add(chunk.Key);
            }
        } foreach (var key in chunksToDelete) {
            chunks.Remove(key);
            chunksDeleted++;
        }
        
        //draw each chunk
        foreach (var chunk in chunks) {
            chunk.Value.DrawChunk();
        }

        UITexts[0].text = $"Chunk Count: {chunks.Count}";
        UITexts[1].text = $"chunksCreated: {chunksCreated}";
        UITexts[2].text = $"chunksDeleted: {chunksDeleted}";
        
    }
}
