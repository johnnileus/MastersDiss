using System.Collections.Generic;
using System.Net;
using UnityEditor.TerrainTools;
using UnityEngine;
using TMPro;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using UnityEngine.Serialization;


public class Chunk {
    public Vector2 chunkID;

    public Vector2 minPos;
    public Vector2 maxPos;
    public Vector3[] corners;
    
    public Chunk(int x, int y, Vector2 chunkSize) {
        chunkID = new Vector2(x, y);

        minPos = new Vector2(x * chunkSize.x, y * chunkSize.y);
        maxPos = new Vector2((x + 1) * chunkSize.x, (y + 1) * chunkSize.y);

        corners = new[] {
            new Vector3(minPos.x, 0, minPos.y),
            new Vector3(maxPos.x, 0, minPos.y),
            new Vector3(minPos.x, 0, maxPos.y),
            new Vector3(maxPos.x, 0, maxPos.y)
        };
    }
    
    public void DrawChunk() {
        Color col = Color.red;
        Debug.DrawLine(corners[0], corners[1], col);
        Debug.DrawLine(corners[0], corners[2], col);
        Debug.DrawLine(corners[1], corners[3], col);
        Debug.DrawLine(corners[2], corners[3], col);
    }
}


public class GridBasedGen : MonoBehaviour {


    [SerializeField] public Vector2 chunkSize;
    public Dictionary<(int x, int y), Chunk> chunks = new Dictionary<(int x, int y), Chunk>();
    
    
    //creates a new empty chunk, return true if succeeds
    public bool CreateChunk(int x, int y) {
        
        if (chunks.ContainsKey((x,y))) {
            return false;
        }

        Chunk newChunk = new Chunk(x, y, chunkSize);
        chunks.Add((x,y), newChunk);

        return true;
    }

    void Start() {
        CreateChunk(0, 0);
    }

    void Update()
    {
        foreach (var chunk in chunks) {
            chunk.Value.DrawChunk();
        }
    }
}
