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
    public Node prevNode;
    public int nodesSinceSplit = 0;
    public bool disabled = false;
    public int ID = -1;

    public HeadNode(Vector3 p, Vector3 d, int id) {
        pos = p;
        dir = d;
        ID = id;

    }
}

public class Node {
    public Vector3 pos;
    public List<Node> connections = new List<Node>();
    public int headID = -1;
    public int ID;

    public Node(Vector3 p) {
        pos = p;
    }
}

public class RoadGenerator : MonoBehaviour {

    [SerializeField] private GameObject testBall;

    private List<HeadNode> headNodes = new List<HeadNode>();
    private List<Node> nodes = new List<Node>();

    private float lastTicked;
    [SerializeField] private float tickDelay;

    [SerializeField] private float splitAngle;
    [SerializeField] private float angleRandomness;

    [SerializeField] private float nodeDistance;

    private QuadTree nodeTree = new QuadTree(1024);

    private int headCounter = 0;
    private int nodeCounter = 0;
    private int disabledHeads = 0;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastTicked = Time.time;
        headNodes.Add(new HeadNode(Vector3.zero, Vector3.forward, headCounter));
        headCounter++;
    }

    // Update is called once per frame
    void Update()
    {
        if (lastTicked + tickDelay < Time.time && false) {
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

        //create new node
        Node n = new Node(h.pos);
        n.ID = nodeCounter;
        n.headID = h.ID;
        if (h.prevNode != null) {
            n.connections.Add(h.prevNode);
        }
        h.prevNode?.connections.Add(n);
        
        
        
        nodes.Add(n);
        DisplayNode(n);
        if (!(h.nodesSinceSplit < 1)) {
            Node closestNode = FindClosestNode(h);
            if (closestNode != null && Vector3.Distance(closestNode.pos, h.pos) < nodeDistance * 1.8f) {
                h.disabled = true;
                closestNode.connections.Add(n);
                n.connections.Add(closestNode);
            }
        }

        
        
        Vector3 newDir = Quaternion.AngleAxis(Random.Range(-angleRandomness, angleRandomness), Vector3.up) * h.dir;
        h.dir = newDir;
        h.pos += newDir * nodeDistance;
        h.prevNode = n;
        h.nodesSinceSplit++;
    }
    
    void DisplayNode(Node n) {
        Instantiate(testBall, n.pos, Quaternion.identity);
    }
    
    Node FindClosestNode(HeadNode h) {
        float closest = 10000f;
        Node closestNode = null;
        
        for (int i = 0; i < nodes.Count; i++) {
            Node node = nodes[i];
            if (node.headID != h.ID) {
                float dist = Vector3.Distance(h.pos, node.pos);
                if (closest > dist) {
                    closest = dist;
                    closestNode = nodes[i];
                }
            }
        }   
        return closestNode;
    }
    
    void DrawConnections() {
        for (int i = 0; i < nodes.Count; i++) {
            Node node = nodes[i];

            for (int j = 0; j < node.connections.Count; j++) {
                Node neighbour = node.connections[j];
                Vector3 pos1 = node.pos;
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
        HeadNode newHead = new HeadNode(h.prevNode.pos + newDir * nodeDistance, newDir, headCounter);

        if (Vector3.Distance(FindClosestNode(newHead).pos, newHead.pos) < nodeDistance * .9f) {
            //discard head
        }
        headCounter++;
        newHead.prevNode = h.prevNode;
        headNodes.Add(newHead);
        h.nodesSinceSplit = 0;
        h.ID = headCounter;
        headCounter++;
    }
    
}
