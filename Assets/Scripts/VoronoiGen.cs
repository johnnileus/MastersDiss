using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;
using Unity.Mathematics;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;


public class Chunk{
    private Vector2 _pos; //bottom left corner
    private Vector2Int _coords;
    private float _width;
    private Vector2 _center;
    private float _gap;


    private Vector2 _voronoiCenter;
    private List<Vector2> _voronoiPoints;
    private List<Block> _blocks = new List<Block>();
    
    public Chunk(int x, int y, float w, float gap){
        _width = w;
        _pos = new Vector2(x * w, y * w);
        _center = _pos + new Vector2(w / 2f, w / 2f);
        _coords = new Vector2Int(x, y);
        _gap = gap;
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

        _voronoiPoints = CalculateVoronoiCell(_voronoiCenter, neighbours, bounds);
    }

    private List<Vector2> CalculateVoronoiCell(Vector2 centerPoint, List<Vector2> neighborPoints, List<Vector2> bounds){
        List<Vector2> subjectPolygon = new List<Vector2>(bounds);

        foreach (var neighbor in neighborPoints) {
            Vector2 midPoint = (centerPoint + neighbor) / 2f;
            Vector2 normal = (centerPoint - neighbor).normalized;

            List<Vector2> clippedPolygon = new List<Vector2>();
            if (subjectPolygon.Count == 0) continue;

            Vector2 start = subjectPolygon[^1];

            //clip
            foreach (var end in subjectPolygon) {
                bool startIsInside = PolyUtil.IsInside(start, midPoint, normal);
                bool endIsInside = PolyUtil.IsInside(end, midPoint, normal);

                if (endIsInside) {
                    if (!startIsInside) {
                        clippedPolygon.Add(PolyUtil.GetIntersection(start, end, midPoint, normal));
                    }
                    clippedPolygon.Add(end);
                }
                else if (startIsInside) {
                    clippedPolygon.Add( PolyUtil.GetIntersection(start, end, midPoint, normal));
                }
                start = end;
            }

            subjectPolygon = clippedPolygon;
        }
        return subjectPolygon;
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

    private List<IPoint> GeneratePoints(){
        List<IPoint> pts = new List<IPoint>();
        for (int i = 0; i < 200; i++) {
            float x = (Random.value * 2 - 1) * _width / 2 + _voronoiCenter.x;
            float y = (Random.value * 2 - 1) * _width / 2 + _voronoiCenter.y;

            if (PolyUtil.IsPointInPolygon(new Vector2(x, y), _voronoiPoints, _voronoiCenter)) {
                pts.Add(new Point(x, y));

            }

        }
        return pts;
    }
    
    public void GenerateBlocks(Material blockMaterial, Material roadMaterial){

        List<IPoint> originalPoints = GeneratePoints();
        
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
            
            List<Vector2> pts = CalculateVoronoiCell(new Vector2((float)center.X, (float)center.Y), neighbours, bounds);

            for (int i = 0; i < pts.Count; i++) {
                RoadNode node = new RoadNode(pts[^(i+1)].x, pts[^(i+1)].y);
                face.nodes.Add(node);
            }
            

            Block block = new Block(face);
            block.GenerateInset();
            block.GenerateBlock(blockMaterial);
            block.GenerateRoad(roadMaterial);

        }
        

        




            // Debug.Log($"{halfedges[0]}, {halfedges[1]}");
        // int p1 = triangles[halfedges[0]];
        // int p2 = halfedges[halfedges[0]];
        // Debug.DrawLine(points[p1].ToVector3(), points[p2].ToVector3(), Color.blue, 99f);
        //



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
    
    private List<Chunk> chunks = new List<Chunk>();

    void CreateChunk(int x, int y){
        Chunk newChunk = new Chunk(x, y, chunkWidth, edgeGap);
        newChunk.GenerateVoronoi();
        newChunk.GenerateBlocks(blockMaterial, roadMaterial);
        
        chunks.Add(newChunk);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        int temp = 16;
        for (int y = 0; y < temp; y++) {
            for (int x = 0; x < temp; x++) {
                CreateChunk(x, y);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
