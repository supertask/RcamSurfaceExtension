// Rcam depth surface reconstruction shader (fragment shader)

// Uniforms given from RcamSurface.cs
float _RcamHue;
float2 _RcamLine;   // (alpha, emission)
float2 _RcamSlit;   // (alpha, emission)
float2 _RcamSlider; // (alpha, emission)

float4 _ToonAmbientColor;
float _ToonGlossiness;
float4 _ToonSpecularColor;
float4 _ToonRimColor;
float _ToonRimAmount;
float _ToonRimThreshold;

// Surface effector: Returns an RGBA value.

#include "SimplexNoise2D.hlsl"

float4 Effector(float3 wpos, float2 uv, float time)
{
    float hue = 0, val = 0, alpha = 1;

    // Contour lines
    {
        // Contour using derivatives
        float y = wpos.y * 200;
        float fw = fwidth(y);
        float g = saturate(1 - abs(1 - frac(y) / fw));

        // High frequency noise
        g *= 1 + snoise(uv * 200);

        // Frequency filter
        g = lerp(g, 0.1, smoothstep(0.4, 0.7, fw));

        val += lerp(g, 1, _RcamLine.x) * _RcamLine.y;
        alpha -= (1 - g) * _RcamLine.x;
    }

    // Moving slits
    {
        float g = 0;
        g += snoise(float2(wpos.y * 39 - time * 1.2, time * 1.1)) * 0.7;
        g += snoise(float2(wpos.y * 22 - time * 0.4, time * 0.7)) * 0.7;
        g = saturate(abs(g));

        val += g < (_RcamSlit.x + _RcamSlit.y);
        alpha -=  g < _RcamSlit.x;
    }

    // Sliding rects
    {
        float phi = atan2(wpos.z - 3, wpos.x);
        uint seed = (wpos.y + 10) * 100;

        float wid = lerp(0.02, 2, Hash(seed * 2));
        float spd = lerp(0.50, 3, Hash(seed * 2 + 1));

        float p = phi * wid + spd * time;
        float g = frac(p);

        hue += Hash(seed * 37 + (uint)p) - 0.5;
        val += g < _RcamSlider.y;
        alpha -= (1 - g) < _RcamSlider.x;
    }

    // Actual RGB value
    hue = frac(_Time.x + _RcamHue * hue);
    float3 rgb = HsvToRgb(float3(hue, 1, val * 15));

    return float4(rgb, alpha);
}

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

    // Custom: Color emission from the surface effector
    //float3 wpos = GetAbsolutePositionWS(input.positionRWS);

	DirectionalLightData lightData = _DirectionalLightDatas[0];
	float3 normal = normalize(surfaceData.normalWS);
	//float3 lightPos = normalize(lightData.positionRWS);
	float3 lightPos = lightData.positionRWS;
	float NdotL = dot(lightPos, normal); //same dir = 1, perpendicular dir = 0, opp dir = -1
	float lightIntensity = smoothstep(0, 0.01, NdotL);
	float3 light = lightIntensity * lightData.color;

	float3 halfVectorH = normalize(lightPos + V);
	float NdotH = dot(normal, halfVectorH);
	NdotH = max(NdotH, 0); //(-x ~ Ndot ~ +x) -> (0 ~ NdotH ~ +x)
	float specularIntensity = pow(NdotH * lightIntensity, _ToonGlossiness * _ToonGlossiness);
	float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
	float4 specular = specularIntensitySmooth * _ToonSpecularColor;
	float rimDot = 1 - dot(V, normal);
	//float rimIntensity = rimDot * NdotL;
	float rimIntensity = rimDot * pow(NdotL, _ToonRimThreshold);
	rimIntensity = smoothstep(_ToonRimAmount - 0.01, _ToonRimAmount + 0.01, rimIntensity);
	float4 rim = rimIntensity * _ToonRimColor;

	builtinData.emissiveColor = surfaceData.baseColor  * (_ToonAmbientColor.xyz + light + specular + rim) ;
	surfaceData.baseColor = float3(0,0,0);

#ifdef DEBUG_DISPLAY
    ApplyDebugToSurfaceData(input.worldToTangent, surfaceData);
#endif

    ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}
