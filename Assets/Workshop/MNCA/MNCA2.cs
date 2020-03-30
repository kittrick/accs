using EasyButtons;
using UnityEngine;

public class MNCA2 : MonoBehaviour
{
    // ------------------------------
    // Primary CCA Parameters
    // ------------------------------
    [Header("MNCA2 Settings")]

    public Texture neighborhoodZero;
    public int minDeathZero = 0;
    public int maxDeathZero = 17;
    public int minLifeZero = 40;
    public int maxLifeZero = 42;

    public Texture neighborhoodOne;
    public int minLifeOne = 10;
    public int maxLifeOne = 13;

    public Texture neighborhoodTwo;
    public int minDeathTwo = 9;
    public int maxDeathTwo = 21;

    public Texture neighborhoodThree;
    public int minDeathThree = 78;
    public int maxDeathThree = 89;
    public int overflowDeathThree = 108;

    // ------------------------------
    // Global Parameters
    // ------------------------------
    [Header("Setup")]

    [Range(0, 2048)]
    public int rez = 8;

    [Range(0, 50)]
    public int stepsPerFrame = 1;

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
        
        cs.SetTexture(k, "neighborhoodZero", neighborhoodZero);
        cs.SetInt("minDeathZero", minDeathZero); 
        cs.SetInt("maxDeathZero", maxDeathZero); 
        cs.SetInt("minLifeZero", minLifeZero); 
        cs.SetInt("maxLifeZero", maxLifeZero);

        cs.SetTexture(k, "neighborhoodOne", neighborhoodOne);
        cs.SetInt("minLifeOne", minLifeOne); 
        cs.SetInt("maxLifeOne", maxLifeOne);

        cs.SetTexture(k, "neighborhoodTwo", neighborhoodTwo);
        cs.SetInt("minDeathTwo", minDeathTwo);
        cs.SetInt("maxDeathTwo", maxDeathTwo);

        cs.SetTexture(k, "neighborhoodThree", neighborhoodThree);
        cs.SetInt("minDeathThree", minDeathThree); 
        cs.SetInt("maxDeathThree", maxDeathThree); 
        cs.SetInt("overflowDeathThree", overflowDeathThree);

        cs.SetInt("rez", rez);
        cs.SetFloat("u_time", Time.time);
        cs.Dispatch(k, rez / 32, rez / 32, 1);
        SwapTex();
    }

    // ------------------------------
    // Step
    // ------------------------------
    [Button]
    public void Step()
    {
        cs.SetTexture(stepKernel, "neighborhoodZero", neighborhoodZero);
        cs.SetTexture(stepKernel, "neighborhoodOne", neighborhoodOne);
        cs.SetTexture(stepKernel, "neighborhoodTwo", neighborhoodTwo);
        cs.SetTexture(stepKernel, "neighborhoodThree", neighborhoodThree);
        
        cs.SetTexture(stepKernel, "readTex", readTex);
        cs.SetTexture(stepKernel, "writeTex", writeTex);
        cs.SetTexture(stepKernel, "outTex", outTex);
        
        cs.SetFloat("u_time", Time.time);

        cs.Dispatch(stepKernel, rez / 32, rez / 32, 1);

        SwapTex();

        writeMat.SetTexture("_UnlitColorMap", readTex);
        readMat.SetTexture("_UnlitColorMap", writeTex);
        outMat.SetTexture("_UnlitColorMap", outTex);
    }

    [Button]
    public void Randomize()
    {
        var rand = new System.Random();

        maxDeathZero = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minDeathZero = (int) (rand.NextDouble() * (maxDeathZero - 1)) + 1;
        maxLifeZero = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minLifeZero = (int) (rand.NextDouble() * (maxLifeZero - 1)) + 1;
        
        maxLifeOne = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minLifeOne = (int) (rand.NextDouble() * (maxLifeOne - 1)) + 1;
        
        maxDeathTwo = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minDeathTwo = (int) (rand.NextDouble() * (maxDeathTwo - 1)) + 1;

        maxDeathThree = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minDeathThree = (int) (rand.NextDouble() * (maxDeathThree - 1)) + 1;
        overflowDeathThree = (int) (rand.NextDouble() * (120 - 1)) + maxDeathThree;
    }

    [Button]
    public void ResetAndRandomize()
    {
        Randomize();
        Reset();
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
