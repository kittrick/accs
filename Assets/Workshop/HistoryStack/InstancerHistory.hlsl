//////////////////////////////////////////////////
// Transform Vars from Parent
//////////////////////////////////////////////////
float4x4 mat;
float3 position;

//////////////////////////////////////////////////
// Texture to grid vars
//////////////////////////////////////////////////
struct Voxel {
    float3 position;
    float4 color;
};

StructuredBuffer<Voxel> voxelBuffer;

int width;
int height;
float size;
float spacing;

//////////////////////////////////////////////////
// GPU Instanced Vertex Shader
//////////////////////////////////////////////////

PackedVaryingsType InstancedVert(AttributesMesh inputMesh, uint instanceID : SV_InstanceID)
{
    Voxel v = voxelBuffer[instanceID];
    

	// Get mesh vertex position
	float3 pos = inputMesh.positionOS;
    pos *= size;
    
    // SET COLOR
    float4 c = v.color;
    c = clamp(c, 0, 1);
    #ifdef ATTRIBUTES_NEED_COLOR
        inputMesh.color = c;
    #endif
    
    float3 vpos = v.position - float3(width / 2.0, 0, width / 2.0);
    vpos *= spacing;
    
    // SET POSITION
    inputMesh.positionOS = mul(mat, pos + vpos) + position;    

	VaryingsType vt;
	vt.vmesh = VertMesh(inputMesh);
	return PackVaryingsType(vt);
}


//////////////////////////////////////////////////
// GPU Instanced Fragment shader
//////////////////////////////////////////////////
void InstancedFrag(PackedVaryingsToPS packedInput,

	OUTPUT_GBUFFER(outGBuffer)
#ifdef _DEPTHOFFSET_ON
	, out float outputDepth : SV_Depth
#endif
)
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
	FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

	// input.positionSS is SV_Position
	PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
	float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
	// Unused
	float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

	///////////////////////////////////////////////
	// Workshop Customize
	surfaceData.baseColor = input.color.rgb;
	///////////////////////////////////////////////

	ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

#ifdef _DEPTHOFFSET_ON
	outputDepth = posInput.deviceDepth;
#endif
}