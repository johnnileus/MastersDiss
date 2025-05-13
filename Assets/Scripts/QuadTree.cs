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
    public List<QuadNode> children;

    public QuadNode(Vector2 minimum, int treeDepth, float w) {
        min = minimum;
        max = minimum + new Vector2(w, w);
        center = minimum + new Vector2(w / 2, w / 2);
        depth = treeDepth;
        width = w;
        corners.Add(new Vector3(min.x, 0, min.y));
        corners.Add(new Vector3(max.x, 0, min.y));
        corners.Add(new Vector3(min.x, 0, max.y));
        corners.Add(new Vector3(max.x, 0, max.y));
    }
    
    public void DrawNode() {
    
        if (isLeaf) {
            Debug.DrawLine(corners[0], corners[1]);
            Debug.DrawLine(corners[0], corners[2]);
            Debug.DrawLine(corners[1], corners[3]);
            Debug.DrawLine(corners[2], corners[3]);
        } else {
            foreach (var child in children) {
                child.DrawNode();
            }
        }

    }
    
    public void SplitNode() {
        isLeaf = false;
        children = new List<QuadNode> {
            new(min, depth + 1, width / 2),
            new(min + new Vector2(width / 2, 0), depth + 1, width / 2),
            new(min + new Vector2(0, width/2), depth+1, width/2),
            new(min + new Vector2(width/2, width/2), depth+1, width/2)
        };
    }
    

}

public class QuadTree {
    public float width;

    public QuadNode root;

    public QuadTree(float w) {
        width = w;
        root = new QuadNode(new Vector2(0,0), 0, width);
        root.SplitNode();
        root.children[0].SplitNode();
    }
    
    public void DrawTree() {
        root.DrawNode();
    }
    
    public void AddNode(RoadNode node) {
        //TODO
    }
}