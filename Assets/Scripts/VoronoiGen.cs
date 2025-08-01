using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;


public class Chunk{
    private Vector2 _pos; //bottom left corner
    private Vector2Int _coords;
    private float _width;
    public Vector2 Center;
    private float _gap;


    private Vector2 _voronoiCenter;
    private List<Vector2> _voronoiPoints;
    public List<Block> Blocks = new List<Block>();
    
    public Chunk(int x, int y, float w, float gap){
        _width = w;
        _pos = new Vector2(x * w, y * w);
        Center = _pos + new Vector2(w / 2f, w / 2f);
        _coords = new Vector2Int(x, y);
        _gap = gap;
    }
    
    private int GetChunkSeed(){
        return (int)(_coords.x * 1619.5125f + _coords.y * 31337.65125f);
    }
    public void GenerateVoronoi(){
        _voronoiCenter = GenerateVoronoiCenter(_coords.x, _coords.y);

        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; y++) {
            for (int x = -1; x < 2; x++) {
                if (y == 0 && x == 0) continue;
                neighbours.Add(GenerateVoronoiCenter(_coords.x + x, _coords.y + y));
            }
        }

        float boundSize = _width;
    
        List<Vector2> bounds = new List<Vector2> {
            new (_voronoiCenter.x - boundSize, _voronoiCenter.y - boundSize), 
            new (_voronoiCenter.x + boundSize, _voronoiCenter.y - boundSize), 
            new (_voronoiCenter.x + boundSize, _voronoiCenter.y + boundSize),
            new (_voronoiCenter.x - boundSize, _voronoiCenter.y + boundSize)
        };

        _voronoiPoints = PolyUtil.CalculateVoronoiCell(_voronoiCenter, neighbours, bounds);
    }

   
    
    private Vector2 GenerateVoronoiCenter(int x, int y){
        Vector2 chunkCenter = new Vector2(x * _width, y * _width) + new Vector2(_width / 2f, _width / 2f);

        float noiseScale = 159.23f;
        float noiseX = Mathf.PerlinNoise(x * noiseScale, y * noiseScale); 
        float noiseY = Mathf.PerlinNoise(x * noiseScale + 1000f, y * noiseScale + 1000f);

        noiseX = noiseX * 491051.4194f % 1;
        noiseY = noiseY * 950925.95285f % 1;

        Vector2 randomDirection = new Vector2(noiseX * 2 - 1, noiseY * 2 - 1);
        float maxOffset = Mathf.Max(0f, (_width / 2f) - _gap);

        Vector2 finalOffset = randomDirection * maxOffset;
        Vector2 newPoint = chunkCenter + finalOffset;
    
        return newPoint;
    }


    private List<IPoint> GeneratePoints(int numPoints, float minDistance, int seed)
    {
        List<IPoint> pts = new List<IPoint>();
        System.Random rand = new System.Random(seed);
    
        int maxAttempts = numPoints * 50;
        int currentAttempts = 0;

        while (pts.Count < numPoints && currentAttempts < maxAttempts) {
            currentAttempts++;

            float x = ((float)rand.NextDouble() * 2 - 1) * _width + _voronoiCenter.x;
            float y = ((float)rand.NextDouble() * 2 - 1) * _width + _voronoiCenter.y;
            Point candidatePoint = new Point(x, y);
        
            if (!PolyUtil.IsPointInPolygon(new Vector2(x, y), _voronoiPoints, _voronoiCenter)) {
                continue; 
            }
        
            bool isTooClose = false;
            float minDistanceSq = minDistance * minDistance; 

            foreach (IPoint existingPoint in pts) {
                float dx = (float)(candidatePoint.X - existingPoint.X);
                float dy = (float)(candidatePoint.Y - existingPoint.Y);
                float distanceSq = dx * dx + dy * dy; 

                if (distanceSq < minDistanceSq)
                {
                    isTooClose = true;
                    break;
                }
            }
            if (!isTooClose) {
                pts.Add(candidatePoint);
            }
        }

        return pts;
    }

    
    public List<GameObject> GenerateBlocks(Material blockMaterial, Material roadMaterial){

        List<GameObject> GOs = new List<GameObject>();
            List<IPoint> originalPoints = GeneratePoints(50, _width/5f, GetChunkSeed());
        
        // List<IPoint> points = _voronoiPoints.Select(point => new Vector2(point.x, point.y)).ToPoints().ToList();
        
        var delaunator = new Delaunator(originalPoints.ToArray());
        
        var points = delaunator.Points;
        var neighboursDict = new Dictionary<int, HashSet<int>>();
        for (int i = 0; i < delaunator.Points.Length; i++) {
            neighboursDict[i] = new HashSet<int>();
        }

        for (int e = 0; e < delaunator.Triangles.Length; e++) {
            int p = delaunator.Triangles[e];
            int q = delaunator.Triangles[Delaunator.NextHalfedge(e)];

            neighboursDict[p].Add(q);
            neighboursDict[q].Add(p);
        }
        
                
        foreach (var point in neighboursDict) {
            IPoint center = points[point.Key];
            Face face = new Face();

            List<Vector2> neighbours = new List<Vector2>();
            foreach (var neighbour in point.Value) {
                Vector2 p = new Vector2((float)points[neighbour].X, (float)points[neighbour].Y);
                neighbours.Add(p);
            }
            
            List<Vector2> bounds = _voronoiPoints;
            
            List<Vector2> pts = PolyUtil.CalculateVoronoiCell(new Vector2((float)center.X, (float)center.Y), neighbours, bounds);

            for (int i = 0; i < pts.Count; i++) {
                RoadNode node = new RoadNode(pts[^(i+1)].x, pts[^(i+1)].y);
                face.nodes.Add(node);
            }
            

            Block block = new Block(face);
            block.GenerateInset();
            GOs.Add(block.GenerateBlock());
            GOs.Add(block.GenerateRoad());
            Blocks.Add(block);
            
        }

        return GOs;

    }
    
    public void Draw(){
        // Vector3 pos = new Vector3(_pos.x, 0, _pos.y);
        // Vector3 up = Vector3.forward * _width;
        // Vector3 right = Vector3.right * _width;
        // Debug.DrawLine(pos, pos + right,Color.cyan);
        // Debug.DrawLine(pos, pos + up,Color.cyan);
        // Debug.DrawLine(pos + up, pos + right + up, Color.cyan);
        // Debug.DrawLine(pos + right, pos + up + right ,Color.cyan);
        
        // Vector3 center = new Vector3(_center.x, 0, _center.y);
        // Debug.DrawLine(center, center + Vector3.up * _width/2, Color.white);
        
        Vector3 voronoiCenter = new Vector3(_voronoiCenter.x, 0, _voronoiCenter.y);
        Debug.DrawLine(voronoiCenter, voronoiCenter + Vector3.up * _width/2, Color.red);

        for (int i = 0; i < _voronoiPoints.Count - 1; i++) {
            Vector3 from = new Vector3(_voronoiPoints[i].x, 0, _voronoiPoints[i].y);
            Vector3 to = new Vector3(_voronoiPoints[i+1].x, 0, _voronoiPoints[i+1].y);
            Debug.DrawLine(from, to, Color.red);
        }
    }
}


public class VoronoiGen : MonoBehaviour{
    [SerializeField] private float chunkWidth;
    [SerializeField] private float edgeGap;

    [SerializeField] public Material blockMaterial;
    [SerializeField] public Material roadMaterial;

    [SerializeField] private GameObject player;
    [SerializeField] public int renderDistance;

    [SerializeField] public bool DrawVoronoiLines;
    
    public Dictionary<(int x, int y), Chunk> chunks = new Dictionary<(int x, int y), Chunk>();


    bool CreateChunk(int x, int y){
        if (chunks.ContainsKey((x, y))) {
            return false;
        }
        
        Chunk newChunk = new Chunk(x, y, chunkWidth, edgeGap);
        newChunk.GenerateVoronoi();
        List<GameObject> GOs = newChunk.GenerateBlocks(blockMaterial, roadMaterial);
        foreach (var GO in GOs) {
            GO.transform.SetParent(this.transform);
        }
        
        chunks.Add((x, y), newChunk);
        return true;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){

        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 plrPos = player.transform.position;
        Vector2Int plrChunk = new Vector2Int(Mathf.FloorToInt(plrPos.x / chunkWidth), Mathf.FloorToInt(plrPos.z / chunkWidth));
        
        int chunksCreated = 0;
        int chunksDeleted = 0;
        
        for (int y = -renderDistance; y < renderDistance; y++) {
            for (int x = -renderDistance; x < renderDistance; x++) {
                
                Vector2Int currentChunkCoords = new Vector2Int(plrChunk.x + x, plrChunk.y + y);

                float distToPlr = Vector3.Distance(
                    new Vector3((currentChunkCoords.x + 0.5f) * chunkWidth,0, (currentChunkCoords.y + 0.5f) * chunkWidth),
                    plrPos
                );
                
                
                if (distToPlr < renderDistance * chunkWidth) {

                    if (CreateChunk(currentChunkCoords.x, currentChunkCoords.y)) {
                        chunksCreated++;
                    }
                }
            }
            
        }
        //delete distant chunks
        List<(int, int)> chunksToDelete = new List<(int, int)>();
        foreach (var chunk in chunks) {
            float distToPlr = Vector3.Distance(PolyUtil.ToVec3(chunk.Value.Center), plrPos);
            if (distToPlr > renderDistance * chunkWidth) {
                chunksToDelete.Add(chunk.Key);
            }
        }
        
        foreach (var key in chunksToDelete) {
            foreach (var block in chunks[key].Blocks) {
                Destroy(block.GetBuildingObject());
                Destroy(block.GetRoadObject());
            }
            chunks[key] = null;
            chunks.Remove(key);
            chunksDeleted++;
        }
        
        if (DrawVoronoiLines) {
            foreach (var chunk in chunks) {
                chunk.Value.Draw();
            }
        }
        
            
        
    }
}
