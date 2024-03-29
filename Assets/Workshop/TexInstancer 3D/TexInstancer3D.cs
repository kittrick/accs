﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexInstancer3D : MonoBehaviour
{
    public Mesh mesh;
    public Material instanceMaterial;
    public CCA3D cca;

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

        Texture tex = cca.outTex;
        if (tex)
        {
            int rez = tex.width;
            
            argBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            instanceMaterial.SetTexture("tex", tex);

            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint) (rez * rez * rez);
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
            argBuffer.SetData(args);
            
            instanceMaterial.SetMatrix("mat", transform.localToWorldMatrix);
            instanceMaterial.SetVector("position", transform.position);
            instanceMaterial.SetInt("rez", rez);
            instanceMaterial.SetFloat("size", size);
            instanceMaterial.SetFloat("spacing", spacing);

            Graphics.DrawMeshInstancedIndirect(mesh, 0, instanceMaterial, bounds, argBuffer);
        
        }
    }
}
