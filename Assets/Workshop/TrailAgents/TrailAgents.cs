using System;
using System.Collections.Generic;
using EasyButtons;
using UnityEngine;

[ExecuteAlways]
public class TrailAgents : MonoBehaviour
{
    // ------------------------------
    // Primary CCA Parameters
    // ------------------------------
    [Header("Trail Agents Settings")]
    [Range(1, 50000)]
    public int agentsCount = 1;

    private ComputeBuffer agentsBuffer;

    // ------------------------------
    // Global Parameters
    // ------------------------------
    [Header("Setup")]

    [Range(8, 2048)]
    public int rez = 512;

    [Range(0, 50)]
    public int stepsPerFrame = 1;

    [Range(1, 50)]
    public int stepMod = 1;

    public ComputeShader cs;
    public Material outMat;
    
    private RenderTexture outTex;
    private RenderTexture readTex;
    private RenderTexture writeTex;

    private int agentsDebugKernel;
    private int moveAgentsKernel;
    
    protected List<ComputeBuffer> buffers;
    protected List<RenderTexture> textures;

    protected int stepn = -1;

    
    // ------------------------------
    // Reset
    // ------------------------------
    [Button]
    private void Reset()
    {
        Release();
        agentsDebugKernel = cs.FindKernel("AgentsDebugKernel");
        moveAgentsKernel = cs.FindKernel("MoveAgentsKernel");
        
        readTex = CreateTexture(rez, FilterMode.Point);
        writeTex = CreateTexture(rez, FilterMode.Point);
        outTex = CreateTexture(rez, FilterMode.Point);
        
        agentsBuffer = new ComputeBuffer(agentsCount, sizeof(float) * 4);
        buffers.Add(agentsBuffer);
        
        GPUResetKernel();
        Render();
    }
    
    private void GPUResetKernel()
    {
        int kernel;
        
        cs.SetInt("rez", rez);
        cs.SetInt("time", Time.frameCount);

        kernel = cs.FindKernel("ResetTextureKernel");
        cs.SetTexture(kernel, "writeTex", writeTex);
        cs.Dispatch(kernel, rez, rez, 1);
        
        cs.SetTexture(kernel, "writeTex", readTex);
        cs.Dispatch(kernel, rez, rez, 1);

        kernel = cs.FindKernel("ResetAgentsKernel");
        cs.SetBuffer(kernel, "agentsBuffer", agentsBuffer);
        cs.Dispatch(kernel, agentsCount, 1,1);
    }
    
    // ------------------------------
    // Start
    // ------------------------------
    private void Start()
    {
        Reset();    
    }

    // ------------------------------
    // Update
    // ------------------------------
    private void Update()
    {
        if(Time.frameCount % stepMod == 0)
        {
            for (int i = 0; i < stepsPerFrame; i++)
            {
                Step();
            }
        }
    }

    // ------------------------------
    // Step
    // ------------------------------
    [Button]
    public void Step()
    {
        stepn += 1;
        GPUMoveAgentsKernel();

        Render();
    }

    private void GPUMoveAgentsKernel()
    {
        cs.SetBuffer(moveAgentsKernel, "agentsBuffer", agentsBuffer);
        cs.SetTexture(moveAgentsKernel, "readTex", readTex);
        
        cs.Dispatch(moveAgentsKernel, agentsCount, 1, 1);
    }

    public void Render()
    {
        GPUAgentsDebugKernel();
        
        outMat.SetTexture("_UnlitColorMap", outTex);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneView.RepaintAll();
        }
    }

    private void GPUAgentsDebugKernel()
    {
        cs.SetBuffer(agentsDebugKernel, "agentsBuffer", agentsBuffer);
        cs.SetTexture(agentsDebugKernel, "outTex", outTex);
        
        cs.Dispatch(agentsDebugKernel, agentsCount, 1, 1);
    }

    // ------------------------------
    // Helper Functions
    // ------------------------------
    public void Release()
    {
        if (buffers != null)
        {
            foreach (ComputeBuffer buffer in buffers)
            {
                if (buffer != null)
                {
                    buffer.Release();
                }
            }
        }
        
        buffers = new List<ComputeBuffer>();

        if (textures != null)
        {
            foreach (RenderTexture tex in textures)
            {
                if (tex != null)
                {
                    tex.Release();
                }
            }
        }
        
        textures = new List<RenderTexture>();
    }

    private void OnDestroy()
    {
        Release();
    }
    
    private void OnEnable()
    {
        Release();
    }
    
    private void OnDisable()
    {
        Release();
    }

    protected RenderTexture CreateTexture(int r, FilterMode filterMode)
    {
        RenderTexture texture = new RenderTexture(r, r, 1, RenderTextureFormat.ARGBFloat);

        texture.name = "out";
        texture.enableRandomWrite = true;
        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        texture.volumeDepth = 1;
        texture.filterMode = filterMode;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.autoGenerateMips = false;
        texture.useMipMap = false;
        texture.Create();
        textures.Add(texture);
        
        return texture;
    }
}
