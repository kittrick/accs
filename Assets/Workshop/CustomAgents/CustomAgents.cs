using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EasyButtons;
using UnityEngine;

public class CustomAgents : MonoBehaviour
{
    [Header("Texture Size")]
    [Range(32, 2056)]
    public int width = 32;
    [Range(32, 2056)]
    public int height = 32;

    [Header("Compute Shader")]
    public ComputeShader cs;
    private ComputeBuffer particleBuffer;

    [Header("Particle Info")]
    [Range(64,1000000)]
    public int particleCount = 64;
    public float minMass = 1f;
    public float maxMass = 10f;

    [Header("Environment Info")]
    public float drag;

    // Mouse Info
    public GameObject interactivePlane;
    public float repulsionStrength = 3000f;
    [Range(0,1)]
    public float decayFactor = 0.99f;
    public float noiseStrength = 5f;
    private Vector2 mousePosition;
    private Camera cam;
    
    [Header("Output Material")]
    public Material outMat;
    private RenderTexture readTex;
    private RenderTexture writeTex;
    private RenderTexture debugTex;
    private RenderTexture outTex;
    
    // Kernels
    private int StepKernel, ClearKernel, RenderKernel;
    
    // Buffers and Textures
    protected List<ComputeBuffer> buffers;
    protected List<RenderTexture> textures;
    
    // Particle Struct
    struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
        public Vector2 acceleration;
        public Vector4 color;
        public float particleMass;
    }
    
    // Runs at start
    private void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        // Setup everything for the compute shader
        Reset();
    }
    
    // Clean up and run
    private void Reset()
    {
        Release();
        
        StepKernel = cs.FindKernel("StepKernel");
        ClearKernel = cs.FindKernel("ClearKernel");
        RenderKernel = cs.FindKernel("RenderKernel");

        readTex = CreateTexture(width, height);
        writeTex = CreateTexture(width, height);
        outTex = CreateTexture(width, height);
        debugTex = CreateTexture(width, height);

        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 11);
        buffers.Add(particleBuffer);
        
        GPUResetKernel();
        Render();
    }
    
    // Reset
    private void GPUResetKernel()
    {
        int kernel;

        StepKernel = cs.FindKernel("StepKernel");
        RenderKernel = cs.FindKernel("RenderKernel");
        
        cs.SetInt("width", width);
        cs.SetInt("height", height);
        cs.SetFloat("time", Time.time);
        cs.SetFloat("minMass", minMass);
        cs.SetFloat("maxMass", maxMass);
        cs.SetFloat("drag", drag);

        kernel = cs.FindKernel("ResetTextureKernel");
        cs.SetTexture(kernel, "writeTex", writeTex);
        cs.Dispatch(kernel, width / 32, height / 32, 1);
        
        cs.SetTexture(kernel, "writeTex", readTex);
        cs.Dispatch(kernel, width / 32, height / 32, 1);

        kernel = cs.FindKernel("ResetParticlesKernel");
        cs.SetBuffer(kernel, "particleBuffer", particleBuffer);
        cs.Dispatch(kernel, particleCount / 64, 1,1);
    }

    // Runs every frame
    private void Update()
    {
        // Run the step function
        Step();
    }
    
    // ------------------------------------------------------------
    // Main Animator Function
    // ------------------------------------------------------------
    private void Step()
    {
        HandleInput();
        
        cs.SetFloat("time", Time.time);
        cs.SetFloat("drag", drag);
        cs.SetVector("mousePosition", mousePosition);
        cs.SetFloat("repulsionStrength", repulsionStrength);
        cs.SetFloat("noiseStrength", noiseStrength);
        GPUClearKernel();
        GPUStepKernel();
        Render();
    }
    
    // ------------------------------------------------------------
    // Send Mouse Coords
    // ------------------------------------------------------------
    void HandleInput()
    {
        if (!Input.GetMouseButton(0))
        {
            mousePosition.x = mousePosition.y = 0;
            return;
        }

        RaycastHit hit;
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity))
        {
            return;
        }

        if (hit.transform != interactivePlane.transform)
        {
            return;
        }

        mousePosition = hit.textureCoord * new Vector2(width, height);
    }
    
    // ------------------------------------------------------------
    // Actual Render to OutTex
    // ------------------------------------------------------------
    private void Render()
    {
        GPURenderKernel();
        
        // For Unlit
        //outMat.SetTexture("_UnlitColorMap", outTex);
        
        // For Lit
        outMat.SetTexture("_BaseColorMap", outTex);
        outMat.SetTexture("_EmissiveColorMap", outTex);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneView.RepaintAll();
        }
    }
    
    // ------------------------------------------------------------
    // Compute agent positions on GPU
    // ------------------------------------------------------------
    private void GPUStepKernel()
    {
        cs.SetBuffer(StepKernel, "particleBuffer", particleBuffer);
        cs.SetTexture(StepKernel, "writeTex", writeTex);
        cs.SetTexture(StepKernel, "readTex", readTex);
        cs.SetTexture(StepKernel, "debugTex", debugTex);
        
        cs.Dispatch(StepKernel, particleCount / 64, 1, 1);
    }
    
    // ------------------------------------------------------------
    // Clear the stage
    // ------------------------------------------------------------
    private void GPUClearKernel()
    {
        cs.SetFloat("decayFactor", decayFactor);
        cs.SetTexture(ClearKernel, "writeTex", writeTex);
        cs.SetTexture(ClearKernel, "readTex", readTex);
        cs.SetTexture(ClearKernel, "debugTex", debugTex);
        
        cs.Dispatch(ClearKernel, width / 32, height /32, 1);
    }

    // ------------------------------------------------------------
    // Render texture on GPU
    // ------------------------------------------------------------
    private void GPURenderKernel()
    {
        cs.SetTexture(RenderKernel, "readTex", readTex);
        cs.SetTexture(RenderKernel, "writeTex", writeTex);
        cs.SetTexture(RenderKernel, "outTex", outTex);
        cs.SetTexture(RenderKernel, "debugTex", debugTex);
        
        cs.Dispatch(RenderKernel, width / 32, height / 32, 1);
    }
    
    // ------------------------------------------------------------
    // Helper Functions
    // ------------------------------------------------------------

    // Create new RenderTexture
    protected RenderTexture CreateTexture(int width, int height)
    {
        RenderTexture texture = new RenderTexture(width, height, 1, RenderTextureFormat.ARGBFloat);

        texture.name = "out";
        texture.enableRandomWrite = true;
        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        texture.volumeDepth = 1;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.autoGenerateMips = false;
        texture.useMipMap = false;
        texture.Create();
        textures.Add(texture);
        
        return texture;
    }
    
    // Swap Textures
    private void SwapTex()
    {
        RenderTexture tmp = readTex;
        readTex = writeTex;
        writeTex = tmp;
    }
    
    // Release textures and buffers
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
}
