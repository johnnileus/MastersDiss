using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class QuadNode {
    private Vector2 min;
    private Vector2 max;
    private Vector2 center;
    private int depth;
    private float width;
    private List<Vector3> corners = new List<Vector3>(); // world coordinates of each corner

    private bool isLeaf = true;
    private List<QuadNode> children;
    private List<RoadNode> roadNodes = new List<RoadNode>();
    private int maxRoadNodes;

    public QuadNode(Vector2 minimum, int treeDepth, float w, int maxNodes) {
        min = minimum;
        max = minimum + new Vector2(w, w);
        center = minimum + new Vector2(w / 2, w / 2);
        depth = treeDepth;
        width = w;
        corners.Add(new Vector3(min.x, 0, min.y));
        corners.Add(new Vector3(max.x, 0, min.y));
        corners.Add(new Vector3(min.x, 0, max.y));
        corners.Add(new Vector3(max.x, 0, max.y));

        maxRoadNodes = maxNodes;
    }
    
    public void DrawNode() {
    
        if (isLeaf) {
            Color col = Color.white;
            if (roadNodes.Count != 0) {
                col = Color.red;
            }
            Debug.DrawLine(corners[0], corners[1], col);
            Debug.DrawLine(corners[0], corners[2], col);
            Debug.DrawLine(corners[1], corners[3], col);
            Debug.DrawLine(corners[2], corners[3], col);
        } else {
            foreach (var child in children) {
                child.DrawNode();
            }
        }

    }
    
    public void SplitNode() {
        isLeaf = false;
        children = new List<QuadNode> {
            new(min, depth + 1, width / 2, maxRoadNodes),
            new(min + new Vector2(width / 2, 0), depth + 1, width / 2, maxRoadNodes),
            new(min + new Vector2(0, width/2), depth+1, width/2, maxRoadNodes),
            new(min + new Vector2(width/2, width/2), depth+1, width/2, maxRoadNodes)
        };

        foreach (var node in roadNodes) {
            if (node.pos.x < center.x && node.pos.z < center.y) {
                children[0].roadNodes.Add(node);
            } else if (node.pos.x > center.x && node.pos.z < center.y) {
                children[1].roadNodes.Add(node);
            } else if (node.pos.x < center.x && node.pos.z > center.y) {
                children[2].roadNodes.Add(node);
            } else if (node.pos.x > center.x && node.pos.z > center.y) {
                children[3].roadNodes.Add(node);
            }
            
        }
        roadNodes.Clear();
        
    }
    
    public void InsertNode(RoadNode node) {
        if (isLeaf) {
            roadNodes.Add(node);
            if (roadNodes.Count >= maxRoadNodes) {
                SplitNode();
            }
        } else {
            if (node.pos.x < center.x && node.pos.z < center.y) {
                children[0].InsertNode(node);
            } else if (node.pos.x > center.x && node.pos.z < center.y) {
                children[1].InsertNode(node);
            } else if (node.pos.x < center.x && node.pos.z > center.y) {
                children[2].InsertNode(node);
            } else if (node.pos.x > center.x && node.pos.z > center.y) {
                children[3].InsertNode(node);
            }

        }
    }
    
    public void GetNearRoadNodes(Vector3 pos, float r, List<RoadNode> nodes) {
        if (CircleIntersection(pos, r)) {
            if (isLeaf) {
                foreach (var node in roadNodes) {
                    nodes.Add(node);
                }
            } else {
                foreach (var child in children) {
                    child.GetNearRoadNodes(pos, r, nodes);
                }
            }
        }
    }
    
    public bool CircleIntersection(Vector3 pos, float r) {
        Vector3 cp = new Vector3(Mathf.Clamp(pos.x, min.x, max.x), 0,
                                Mathf.Clamp(pos.z, min.y, max.y));
        float d = Vector3.Distance(cp, pos);
        return !(d > r);
    }
}

public class QuadTree {
    public float width;
    public QuadNode root;
    private int maxRoadNodes = 8;

    public QuadTree(float w) {
        width = w;
        root = new QuadNode(new Vector2(0,0), 0, width, maxRoadNodes);
    }
    
    public void DrawTree() {
        root.DrawNode();
    }
    
    public void AddNodeToTree(RoadNode node) {
        root.InsertNode(node);
    }
    
    public List<RoadNode> GetNearRoadNodes(Vector3 pos, float r) {
        List<RoadNode> nodes = new List<RoadNode>();

        root.GetNearRoadNodes(pos, r, nodes);

        return nodes;
    }
}