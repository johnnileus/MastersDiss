using System.Collections.Generic;
using UnityEngine;

public class HeadNode {
    public Vector3 pos;
    public Vector3 dir;

    public HeadNode(Vector3 p, Vector3 d) {
        pos = p;
        dir = d;
    }
}

public class Node {
    public Vector3 pos;

    public Node(Vector3 p) {
        pos = p;
    }
}

public class RoadGenerator : MonoBehaviour {

    [SerializeField] private GameObject testBall;

    private HeadNode headNode;
    private List<Node> nodes = new List<Node>();

    private float lastTicked;
    private float tickDelay = 1.0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastTicked = Time.time;
        headNode = new HeadNode(Vector3.zero, Vector3.forward);
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
        Node n = new Node(headNode.pos);
        nodes.Add(n);
        DisplayNode(n);
        headNode.pos += headNode.dir * 10;
    }
    
    void DisplayNode(Node n) {
        Instantiate(testBall, n.pos, Quaternion.identity);
    }
    
}
