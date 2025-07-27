using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Net;
using UnityEngine;
using TMPro;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using UnityEngine.Profiling;




public class GridBasedGen : MonoBehaviour{

    [SerializeField] public GameObject TextUI;
    private List<TMP_Text> UITexts;

    [SerializeField] public GameObject playerObj;
    [SerializeField] public int chunkSize;
    [SerializeField] public bool drawChunks;
    [SerializeField] public int renderDistance;
    [SerializeField] public Vector2Int roadPartitions;
    [SerializeField] public float nodeJitter;
    public Dictionary<(int x, int y), Chunk> chunks = new Dictionary<(int x, int y), Chunk>();
    
    
    //creates a new empty chunk, return true if succeeds. exists => false
    public bool CreateChunk(int x, int y) {
        
        if (chunks.ContainsKey((x,y))) {
            return false;
        }

        Chunk newChunk = new Chunk(x, y, chunkSize, nodeJitter, roadPartitions);

        newChunk.GenerateRoads(roadPartitions.x, roadPartitions.y);
        newChunk.AssignEdgeAngles();    
        newChunk.AssignAllNextEdges();
        newChunk.GenerateBlocks();
        newChunk.DrawAllBlocks();
        
        // newChunk.DrawAllNextPointers();
        UITexts[2].text = $"{newChunk.edgesChecked}";
        newChunk.edgesChecked = 0;

        
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
            chunks[key] = null;
            chunks.Remove(key);
            chunksDeleted++;
        }

        if (drawChunks) {
            foreach (var chunk in chunks) {
                chunk.Value.DrawChunk();
            }
        }



        UITexts[0].text = $"Chunk Count: {chunks.Count}";
        UITexts[1].text = $"faces: {chunks[(0,0)].blocks.Count}";

        float startTime = Time.realtimeSinceStartup;
        // for (int i = 0; i < 10; i++) {
        //     chunks.Remove((0,0));
        //     CreateChunk(0, 0);
        // }

        UITexts[3].text = $"ms for chunk gen: {(Time.realtimeSinceStartup - startTime) * 1000}";


    }
}
