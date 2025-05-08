using System.Collections.Generic;
using UnityEditor.TerrainTools;
using UnityEngine;

public class HeadNode {
    public Vector3 pos;
    public Vector3 dir;
    public Node prevNode;

    public HeadNode(Vector3 p, Vector3 d) {
        pos = p;
        dir = d;
    }
}

public class Node {
    public Vector3 pos;
    public List<Node> connections = new List<Node>();

    public Node(Vector3 p) {
        pos = p;
    }
}

public class RoadGenerator : MonoBehaviour {

    [SerializeField] private GameObject testBall;

    private List<HeadNode> headNodes = new List<HeadNode>();
    private List<Node> nodes = new List<Node>();

    private float lastTicked;
    private float tickDelay = 1.0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastTicked = Time.time;
        headNodes.Add(new HeadNode(Vector3.zero, Vector3.forward));
    }

    // Update is called once per frame
    void Update()
    {
        if (lastTicked + tickDelay < Time.time) {
            IterateGraph();
            lastTicked = Time.time;
        }
    }
    
    void IterateGraph() {
        for (int i = 0; i < headNodes.Count; i++) {
            HeadNode h = headNodes[i];
            
            if (Random.Range(0,5) == 1) {
                SplitHeadNode(h);
            }
            
            IterateHead(h);
        }
        DrawConnections();
    }
    
    void IterateHead(HeadNode h) {
        Node n = new Node(h.pos);
        if (h.prevNode != null) {
            n.connections.Add(h.prevNode);

        }
        h.prevNode?.connections.Add(n);
        nodes.Add(n);
        DisplayNode(n);
        h.pos += h.dir * 10;
        h.prevNode = n;
    }
    
    void DisplayNode(Node n) {
        Instantiate(testBall, n.pos, Quaternion.identity);
    }
    
    void DrawConnections() {
        for (int i = 0; i < nodes.Count; i++) {
            Node node = nodes[i];

            for (int j = 0; j < node.connections.Count; j++) {
                Node neighbour = node.connections[j];
                Vector3 pos1 = node.pos;
                Vector3 pos2 = neighbour.pos;
                Debug.DrawLine(pos1, pos2, Color.red, 1.0f);
            }
        }
    }

    
    void SplitHeadNode(HeadNode h) {
        Vector3 newDir = Quaternion.AngleAxis(45f, Vector3.up) * h.dir;
        HeadNode newHead = new HeadNode(h.prevNode.pos + newDir * 10, newDir);
        newHead.prevNode = h.prevNode;
        headNodes.Add(newHead);
    }
    
}
