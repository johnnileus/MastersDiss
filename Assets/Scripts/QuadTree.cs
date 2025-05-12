using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class QuadNode {
    public Vector2 min;
    public Vector2 max;
    public int depth;
    public float width;
    public List<Vector3> corners = new List<Vector3>();

    public QuadNode(Vector2 minimum, int treeDepth, float w) {
        min = minimum;
        max = minimum + new Vector2(w, w);
        depth = treeDepth;
        width = w;
        corners.Add(new Vector3(min.x, 0, min.y));
        corners.Add(new Vector3(max.x, 0, min.y));
        corners.Add(new Vector3(min.x, 0, max.y));
        corners.Add(new Vector3(max.x, 0, max.y));
    }
    
    public void DrawNode() {
        Debug.DrawLine(corners[0], corners[1]);
        Debug.DrawLine(corners[0], corners[2]);
        Debug.DrawLine(corners[1], corners[3]);
        Debug.DrawLine(corners[2], corners[3]);
    }


}

public class QuadTree {
    public float width;

    public QuadNode root;

    public QuadTree(float w) {
        width = w;
        root = new QuadNode(new Vector2(0,0), 0, width);
    }
    
    public void DrawTree() {
        root.DrawNode();
    }
}