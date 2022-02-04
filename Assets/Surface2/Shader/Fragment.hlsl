// Rcam depth surface reconstruction shader (fragment shader)

// Uniforms given from RcamSurface.cs

sampler2D _HoloLinesMap0;
float4 _HoloLinesMap_ST0;
float4 _HoloLinesColor0;

sampler2D _HoloLinesMap1;
float4 _HoloLinesMap_ST1;
float4 _HoloLinesColor1;

sampler2D _HoloLinesMap2;
float4 _HoloLinesMap_ST2;
float4 _HoloLinesColor2;

sampler2D _HoloLinesMap3;
float4 _HoloLinesMap_ST3;
float4 _HoloLinesColor3;

float _FadeHeight;
float _FadeGradSpan;

// Surface effector: Returns an RGBA value.

#include "SimplexNoise2D.hlsl"

// Fragment shader function, copy-pasted from HDRP/ShaderPass/ShaderPassGBuffer.hlsl
// There are a few modification from the original shader. See "Custom:" for details.
void Fragment(
            PackedVaryingsToPS packedInput,
            OUTPUT_GBUFFER(outGBuffer)
            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
            #endif
            )
{
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

	//World normal dir & camera view dir
	float fresnel = dot(surfaceData.normalWS, V);
	//invert the fresnel so the big values are on the outside
	fresnel = saturate(1 - fresnel);
	//raise the fresnel value to the exponents power to be able to adjust it
	fresnel = pow(fresnel, _FresnelExponent);
	float3 fresnelColor = fresnel * _FresnelColor;

	float2 uv = input.texCoord0;
	float2 hololinesUV0 = uv * _HoloLinesMap_ST0.xy + (_HoloLinesMap_ST0.zw);
	float2 hololinesUV1 = uv * _HoloLinesMap_ST1.xy + (_HoloLinesMap_ST1.zw);
	float2 hololinesUV2 = uv * _HoloLinesMap_ST2.xy + (_HoloLinesMap_ST2.zw);
	float2 hololinesUV3 = uv * _HoloLinesMap_ST3.xy + (_HoloLinesMap_ST3.zw);
	float4 hololinesMapPx0 = tex2Dlod(_HoloLinesMap0, float4(hololinesUV0, 0, 0));
	float4 hololinesMapPx1 = tex2Dlod(_HoloLinesMap1, float4(hololinesUV1, 0, 0));
	float4 hololinesMapPx2 = tex2Dlod(_HoloLinesMap2, float4(hololinesUV2, 0, 0));
	float4 hololinesMapPx3 = tex2Dlod(_HoloLinesMap3, float4(hololinesUV3, 0, 0));

	builtinData.emissiveColor = surfaceData.baseColor * 10 + fresnelColor;
	builtinData.emissiveColor += _HoloLinesColor0 * hololinesMapPx0 + _HoloLinesColor1 * hololinesMapPx1 + _HoloLinesColor2 * hololinesMapPx2 + _HoloLinesColor3 * hololinesMapPx3;
	surfaceData.baseColor = float4(0,0,0,0);

	//
	// Fade out by Height
	//
	float3 wpos = GetAbsolutePositionWS(input.positionRWS);
	wpos.y += 0.5; //Shift to zero Y axis

	//float3 pos = posInput.positionWS;
	//builtinData.emissiveColor *= step(wpos.y, _FadeHeight);
	builtinData.emissiveColor *= 1 - smoothstep(_FadeHeight - _FadeGradSpan, _FadeHeight, wpos.y);


#ifdef DEBUG_DISPLAY
    ApplyDebugToSurfaceData(input.worldToTangent, surfaceData);
#endif

    ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}
