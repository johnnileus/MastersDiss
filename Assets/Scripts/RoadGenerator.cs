using System.Collections.Generic;
using UnityEditor.TerrainTools;
using UnityEngine;
using TMPro;
using Unity.Mathematics.Geometry;
using UnityEngine.Serialization;

//todo:
//density texture
// \_> nodes that try to stick to most or least change in density
// generate overlay graph that only shows intersections
// \_> find cycles in this new graph and find rough area for block designation
// scan random segments for distance between end points OR merge small cycles into larger ones

//create different starting configurations, such as star and circle seeds
//merge smaller areas into larger ones
// split areas into grid road networks
 


public class HeadNode {
    public Vector3 pos;
    public Vector3 dir;
    public RoadNode prevNode;
    public int nodesSinceSplit = 0;
    public bool disabled = false;
    public int ID = -1;

    public HeadNode(Vector3 p, Vector3 d, int id, RoadNode prevRoadNode = null) {
        pos = p;
        dir = d;
        ID = id;
        prevNode = prevRoadNode;

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

    [SerializeField] private bool active;
    
    [SerializeField] private GameObject testBall;
    [SerializeField] private GameObject TextUI;
    [SerializeField] private Texture2D densityMap;
    private List<TMP_Text> UITexts = new List<TMP_Text>();
    private GameObject mapMesh;
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
    [SerializeField] private int lowerNodesUntilSplit; 
    [SerializeField] private int UpperNodesUntilSplit;
    

    private int headCounter = 0;
    private int nodeCounter = 0;
    private int disabledHeads = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastTicked = Time.time;
        nodeTree = new QuadTree(networkWidth);


        mapMesh = transform.GetChild(0).gameObject;
        mapMesh.transform.localScale = new Vector3(networkWidth, networkWidth, 1);
        mapMesh.transform.position = new Vector3(networkWidth / 2f, -1, networkWidth / 2f);

        
        foreach (var text in TextUI.GetComponentsInChildren<TMP_Text>()) {
            UITexts.Add(text);
        }
        InitialiseNodes();

    }

    // Update is called once per frame
    void Update() {
        if (!active) return;
        if (lastTicked + tickDelay < Time.time) {
            IterateGraph();
            lastTicked = Time.time;
                           
        }
        if (DrawGraph) DrawConnections();
        if (DrawQuadTree) nodeTree.DrawTree();
;
    }
    
    void IterateGraph() {
        UITexts[0].text = "head nodes: " + headNodes.Count;
        UITexts[1].text = "disabled heads: " + disabledHeads;
        UITexts[2].text = "total nodes: " + nodes.Count;
        for (int i = 0; i < headNodes.Count; i++) {
            HeadNode h = headNodes[i];
            float density = GetMapColour(h.pos, densityMap)[0];
            float nodesUntilSplit = Mathf.Lerp(UpperNodesUntilSplit, lowerNodesUntilSplit, density);
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
        
        if (h.prevNode != null) {
            n.connections.Add(h.prevNode);
        }

        h.prevNode?.connections.Add(n);
        nodes.Add(n);
        DisplayNode(n);
        nodeTree.AddNodeToTree(n);
        
        
        if (!(h.nodesSinceSplit <= 1)) {
            RoadNode ClosestRoadNode = FindClosestNode(h);
            if (ClosestRoadNode != null && Vector3.Distance(ClosestRoadNode.pos, h.pos) < nodeDistance * 1.5f) {
                h.disabled = true;
                disabledHeads++;
                ClosestRoadNode.connections.Add(n);
                n.connections.Add(ClosestRoadNode);
            }
        }

        
        
        Vector3 newDir = Quaternion.AngleAxis(Random.Range(-angleRandomness, angleRandomness), Vector3.up) * h.dir;
        h.dir = newDir;
        h.pos += newDir * nodeDistance;
        if (!NodeInBoundary(h.pos)) {
            h.disabled = true;
            disabledHeads++;
        }        
        h.prevNode = n;
        h.nodesSinceSplit++;
    }
    
    bool NodeInBoundary(Vector3 pos) {
        if (pos.x < 0 || pos.x > networkWidth || pos.z < 0 || pos.z > networkWidth) return false;
        return true;
    }
    
    void DisplayNode(RoadNode n) {
        Instantiate(testBall, n.pos, Quaternion.identity);
    }
    
    RoadNode FindClosestNode(HeadNode h) {
        float closest = 10000f;
        RoadNode ClosestRoadNode = null;

        List<RoadNode> nearNodes = nodeTree.GetNearRoadNodes(h.pos, nodeDistance);
        

        
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
            headNodes = newNodes;
            
        }

    }
    
    void SplitHeadNode(HeadNode h) {
        float direction = Random.value < 0.5f ? -1 : 1;
        Vector3 newDir = Quaternion.AngleAxis(splitAngle * direction, Vector3.up) * h.dir;
        HeadNode newHead = new HeadNode(h.prevNode.pos + newDir * nodeDistance, newDir, headCounter);
        if (Vector3.Distance(FindClosestNode(newHead).pos, newHead.pos) < nodeDistance * .9f) {
            newHead.disabled = true;
            return;
        }
        headCounter++;
        newHead.prevNode = h.prevNode;
        h.nodesSinceSplit = 0;
        h.ID = headCounter;
        AddHead(newHead);
    }
    
    Color GetMapColour(Vector3 pos, Texture2D map) {
        return map.GetPixelBilinear(pos.x / networkWidth, pos.z / networkWidth);
    }
    
    void CreateHead(Vector3 pos, Vector3 dir, RoadNode prevRoadNode = null) {
        headNodes.Add(new HeadNode(pos, dir, headCounter, prevRoadNode: prevRoadNode));
        headCounter++;
    }
    void AddHead(HeadNode h) {
        headNodes.Add(h);
        headCounter++;
    }
    
    void InitialiseNodes() {
        Vector3 center = new Vector3(networkWidth / 2f, 0, networkWidth / 2f);
        RoadNode node = new RoadNode(center);
        nodes.Add(node);
        DisplayNode(node);
        nodeTree.AddNodeToTree(node);
        CreateHead(center + Vector3.forward * nodeDistance, Vector3.forward, prevRoadNode:node);
        CreateHead(center + Vector3.forward * -nodeDistance, Vector3.back, prevRoadNode:node);
    }
}
