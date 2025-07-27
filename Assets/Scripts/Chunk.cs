using System.Collections.Generic;
using UnityEngine;

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
    public List<HalfEdge> edges = new List<HalfEdge>();


    public void Draw(){
        Color col = new Color(Random.value, Random.value, Random.value);
        Vector3 offset = new Vector3(0, Random.value * 15f, 0);
        foreach (var edge in edges) {
            Debug.DrawLine(edge.from.pos + offset, edge.to.pos + offset, col, 999f);
        }
    }
    
}

public class HalfEdge{
    public RoadNode to;
    public RoadNode from;
    public HalfEdge next;

    public float angleFromX;
    
    public bool nextVisited = false; // visited when generating next pointer
    public bool faceVisited = false; // visited when generating faces using next pointer

    public HalfEdge(RoadNode target, RoadNode source){
        to = target;
        from = source;
        next = null;
    }

    public void DrawEdge(float t = 999f){
        Vector3 offset = new Vector3(Random.value, 2f, Random.value) * 10f;
        Debug.DrawLine(from.pos + Vector3.up * 20f, to.pos + offset, Color.gray, t);
        Debug.DrawLine(from.pos, from.pos + Vector3.up * 50f + offset, Color.cyan, t);
        Debug.DrawLine(to.pos, to.pos + Vector3.up * 50f + offset, Color.red, t);
    }

    public Vector3 Midpoint(){
        return (to.pos - from.pos) / 2 + from.pos;
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
    public Vector2Int roadPartitions;

    private float size;

    public List<RoadNode> nodes = new List<RoadNode>();
    public List<Face> faces = new List<Face>();

    public int edgesChecked = 0;
    
    public Chunk(int x, int y, float chunkSize, float jitter, Vector2Int roadPart){
        size = chunkSize;
        nodeJitter = chunkSize * jitter; // convert 0 -> 1 to 0 -> distance to chunkSize;
        chunkID = new Vector2(x, y);
        roadPartitions = roadPart;

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
    
    public void AssignEdgeAngles(){ // generate the angle between the x axis for each edge
        int w = roadPartitions.x + 2;
        int h = roadPartitions.y + 2;
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                int index = y * w + x;
                foreach (var edge in nodes[index].edges) {
                    float angle = WrapAngleRadian(Mathf.Atan2(edge.to.pos.z - edge.from.pos.z, edge.to.pos.x - edge.from.pos.x));
                    edge.angleFromX = angle;
                }
            }
        }
    }
    
    private int FindNextEdge(HalfEdge edge){  // find next CCW edge for given edge, returns index for 'to' node
        //angle between edge and x axis
        float baseAngle = WrapAngleRadian(-Mathf.Atan2(edge.from.pos.z - edge.to.pos.z, edge.from.pos.x - edge.to.pos.x ));
        int closestEdge = 0;
        float smallestAngle = 9999f;

        for (int i = 0; i < edge.to.edges.Count; i++) {
            
            
            HalfEdge newEdge = edge.to.edges[i];
            if (edge.from == newEdge.to) continue;

            float angleFromX = newEdge.angleFromX;
            float angleFromBase = WrapAngleRadian(angleFromX + baseAngle);
            
            if (angleFromBase > Mathf.Epsilon && angleFromBase < smallestAngle) {
                smallestAngle = angleFromBase;
                closestEdge = i;
            }
        }
        return closestEdge;
    }

    public void AssignAllNextEdges(){ // assign next edge for all edges.
        List<HalfEdge> edgesToCheck = new List<HalfEdge> { nodes[0].edges[0] };

        int count = 0;
        
        while (edgesToCheck.Count > 0 && count < 10000) {
            
            HalfEdge edge = edgesToCheck[0];
            
            int closestEdgeIndex = FindNextEdge(edge);
            for (int i = 0; i < edge.to.edges.Count; i++) {
                if (!edge.to.edges[i].nextVisited) {
                    edgesToCheck.Add(edge.to.edges[i]);
                }
            }
            edge.next = edge.to.edges[closestEdgeIndex];

            edge.nextVisited = true;
            edgesToCheck.RemoveAt(0);
            edgesChecked++;
            count++;
        }
        
    }
    //fix for when edge crashes
    public void GenerateFaces(){
        foreach (var node in nodes) {
            foreach (var edge in node.edges) {
                if (edge.faceVisited) {
                    continue;
                }
        
                Face face = new Face();
                HalfEdge currentEdge = edge;
                do {
                    if (currentEdge.faceVisited) {
                        break;
                    }
                    face.edges.Add(currentEdge);
                    face.nodes.Add(currentEdge.from);
                    currentEdge.faceVisited = true;
                    currentEdge = currentEdge.next;
                } while (currentEdge != edge && !currentEdge.faceVisited);
                
                faces.Add(face);
                

            }
        }

    }

    public void DrawAllNextPointers(){
        foreach (var node in nodes) {
            foreach (var edge in node.edges) {
                Debug.DrawLine(edge.Midpoint(), edge.next.Midpoint() + Vector3.up*5f, Color.cyan, 99f);
            }
        }
    }
    
    public void DrawAllFaces(){
        foreach (var face in faces) {
            face.Draw();
        }
    }
}
