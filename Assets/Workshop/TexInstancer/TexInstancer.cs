using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class TexInstancer : MonoBehaviour
{
    public Mesh mesh;
    public Material instanceMaterial;
    public Material inputMaterial;
    
    [Range(0, 10)]
    public float sizey = .5f;

    [Range(0, .5f)]
    public float size = .5f;

    [Range(0, .1f)]
    public float spacing = .1f;
    
    private ComputeBuffer argBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0};
    
    readonly Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 25);

    // Update is called once per frame
    void Update()
    {
        if (argBuffer != null)
        {    
            argBuffer.Release();
            argBuffer.Dispose();
        }

        Texture tex = inputMaterial.GetTexture("_UnlitColorMap");
        if (tex)
        {
            int rez = tex.width;
            
            argBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            instanceMaterial.SetTexture("tex", tex);

            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint) (rez * rez);
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            argBuffer.SetData(args);
            
            instanceMaterial.SetMatrix("mat", transform.localToWorldMatrix);
            instanceMaterial.SetVector("position", transform.position);
            instanceMaterial.SetInt("rez", rez);
            instanceMaterial.SetFloat("size", size);
            instanceMaterial.SetFloat("spacing", spacing);
            instanceMaterial.SetFloat("sizey", sizey);
            
            Graphics.DrawMeshInstancedIndirect(mesh, 0, instanceMaterial, bounds, argBuffer);
        
        }
    }

    private void OnApplicationQuit()
    {
        argBuffer.Release();
        argBuffer.Dispose();
    }
}
