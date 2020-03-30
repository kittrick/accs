using System;
using EasyButtons;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class CCARGB : MonoBehaviour
{
    // ------------------------------
    // Primary CCA Parameters
    // ------------------------------
    [Header("Simple CCA Settings")]
    
    public bool linkSettings = false;
    public bool randomizeColors = true;
    
    const int MAX_RANGE = 10;
    [Range(1, MAX_RANGE)]
    public int rangeR = 1;
    [Range(1, MAX_RANGE)]
    public int rangeG = 1;
    [Range(1, MAX_RANGE)]
    public int rangeB = 1;

    const int MAX_THRESHOLD = 25;
    [Range(0, MAX_THRESHOLD)]
    public int thresholdR = 3;
    [Range(0, MAX_THRESHOLD)]
    public int thresholdG = 3;
    [Range(0, MAX_THRESHOLD)]
    public int thresholdB = 3;

    const int MAX_STATES = 20;
    [Range(0, MAX_STATES)]
    public int nstatesR = 3;
    [Range(0, MAX_STATES)]
    public int nstatesG = 3;
    [Range(0, MAX_STATES)]
    public int nstatesB = 3;

    public bool mooreR;
    public bool mooreG;
    public bool mooreB;

    // ------------------------------
    // Global Parameters
    // ------------------------------
    [Header("Setup")]

    [Range(0, 2048)]
    public int rez = 8;
    
    const int MAX_COLORS = 8;
    [Range(0, MAX_COLORS)]
    public int colorCount = 8;

    [Range(0, 50)]
    public int stepsPerFrame = 0;

    [Range(1, 50)]
    public int stepMod = 1;

    public ComputeShader cs;

    public Material readMat;
    public Material writeMat;
    public Material outMat;
    
    private RenderTexture outTex;
    private RenderTexture readTex;
    private RenderTexture writeTex;

    private int stepKernel;

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
    // Reset
    // ------------------------------
    [Button]
    private void Reset()
    {
        readTex = CreateTexture(RenderTextureFormat.ARGBFloat);
        writeTex = CreateTexture(RenderTextureFormat.ARGBFloat);
        outTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        stepKernel = cs.FindKernel("StepKernel");

        GPUResetKernel();
    }

    private void GPUResetKernel()
    {
        int k = cs.FindKernel("ResetKernel");
        cs.SetTexture(k, "writeTex", writeTex);
        cs.SetFloat("u_time", Time.time);
        cs.SetBool("randomizeColors", randomizeColors);

        cs.SetInt("rangeR", rangeR);
        cs.SetInt("thresholdR", thresholdR);
        cs.SetInt("nstatesR", nstatesR);
        cs.SetBool("mooreR", mooreR);
        
        cs.SetInt("rangeG", rangeR);
        cs.SetInt("thresholdG", thresholdR);
        cs.SetInt("nstatesG", nstatesR);
        cs.SetBool("mooreG", mooreR);
        
        cs.SetInt("rangeB", rangeB);
        cs.SetInt("thresholdB", thresholdB);
        cs.SetInt("nstatesB", nstatesB);
        cs.SetBool("mooreB", mooreB);

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
        rangeR = (int) (rand.NextDouble() * (MAX_RANGE - 1)) + 1;
        thresholdR = (int) (rand.NextDouble() * (MAX_THRESHOLD - 1)) + 1;
        nstatesR = (int) (rand.NextDouble() * (MAX_STATES - 2)) + 2;
        mooreR = rand.NextDouble() <= 0.5f;
        
        cs.SetInt("rangeR", rangeR);
        cs.SetInt("thresholdR", thresholdR);
        cs.SetInt("nstatesR", nstatesR);
        cs.SetBool("mooreR", mooreR);
        
        if(linkSettings) rand = new System.Random();
        
        rangeG = (int) (rand.NextDouble() * (MAX_RANGE - 1)) + 1;
        thresholdG = (int) (rand.NextDouble() * (MAX_THRESHOLD - 1)) + 1;
        nstatesG = (int) (rand.NextDouble() * (MAX_STATES - 2)) + 2;
        mooreG = rand.NextDouble() <= 0.5f;
        
        cs.SetInt("rangeG", rangeG);
        cs.SetInt("thresholdG", thresholdG);
        cs.SetInt("nstatesG", nstatesG);
        cs.SetBool("mooreG", mooreG);
        
        if(linkSettings) rand = new System.Random();
        
        rangeB = (int) (rand.NextDouble() * (MAX_RANGE - 1)) + 1;
        thresholdB = (int) (rand.NextDouble() * (MAX_THRESHOLD - 1)) + 1;
        nstatesB = (int) (rand.NextDouble() * (MAX_STATES - 2)) + 2;
        mooreB = rand.NextDouble() <= 0.5f;

        cs.SetInt("rangeB", rangeB);
        cs.SetInt("thresholdB", thresholdB);
        cs.SetInt("nstatesB", nstatesB);
        cs.SetBool("mooreB", mooreB);
        cs.SetInt("colorCount", colorCount);
        
        if(linkSettings) rand = new System.Random();

        colorCount = (int) (rand.NextDouble() * (MAX_COLORS - 2)) + 2;
    }
    
    [Button]
    public void RandomizeColors()
    {
        var rand = new System.Random(Time.frameCount);
        colorCount = (int) (rand.NextDouble() * (MAX_COLORS - 2)) + 2;
        
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
        Vector4[] colors = new Vector4[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            float t = (float) rand.NextDouble();
            colors[i] = g.Evaluate(t);
        }
        cs.SetInt("colorCount", colorCount);
        cs.SetVectorArray("colors", colors);
    }

    [Button]
    public void ResetAndRandomize()
    {
        RandomizeParams();
        RandomizeColors();
        Reset();
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
        cs.SetFloat("u_time", Time.time);

        cs.Dispatch(stepKernel, rez / 8, rez / 8, 1);

        SwapTex();

        writeMat.SetTexture("_UnlitColorMap", readTex);
        readMat.SetTexture("_UnlitColorMap", writeTex);
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
