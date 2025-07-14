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

public class RoadNode{
    public Vector3 pos;
    public List<HalfEdge> edges = new List<HalfEdge>();

    public RoadNode(float x, float y){
        pos = new Vector3(x, 0, y);
    }
}

public class HalfEdge{
    public RoadNode to;

    public HalfEdge(RoadNode target){
        to = target;
    }
}

public class Chunk {


    public Vector2 chunkID;

    public Vector2 minPos;
    public Vector2 maxPos;
    public Vector2 center;
    public Vector3 vec3Center;
    public Vector3[] corners;

    private float size;

    public List<RoadNode> nodes = new List<RoadNode>();
    
    public Chunk(int x, int y, float chunkSize){
        size = chunkSize;
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
    
    public void DrawChunk() {
        Color col = Color.red;
        // Debug.DrawLine(corners[0], corners[1], col);
        // Debug.DrawLine(corners[0], corners[2], col);
        // Debug.DrawLine(corners[1], corners[3], col);
        // Debug.DrawLine(corners[2], corners[3], col);
        foreach (var node in nodes) {
            Debug.DrawLine(node.pos, node.pos + Vector3.up*50, col);
            foreach (var edge in node.edges) {
                Debug.DrawLine(node.pos, edge.to.pos, Color.cyan);
            }
        }
    }
    
    // w,h = # of inside roads
    public void GenerateRoads(int w, int h){
        Vector2 gap = new Vector2(size / (w + 1), size / (h + 1));
        for (int y = 0; y < h + 2; y++) {
            for (int x = 0; x < w + 2; x++) {
                RoadNode node = new RoadNode((x)*gap.x + minPos.x, (y)*gap.y + minPos.y);
                nodes.Add(node);

                if (y != 0) {
                    //left edge
                    if (x == 0 && y != h + 2) {
                        HalfEdge edge = new HalfEdge(nodes[(y - 1) * (w + 2)]);
                        nodes[y * (w + 2)].edges.Add(edge);
                    }
                    else {
                        HalfEdge southEdge = new HalfEdge(nodes[(y - 1) * (w + 2) + x]);
                        HalfEdge westEdge = new HalfEdge(nodes[y * (w + 2) + x - 1]);
                        nodes[y * (w + 2) + x].edges.Add(southEdge);
                        nodes[y * (w + 2) + x].edges.Add(westEdge);
                    }
                }
                else {
                    //bottom edge
                    if (x != 0) {
                        HalfEdge edge = new HalfEdge(nodes[x - 1]);
                        nodes[x].edges.Add(edge);
                    }
                }
            }
        }
    }
}


public class GridBasedGen : MonoBehaviour{

    [SerializeField] public GameObject playerObj;
    [SerializeField] public int chunkSize;
    [SerializeField] public int renderDistance;
    public Dictionary<(int x, int y), Chunk> chunks = new Dictionary<(int x, int y), Chunk>();
    
    
    //creates a new empty chunk, return true if succeeds. exists => false
    public bool CreateChunk(int x, int y) {
        
        if (chunks.ContainsKey((x,y))) {
            return false;
        }

        Chunk newChunk = new Chunk(x, y, chunkSize);

        newChunk.GenerateRoads(1,1);
        
        chunks.Add((x,y), newChunk);

        return true;
    }

    void Start() {
    }

    void Update(){

        Vector3 plrPos = playerObj.transform.position;
        Vector2Int plrChunk = new Vector2Int(Mathf.FloorToInt(plrPos.x / chunkSize), Mathf.FloorToInt(plrPos.z / chunkSize));
        

        //generate chunks
        for (int y = -renderDistance; y < renderDistance; y++) {
            for (int x = -renderDistance; x < renderDistance; x++) {
                
                Vector2Int currentChunkCoords = new Vector2Int(plrChunk.x + x, plrChunk.y + y);

                float distToPlr = Vector2.Distance(
                    new Vector2(currentChunkCoords.x, currentChunkCoords.y),
                    new Vector2(plrChunk.x, plrChunk.y)
                );
                
                if (distToPlr <= renderDistance * chunkSize) {

                    CreateChunk(currentChunkCoords.x, currentChunkCoords.y);
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
        }
        
        //draw each chunk
        foreach (var chunk in chunks) {
            chunk.Value.DrawChunk();
        }
    }
}
