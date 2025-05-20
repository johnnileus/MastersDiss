using UnityEngine;

public class ComputeRoadGen : MonoBehaviour
{
    public ComputeShader computeShader;

    [SerializeField] private bool active;
    
    [SerializeField] private GameObject testBall;

    [SerializeField] private float gridSize;



    void Start() {
        if (!active) return;
        int count = 64000;
        
        ComputeBuffer buffer = new ComputeBuffer(count, sizeof(float) * 2);
        Vector2[] data = new Vector2[count];
        
        int kernelID = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernelID, "positions", buffer);
        computeShader.SetFloat("gridSize", gridSize);
        computeShader.SetFloat("seed", 1);
        
        computeShader.Dispatch(kernelID, count, 1, 1);

        buffer.GetData(data);

        for (int i = 0; i < count; i++)
        { 
            Debug.Log($"positions[{i}] = {data[i]}");
            DisplayNode(data[i]);
            
        }

        buffer.Release();
    }
    
    void DisplayNode(Vector2 pos) {
        Instantiate(testBall, new Vector3(pos.x, 0, pos.y), Quaternion.identity);
    }
}
