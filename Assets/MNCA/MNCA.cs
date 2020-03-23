using EasyButtons;
using UnityEngine;

public class MNCA : MonoBehaviour
{
    // ------------------------------
    // Primary CCA Parameters
    // ------------------------------
    [Header("MNCA Settings")]

    public Texture neighborhoodZero;
    public float minDeathZero = 0;
    public float maxDeathZero = 17;
    public float minLifeZero = 40;
    public float maxLifeZero = 42;
    public float deltaValueZero = 0.5f;
    
    public Texture neighborhoodOne;
    public float minDeathOne = 8;
    public float maxDeathOne = 10;
    public float minLifeOne = 10;
    public float maxLifeOne = 13;
    public float deltaValueOne = 0.5f;
    
    
    public Texture neighborhoodTwo;
    public float minDeathTwo = 9;
    public float maxDeathTwo = 21;
    public float minLifeTwo = 10;
    public float maxLifeTwo = 13;
    public float deltaValueTwo = 0.5f;

    public Texture neighborhoodThree;
    public float minDeathThree = 78;
    public float maxDeathThree = 89;
    public float minLifeThree = 78;
    public float maxLifeThree = 89;
    public float deltaValueThree = 0.5f;

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
        cs.SetFloat("minDeathZero", minDeathZero); 
        cs.SetFloat("maxDeathZero", maxDeathZero); 
        cs.SetFloat("minLifeZero", minLifeZero); 
        cs.SetFloat("maxLifeZero", maxLifeZero);
        cs.SetFloat("deltaValueZero", deltaValueZero);

        cs.SetTexture(k, "neighborhoodOne", neighborhoodOne);
        cs.SetFloat("minLifeOne", minLifeOne); 
        cs.SetFloat("maxLifeOne", maxLifeOne);
        cs.SetFloat("minDeathOne", minDeathOne); 
        cs.SetFloat("maxDeathOne", maxDeathOne);
        cs.SetFloat("deltaValueOne", deltaValueOne);
        
        cs.SetTexture(k, "neighborhoodTwo", neighborhoodTwo);
        cs.SetFloat("minDeathTwo", minDeathTwo);
        cs.SetFloat("maxDeathTwo", maxDeathTwo); 
        cs.SetFloat("minDeathTwo", minLifeTwo);
        cs.SetFloat("maxDeathTwo", maxLifeTwo);
        cs.SetFloat("deltaValueTwo", deltaValueTwo);
        
        cs.SetTexture(k, "neighborhoodThree", neighborhoodThree);
        cs.SetFloat("minDeathThree", minDeathThree); 
        cs.SetFloat("maxDeathThree", maxDeathThree); 
        cs.SetFloat("minDeathThree", minLifeThree); 
        cs.SetFloat("maxDeathThree", maxLifeThree);
        cs.SetFloat("deltaValueThree", deltaValueThree);

        cs.SetInt("rez", rez);
        cs.SetFloat("u_time", Time.time);
        cs.Dispatch(k, rez / 8, rez / 8, 1);
        SwapTex();
    }

    // ------------------------------
    // Step
    // ------------------------------
    [Button]
    public void Step()
    {
        cs.SetFloat("deltaValueZero", deltaValueZero);
        cs.SetFloat("deltaValueOne", deltaValueOne);
        cs.SetFloat("deltaValueTwo", deltaValueTwo);
        cs.SetFloat("deltaValueThree", deltaValueThree);
        cs.SetTexture(stepKernel, "neighborhoodZero", neighborhoodZero);
        cs.SetTexture(stepKernel, "neighborhoodOne", neighborhoodOne);
        cs.SetTexture(stepKernel, "neighborhoodTwo", neighborhoodTwo);
        cs.SetTexture(stepKernel, "neighborhoodThree", neighborhoodThree);
        
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

    [Button]
    public void Randomize()
    {
        var rand = new System.Random();
        deltaValueZero = Random.Range(0.0f, 1.0f);
        deltaValueOne = Random.Range(0.0f, 1.0f);
        deltaValueTwo = Random.Range(0.0f, 1.0f);
        deltaValueThree = Random.Range(0.0f, 1.0f);
        
        maxDeathZero = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minDeathZero = (int) (rand.NextDouble() * (maxDeathZero - 1)) + 1;
        maxLifeZero = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minLifeZero = (int) (rand.NextDouble() * (maxLifeZero - 1)) + 1;
        
        maxDeathOne = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minDeathOne = (int) (rand.NextDouble() * (maxDeathOne - 1)) + 1;
        maxLifeOne = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minLifeOne = (int) (rand.NextDouble() * (maxLifeOne - 1)) + 1;
        
        maxDeathTwo = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minDeathTwo = (int) (rand.NextDouble() * (maxDeathTwo - 1)) + 1;
        maxLifeTwo = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minLifeTwo = (int) (rand.NextDouble() * (maxLifeTwo - 1)) + 1;
        
        maxDeathThree = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minDeathThree = (int) (rand.NextDouble() * (maxDeathThree - 1)) + 1;
        maxLifeThree = (int) (rand.NextDouble() * (120 - 1)) + 1;
        minLifeThree = (int) (rand.NextDouble() * (maxLifeThree - 1)) + 1;
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
