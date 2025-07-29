using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.TerrainUtils;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Chunk{
    private Vector2 _pos; //bottom left corner
    private Vector2Int _coords;
    private float _width;
    private Vector2 _center;
    private float _gap;


    private Vector2 _voronoiCenter;
    
    public Chunk(int x, int y, float w, float gap){
        _width = w;
        _pos = new Vector2(x * w, y * w);
        _center = _pos + new Vector2(w / 2f, w / 2f);
        _coords = new Vector2Int(x, y);
        _gap = gap;

    }

    public void GenerateVoronoi(float gap){
        _voronoiCenter = GenerateVoronoiCenter(_coords.x, _coords.y);
    }
    
    private Vector2 GenerateVoronoiCenter(int x, int y){
        Vector2 chunkCenter = new Vector2(x * _width, y * _width) + new Vector2(_width / 2f, _width / 2f);

        float noiseScale = 159.23f;
        float noiseX = Mathf.PerlinNoise(x * noiseScale, y * noiseScale);
        float noiseY = Mathf.PerlinNoise(x * noiseScale + 1000f, y * noiseScale + 1000f);

        Vector2 offset = new Vector2(noiseX * 2 - 1, noiseY * 2 - 1); //-1 to 1
        offset *= (_width - _gap) / _width;

        
        Vector2 newCenter = offset * _width/2 + chunkCenter;
        return newCenter;
    }
    
    public void Draw(){
        Vector3 pos = new Vector3(_pos.x, 0, _pos.y);
        Vector3 up = Vector3.forward * _width;
        Vector3 right = Vector3.right * _width;
        Debug.DrawLine(pos, pos + right,Color.cyan);
        Debug.DrawLine(pos, pos + up,Color.cyan);
        Debug.DrawLine(pos + up, pos + right + up, Color.cyan);
        Debug.DrawLine(pos + right, pos + up + right ,Color.cyan);
        
        Vector3 center = new Vector3(_center.x, 0, _center.y);
        Debug.DrawLine(center, center + Vector3.up * _width/2, Color.white);
        
        Vector3 voronoiCenter = new Vector3(_voronoiCenter.x, 0, _voronoiCenter.y);
        Debug.DrawLine(voronoiCenter, voronoiCenter + Vector3.up * _width/2, Color.red);

        for (int y = -1; y < 2; y++) {
            for (int x = -1; x < 2; x++) {
                Vector2 newVoronoi = GenerateVoronoiCenter(_coords.x + x, _coords.y + y);
                Debug.DrawLine(voronoiCenter, new Vector3(newVoronoi.x, 0, newVoronoi.y), Color.red);
            }
        }

    }
}


public class VoronoiGen : MonoBehaviour{
    [SerializeField] private float chunkWidth;
    [SerializeField] private float edgeGap;

    private List<Chunk> chunks = new List<Chunk>();

    void CreateChunk(int x, int y){
        Chunk newChunk = new Chunk(x, y, chunkWidth, edgeGap);
        newChunk.GenerateVoronoi(edgeGap);
        chunks.Add(newChunk);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        int temp = 10;
        for (int y = 0; y < temp; y++) {
            for (int x = 0; x < temp; x++) {
                CreateChunk(x, y);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var chu in chunks) {
            chu.Draw();
        }
    }
}
