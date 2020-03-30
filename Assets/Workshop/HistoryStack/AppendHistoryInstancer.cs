using System.Collections;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

public class AppendHistoryInstancer : MonoBehaviour
{
    public ComputeShader compute;
    public Mesh mesh;
    public Material material;
    public Material inputMaterial;
    
    [Range(0, 10)]
    public float size = .5f;

    [Range(0, .5f)]
    public float spacing = .5f;

    [Range(8, 1024)]
    public int height = 64;

    private ComputeBuffer voxelBuffer;
    private ComputeBuffer argBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0};
    readonly Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 25);
    private int width;
    private int index = 0;

    [Button]
    public void Reset()
    {
        Texture tex = inputMaterial.GetTexture("_UnlitColorMap");
        if (tex)
        {
            width = tex.width;
            if (voxelBuffer != null)
            {
                voxelBuffer.Release();
            }
            voxelBuffer = new ComputeBuffer(width * height * width, sizeof(float) * 6, ComputeBufferType.Append);
            voxelBuffer.SetCounterValue(0);
            
            if (argBuffer != null)
            {
                argBuffer.Release();
            }
            argBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        index = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (voxelBuffer == null || index >= height)
        {
            Reset();
        }

        Texture tex = inputMaterial.GetTexture("_UnlitColorMap");
        if (tex)
        {
            width = tex.width;
            
            // 1. Build history
            int k = compute.FindKernel("WriteHistory");
            compute.SetTexture(k, "inTex", tex);
            compute.SetBuffer(k,"voxelBuffer", voxelBuffer);
            compute.SetInt("index", index);
            compute.Dispatch(k, width / 32, width / 32, 1);
            index++;
            
            // 2. Render
            args[0] = mesh.GetIndexCount(0);
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            
            argBuffer.SetData(args);
            
            ComputeBuffer.CopyCount(voxelBuffer, argBuffer, sizeof(int));
            /*
            //////////////////
            int[] count = new int[1];
            ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.IndirectArguments);
            //instanceBuffer.SetCounterValue(100);
            ComputeBuffer.CopyCount(voxelBuffer, countBuffer, 0);
            countBuffer.GetData(count);
            Debug.Log($"Count: {count[0]}");
            countBuffer.Release();
            //////////////////
            */

            material.SetBuffer("voxelBuffer", voxelBuffer);
            material.SetInt("width", width);
            material.SetInt("height", height);
            material.SetFloat("size", size);
            material.SetFloat("spacing", spacing);
            material.SetVector("position", transform.position);
            material.SetMatrix("mat", transform.localToWorldMatrix);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argBuffer);
        
        }
    }
}
