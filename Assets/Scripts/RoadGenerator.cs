using System.Collections.Generic;
using UnityEditor.TerrainTools;
using UnityEngine;
using TMPro;

//todo:
//density texture
// \_> nodes that try to stick to most or least change in density
// generate overlay graph that only shows intersections
// \_> find cycles in this new graph and find rough area for block designation
 


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
    [SerializeField] private GameObject TextUI;
    private List<TMP_Text> UITexts = new List<TMP_Text>();

    [SerializeField] private bool DrawQuadTree;
    [SerializeField] private bool DrawGraph;

    [SerializeField] private int networkWidth;
    
    private List<HeadNode> headNodes = new List<HeadNode>();
    private List<RoadNode> nodes = new List<RoadNode>();
    private QuadTree nodeTree;

    private float lastTicked;
    [SerializeField] private float tickDelay;

    [SerializeField] private float splitAngle;
    [SerializeField] private float angleRandomness;

    [SerializeField] private float nodeDistance;
    [SerializeField] private int nodesUntilSplit;


    private int headCounter = 0;
    private int nodeCounter = 0;
    private int disabledHeads = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastTicked = Time.time;
        headNodes.Add(new HeadNode(new Vector3(512, 0, 512), Vector3.forward, headCounter));
        headCounter++;
        foreach (var text in TextUI.GetComponentsInChildren<TMP_Text>()) {
            UITexts.Add(text);
        }

        nodeTree = new QuadTree(networkWidth);
    }

    // Update is called once per frame
    void Update()
    {
        if (DrawGraph) DrawConnections();
        if (DrawQuadTree) nodeTree.DrawTree();
        if (lastTicked + tickDelay < Time.time) {
            IterateGraph();
            lastTicked = Time.time;
                           
        }

;
    }
    
    void IterateGraph() {
        UITexts[0].text = "head nodes: " + headNodes.Count;
        UITexts[1].text = "disabled heads: " + disabledHeads;
        for (int i = 0; i < headNodes.Count; i++) {
            HeadNode h = headNodes[i];
            
            if (h.nodesSinceSplit > nodesUntilSplit) {
                SplitHeadNode(h);
            }
            
            IterateHead(h);
        }
        PurgeOldHeads();
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
        ;
        
        
        if (!(h.nodesSinceSplit < 1)) {
            RoadNode ClosestRoadNode = FindClosestNode(h);
            if (ClosestRoadNode != null && Vector3.Distance(ClosestRoadNode.pos, h.pos) < nodeDistance * 1.3f) {
                h.disabled = true;
                disabledHeads++;
                ClosestRoadNode.connections.Add(n);
                n.connections.Add(ClosestRoadNode);
            }
        }
        if (h.pos.x < 0 || h.pos.x > networkWidth || h.pos.z < 0 || h.pos.z > networkWidth) {
            h.disabled = true;
            disabledHeads++;
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

        List<RoadNode> nearNodes = nodeTree.GetNearRoadNodes(h.pos, 5);
        

        
        for (int i = 0; i < nearNodes.Count; i++) {
            RoadNode roadNode = nearNodes[i];
            if (roadNode.headID != h.ID) {
                float dist = Vector3.Distance(h.pos, roadNode.pos);
                if (closest > dist) {
                    closest = dist;
                    ClosestRoadNode = nearNodes[i];
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
        if (disabledHeads > 100) { 
            List<HeadNode> newNodes = new List<HeadNode>();
            for (int i = 0; i < headNodes.Count; i++) {
                if (!headNodes[i].disabled) {
                    newNodes.Add(headNodes[i]);
                }
            }

            disabledHeads = 0;
            headNodes = newNodes;}

    }
    
    void SplitHeadNode(HeadNode h) {
        Vector3 newDir = Quaternion.AngleAxis(splitAngle, Vector3.up) * h.dir;
        HeadNode newHead = new HeadNode(h.PrevRoadNode.pos + newDir * nodeDistance, newDir, headCounter);

        if (Vector3.Distance(FindClosestNode(newHead).pos, newHead.pos) < nodeDistance * .9f) {
            newHead.disabled = true;
            return;
        }
        headCounter++;
        newHead.PrevRoadNode = h.PrevRoadNode;
        headNodes.Add(newHead);
        h.nodesSinceSplit = 0;
        h.ID = headCounter;
        headCounter++;
    }
    
}
