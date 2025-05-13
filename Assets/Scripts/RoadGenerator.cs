using System.Collections.Generic;
using UnityEditor.TerrainTools;
using UnityEngine;

//todo:
// weighted direction changing
// boundary
// quad-tree/other

public class HeadNode {
    public Vector3 pos;
    public Vector3 dir;
    public RoadNode PrevRoadNode;
    public int nodesSinceSplit = 0;
    public bool disabled = false;
    public int ID = -1;

    public HeadNode(Vector3 p, Vector3 d, int id) {
        pos = p;
        dir = d;
        ID = id;

    }
}

public class RoadNode {
    public Vector3 pos;
    public List<RoadNode> connections = new List<RoadNode>();
    public int headID = -1;
    public int ID;

    public RoadNode(Vector3 p) {
        pos = p;
    }
}

public class RoadGenerator : MonoBehaviour {

    [SerializeField] private GameObject testBall;

    private List<HeadNode> headNodes = new List<HeadNode>();
    private List<RoadNode> nodes = new List<RoadNode>();
    private QuadTree nodeTree = new QuadTree(1024);

    private float lastTicked;
    [SerializeField] private float tickDelay;

    [SerializeField] private float splitAngle;
    [SerializeField] private float angleRandomness;

    [SerializeField] private float nodeDistance;


    private int headCounter = 0;
    private int nodeCounter = 0;
    private int disabledHeads = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastTicked = Time.time;
        headNodes.Add(new HeadNode(new Vector3(512, 0, 512), Vector3.forward, headCounter));
        headCounter++;

        
    }

    // Update is called once per frame
    void Update()
    {
        if (lastTicked + tickDelay < Time.time) {
            IterateGraph();
            lastTicked = Time.time;
        }
        nodeTree.DrawTree();
    }
    
    void IterateGraph() {
        for (int i = 0; i < headNodes.Count; i++) {
            HeadNode h = headNodes[i];
            
            if (h.nodesSinceSplit > 8) {
                SplitHeadNode(h);
            }
            
            IterateHead(h);
        }
        PurgeOldHeads();
        DrawConnections();
    }
    
    void IterateHead(HeadNode h) {
        if (h.disabled) return;

        //create node
        RoadNode n = new RoadNode(h.pos);
        n.ID = nodeCounter;
        nodeCounter++;
        n.headID = h.ID;
        
        if (h.PrevRoadNode != null) {
            n.connections.Add(h.PrevRoadNode);
        }

        h.PrevRoadNode?.connections.Add(n);
        nodes.Add(n);
        DisplayNode(n);
        nodeTree.AddNodeToTree(n);
        
        
        if (!(h.nodesSinceSplit < 1)) {
            RoadNode ClosestRoadNode = FindClosestNode(h);
            if (ClosestRoadNode != null && Vector3.Distance(ClosestRoadNode.pos, h.pos) < nodeDistance * 1.8f) {
                h.disabled = true;
                ClosestRoadNode.connections.Add(n);
                n.connections.Add(ClosestRoadNode);
            }
        }

        
        
        Vector3 newDir = Quaternion.AngleAxis(Random.Range(-angleRandomness, angleRandomness), Vector3.up) * h.dir;
        h.dir = newDir;
        h.pos += newDir * nodeDistance;
        h.PrevRoadNode = n;
        h.nodesSinceSplit++;
    }
    
    
    void DisplayNode(RoadNode n) {
        Instantiate(testBall, n.pos, Quaternion.identity);
    }
    
    RoadNode FindClosestNode(HeadNode h) {
        float closest = 10000f;
        RoadNode ClosestRoadNode = null;
        
        for (int i = 0; i < nodes.Count; i++) {
            RoadNode RoadNode = nodes[i];
            if (RoadNode.headID != h.ID) {
                float dist = Vector3.Distance(h.pos, RoadNode.pos);
                if (closest > dist) {
                    closest = dist;
                    ClosestRoadNode = nodes[i];
                }
            }
        }   
        return ClosestRoadNode;
    }
    
    void DrawConnections() {
        for (int i = 0; i < nodes.Count; i++) {
            RoadNode RoadNode = nodes[i];

            for (int j = 0; j < RoadNode.connections.Count; j++) {
                RoadNode neighbour = RoadNode.connections[j];
                Vector3 pos1 = RoadNode.pos;
                Vector3 pos2 = neighbour.pos;
                Debug.DrawLine(pos1, pos2, Color.red, tickDelay);
            }
        }
    }

    void PurgeOldHeads() {
        if (disabledHeads > 10) { 
            List<HeadNode> newNodes = new List<HeadNode>();
            for (int i = 0; i < headNodes.Count; i++) {
                if (!headNodes[i].disabled) {
                    newNodes.Add(headNodes[i]);
                }
            }

            headNodes = newNodes;}

    }
    
    void SplitHeadNode(HeadNode h) {
        Vector3 newDir = Quaternion.AngleAxis(splitAngle, Vector3.up) * h.dir;
        HeadNode newHead = new HeadNode(h.PrevRoadNode.pos + newDir * nodeDistance, newDir, headCounter);

        if (Vector3.Distance(FindClosestNode(newHead).pos, newHead.pos) < nodeDistance * .9f) {
            //discard head
        }
        headCounter++;
        newHead.PrevRoadNode = h.PrevRoadNode;
        headNodes.Add(newHead);
        h.nodesSinceSplit = 0;
        h.ID = headCounter;
        headCounter++;
    }
    
}
