using EasyButtons;
using UnityEngine;

public class CCA2D : MonoBehaviour
{
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
    
    // Kit Code
    private Camera cam;

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


    private void Start()
    {
        cam = Camera.main;
        Reset();    
    }

    [Button]

    private void Reset()
    {
        readTex = CreateTexture(RenderTextureFormat.RFloat);
        writeTex = CreateTexture(RenderTextureFormat.RFloat);
        outTex = CreateTexture(RenderTextureFormat.ARGBFloat);

        stepKernel = cs.FindKernel("StepKernel");

        GPUResetKernel();
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
        cs.Dispatch(k, rez, rez, 1);
        SwapTex();
    }

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
        
        cs.Dispatch(stepKernel, rez, rez, 1);

        SwapTex();

        outMat.SetTexture("_UnlitColorMap", outTex);
    }

    private void SwapTex()
    {
        RenderTexture tmp = readTex;
        readTex = writeTex;
        writeTex = tmp;
    }
}
