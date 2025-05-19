using UnityEngine;

public class ComputeRoadGen : MonoBehaviour
{
    public ComputeShader computeShader;

    void Start()
    {
        int count = 64;
        
        // Create a buffer to hold ints
        ComputeBuffer buffer = new ComputeBuffer(count, sizeof(int));

        // Find kernel ID
        int kernelID = computeShader.FindKernel("CSMain");

        // Set the buffer in the shader
        computeShader.SetBuffer(kernelID, "resultBuffer", buffer);

        // Dispatch shader: (count / numthreads, 1, 1)
        computeShader.Dispatch(kernelID, count / 64, 1, 1);

        // Read back the data
        int[] data = new int[count];
        buffer.GetData(data);

        // Print it
        for (int i = 0; i < count; i++)
        {
            Debug.Log($"data[{i}] = {data[i]}");
        }

        buffer.Release();
    }
}