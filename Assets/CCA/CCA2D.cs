using System;
using EasyButtons;
using UnityEngine;

public class CCA2D : MonoBehaviour
{
    // ------------------------------
    // Primary CCA Parameters
    // ------------------------------
    [Header("CCA Primary Params")]
    
    const int MAX_RANGE = 10;
    [Range(1, MAX_RANGE)]
    public int range = 1;
    
    const int MAX_DRAWRANGE = 32;
    [Range(1, MAX_DRAWRANGE)]
    public int drawRange = 1;

    const int MAX_THRESHOLD = 25;
    [Range(0, MAX_THRESHOLD)]
    public int threshold = 3;

    const int MAX_STATES = 20;
    [Range(0, MAX_STATES)]
    public int nstates = 3;

    public bool moore;
    
    // ------------------------------
    // Secondary CCA Parameters
    // ------------------------------
    [Header("CCA Secondary Params")]
    
    [Range(1, MAX_RANGE)]
    public int range2 = 1;
    
    [Range(1, MAX_DRAWRANGE)]
    public int drawRange2 = 1;
    
    [Range(0, MAX_THRESHOLD)]
    public int threshold2 = 3;
    
    [Range(0, MAX_STATES)]
    public int nstates2 = 3;

    public bool moore2;

    // ------------------------------
    // Global Parameters
    // ------------------------------
    [Header("Setup")]

    [Range(0, 2048)]
    public int rez = 8;

    [Range(0, 50)]
    public int stepsPerFrame = 0;

    [Range(1, 50)]
    public int stepMod = 1;

    public ComputeShader cs;

    public Material outMat;
    private RenderTexture outTex;

    private RenderTexture readTex;
    private RenderTexture writeTex;

    private int stepKernel;
    
    // ------------------------------
    // Camera for Mouse Control
    // ------------------------------
    private Camera cam;

    // ------------------------------
    // Start
    // ------------------------------
    private void Start()
    {
        cam = Camera.main;
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
    // Reset
    // ------------------------------
    [Button]
    private void Reset()
    {
        readTex = CreateTexture(RenderTextureFormat.RFloat);
        writeTex = CreateTexture(RenderTextureFormat.RFloat);
        outTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        stepKernel = cs.FindKernel("StepKernel");

        GPUResetKernel();
    }

    private void GPUResetKernel()
    {
        int k = cs.FindKernel("ResetKernel");
        cs.SetTexture(k, "writeTex", writeTex);

        cs.SetInt("range", range);
        cs.SetInt("drawRange", drawRange);
        cs.SetInt("threshold", threshold);
        cs.SetInt("nstates", nstates);
        cs.SetBool("moore", moore);

        cs.SetInt("rez", rez);
        cs.Dispatch(k, rez / 8, rez / 8, 1);
        SwapTex();
    }

    // ------------------------------
    // Randomize Parameters
    // ------------------------------
    [Button]
    public void RandomizeParams()
    {
        var rand = new System.Random();
        range = (int) (rand.NextDouble() * (MAX_RANGE - 1)) + 1;
        threshold = (int) (rand.NextDouble() * (MAX_THRESHOLD - 1)) + 1;
        nstates = (int) (rand.NextDouble() * (MAX_STATES - 2)) + 2;
        moore = rand.NextDouble() <= 0.5f;
        
        cs.SetInt("range", range);
        cs.SetInt("threshold", threshold);
        cs.SetInt("nstates", nstates);
        cs.SetBool("moore", moore);
    }
    
    public void RandomizeSecondaryParams()
    {
        var rand = new System.Random();
        range2 = (int) (rand.NextDouble() * (MAX_RANGE - 1)) + 1;
        threshold2 = (int) (rand.NextDouble() * (MAX_THRESHOLD - 1)) + 1;
        nstates2 = (int) (rand.NextDouble() * (MAX_STATES - 2)) + 2;
        moore2 = rand.NextDouble() <= 0.5f;
        
        cs.SetInt("range", range2);
        cs.SetInt("threshold", threshold2);
        cs.SetInt("nstates", nstates2);
        cs.SetBool("moore", moore2);
    }

    [Button]
    public void RandomizeSecondaryParamsWithNoise()
    {
        RandomizeSecondaryParams();
        AddNoise();
    }

    [Button]
    public void AddNoise()
    {
        int kernel = cs.FindKernel("SecondaryNoiseKernel");
        cs.SetTexture(kernel, "readTex", readTex);
        cs.SetTexture(kernel, "writeTex", writeTex);
        cs.Dispatch(kernel, rez / 8, rez / 8, 1);
    }

    [Button]
    public void SetPrimaryParams()
    {
        cs.SetInt("range", range);
        cs.SetInt("threshold", threshold);
        cs.SetInt("nstates", nstates);
        cs.SetBool("moore", moore);
    }
    
    [Button]
    public void SetSecondaryParams()
    {
        cs.SetInt("range", range2);
        cs.SetInt("threshold", threshold2);
        cs.SetInt("nstates", nstates2);
        cs.SetBool("moore", moore2);
    }

    [Button]
    public void ResetAndRandomize()
    {
        RandomizeParams();
        RandomizeColors();
        Reset();
    }
    
    [Button]
    public void RandomizeColors()
    {
        var rand = new System.Random(Time.frameCount);
        
        // Step 1: Create a gradient
        Gradient g = new Gradient();
        uint keycount = 8;
        GradientColorKey[] c = new GradientColorKey[keycount];
        GradientAlphaKey[] a = new GradientAlphaKey[keycount];
        // 2/1/19/n
        float hueMax = 1, hueMin = 0f, sMax = 2, sMin = 0, vMax = 1, vMin = 0;
        for (int i = 0; i < keycount; i++)
        {
            float h = (float)rand.NextDouble() * (hueMax - hueMin) + hueMin;
            float s = (float)rand.NextDouble() * (sMax - sMin) + sMin;
            float v = (float)rand.NextDouble() * (vMax - vMin) + vMin;
            Color nc = Color.HSVToRGB(h, s, v);
            c[i].color = nc;
            a[i].time = c[i].time = i * (1.0f / keycount);
            a[i].alpha = 1.0f;
        }
        g.SetKeys(c, a);
        
        // Step 2: Sample colors from the gradient
        // this gives us more "related" colors than just selecting random colors
        Vector4[] colors = new Vector4[nstates];
        for (int i = 0; i < nstates; i++)
        {
            float t = (float) rand.NextDouble();
            colors[i] = g.Evaluate(t);
        }
        cs.SetVectorArray("colors", colors);
    }

    // ------------------------------
    // Step
    // ------------------------------
    [Button]
    public void Step()
    {
        cs.SetTexture(stepKernel, "readTex", readTex);
        cs.SetTexture(stepKernel, "writeTex", writeTex);
        cs.SetTexture(stepKernel, "outTex", outTex);
        cs.SetInts("mouse", new int[]{
            (int) Mathf.Abs(Input.mousePosition.x  * rez / cam.pixelWidth) % rez,
            (int) Mathf.Abs(Input.mousePosition.y  * rez / cam.pixelHeight) % rez
        });
        
        cs.Dispatch(stepKernel, rez / 8, rez / 8, 1);

        SwapTex();

        outMat.SetTexture("_UnlitColorMap", outTex);
    }
    
    // ------------------------------
    // Helper Functions
    // ------------------------------
    private void SwapTex()
    {
        RenderTexture tmp = readTex;
        readTex = writeTex;
        writeTex = tmp;
    }
    
    protected RenderTexture CreateTexture(RenderTextureFormat format)
    {
        RenderTexture texture = new RenderTexture(rez, rez, 1, format);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.useMipMap = false;
        texture.Create();
        return texture;
    }
}
