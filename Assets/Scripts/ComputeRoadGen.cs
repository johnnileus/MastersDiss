using UnityEngine;
using System.Runtime.InteropServices;

public class ComputeRoadGen : MonoBehaviour
{
    public ComputeShader computeShader;

    [SerializeField] private bool active;
    
    [SerializeField] private GameObject testBall;

    [SerializeField] private float gridSize;

    [StructLayout(LayoutKind.Sequential)]
    struct Node {
        public Vector2 pos;
        public int ID;
        public int branchID;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct Connection {
        public int startID;
        public int endID;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct HeadNode {
        public Vector2 pos;
        public int ID;
    }

    void Start() {
        if (!active) return;
        int count = 640;
        
        ComputeBuffer nodeBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Node)));
        ComputeBuffer connectionBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Connection)));
        ComputeBuffer headBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(HeadNode)));

        
        int kernelID = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelID, "nodes", nodeBuffer);
        computeShader.SetBuffer(kernelID, "connections", connectionBuffer);
        computeShader.SetBuffer(kernelID, "headNodes", headBuffer);
        
        computeShader.SetFloat("gridSize", gridSize);
        computeShader.SetFloat("seed", 1);
        
        computeShader.Dispatch(kernelID, count, 1, 1);


        Node[] nodes = new Node[count];
        Connection[] conns = new Connection[count];
        nodeBuffer.GetData(nodes);
        connectionBuffer.GetData(conns);

        for (int i = 0; i < count; i++) {
            Vector2 p1 = nodes[conns[i].startID].pos;
            Vector2 p2 = nodes[conns[i].endID].pos;
            DrawConnection(p1, p2);
        }
        
        for (int i = 0; i < count; i++) {
            DisplayNode(nodes[i].pos);
        }
        

        nodeBuffer.Release();
        connectionBuffer.Release();
        headBuffer.Release();
    }
    
    void DisplayNode(Vector2 pos) {
        Instantiate(testBall, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
    }
    
    void DrawConnection(Vector2 p1, Vector2 p2) {
        Debug.DrawLine(new Vector3(p1.x, 0, p1.y), new Vector3(p2.x, 0, p2.y), Color.red);
    }
}
