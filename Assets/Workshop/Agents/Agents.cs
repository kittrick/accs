using EasyButtons;
using UnityEngine;

public class Agents : MonoBehaviour
{
    // ------------------------------
    // Primary CCA Parameters
    // ------------------------------
    [Header("Agents Settings")]

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
