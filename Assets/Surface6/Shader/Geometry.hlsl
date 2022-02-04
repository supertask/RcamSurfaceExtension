// Rcam depth surface reconstruction shader (geometry shader)
//
// This is a geometry shader that accepts a single point and outputs two
// triangles. It retrieves positions from a given position map and reconstruct
// normal/tangent vectors. It discards triangles that only contains points on
// the far plane.
//
// We use UV1 to deliver alpha values for depth shading to the fragment shader.

// Uniforms given from RcamSurface.cs
uint _XCount, _YCount;
sampler2D _PositionMap;
float4x4 _LocalToWorld;
float4x4 _WorldToCamera;

float _FresnelExponent;
float4 _FresnelColor;
float _BinaryThreshold;

// Position map sample helper
float4 SamplePosition(float u, float v)
{
    return tex2Dlod(_PositionMap, float4(u, v, 0, 0));
}

// Vertex data output helper
PackedVaryingsType VertexOutput
    (float3 position, float3 normal, float3 tangent, float2 uv0, float alpha, float3 color)
{
    AttributesMesh am;
    am.positionOS = position;
#ifdef ATTRIBUTES_NEED_NORMAL
    am.normalOS = normal;
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
    am.tangentOS = float4(tangent, 1);
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD0
    am.uv0 = uv0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD1
    am.uv1 = alpha;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD2
    am.uv2 = 0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD3
    am.uv3 = 0;
#endif
#ifdef ATTRIBUTES_NEED_COLOR
    am.color = float4(color, 0);
#endif
    UNITY_TRANSFER_INSTANCE_ID(input, am);
    return Vert(am);
}

// Geometry shader body
[maxvertexcount(6)]
void Geometry(
    uint pid : SV_PrimitiveID,
    point Attributes input[1],
    inout TriangleStream<PackedVaryingsType> outStream
)
{
    float u = (pid % _XCount + 0.5) / _XCount;
    float v = (pid / _XCount + 0.5) / _YCount;

    float du = 0.5 / _XCount;
    float dv = 0.5 / _YCount;

    // Position map samples
    float4 s21 = SamplePosition(u - du * 1, v - dv * 3);
    float4 s31 = SamplePosition(u + du * 1, v - dv * 3);

    float4 s12 = SamplePosition(u - du * 3, v - dv * 1);
    float4 s22 = SamplePosition(u - du * 1, v - dv * 1);
    float4 s32 = SamplePosition(u + du * 1, v - dv * 1);
    float4 s42 = SamplePosition(u + du * 3, v - dv * 1);

    float4 s13 = SamplePosition(u - du * 3, v + dv * 1);
    float4 s23 = SamplePosition(u - du * 1, v + dv * 1);
    float4 s33 = SamplePosition(u + du * 1, v + dv * 1);
    float4 s43 = SamplePosition(u + du * 3, v + dv * 1);

    float4 s24 = SamplePosition(u - du * 1, v + dv * 3);
    float4 s34 = SamplePosition(u + du * 1, v + dv * 3);

    // Normal vector calculation
    float3 n0 = normalize(cross(s32.xyz - s12.xyz, s23.xyz - s21.xyz));
    float3 n1 = normalize(cross(s42.xyz - s22.xyz, s33.xyz - s31.xyz));
    float3 n2 = normalize(cross(s33.xyz - s13.xyz, s24.xyz - s22.xyz));
    float3 n3 = normalize(cross(s43.xyz - s23.xyz, s34.xyz - s32.xyz));

    // Tangent vector calculation
    float3 t0 = normalize(cross(n0, float3(0, 0, 1)));
    float3 t1 = normalize(cross(n1, float3(0, 0, 1)));
    float3 t2 = normalize(cross(n2, float3(0, 0, 1)));
    float3 t3 = normalize(cross(n3, float3(0, 0, 1)));

    // Convert into the world space.
    float3 p0 = mul(_LocalToWorld, float4(s22.xyz, 1)).xyz;
    float3 p1 = mul(_LocalToWorld, float4(s32.xyz, 1)).xyz;
    float3 p2 = mul(_LocalToWorld, float4(s23.xyz, 1)).xyz;
    float3 p3 = mul(_LocalToWorld, float4(s33.xyz, 1)).xyz;

    n0 = mul((float3x3)_LocalToWorld, n0);
    n1 = mul((float3x3)_LocalToWorld, n1);
    n2 = mul((float3x3)_LocalToWorld, n2);
    n3 = mul((float3x3)_LocalToWorld, n3);

    t0 = mul((float3x3)_LocalToWorld, t0);
    t1 = mul((float3x3)_LocalToWorld, t1);
    t2 = mul((float3x3)_LocalToWorld, t2);
    t3 = mul((float3x3)_LocalToWorld, t3);

    // UV coordinates
    float2 uv0 = float2(u - du, v - dv);
    float2 uv1 = float2(u + du, v - dv);
    float2 uv2 = float2(u - du, v + dv);
    float2 uv3 = float2(u + du, v + dv);

    // Mask values
    float m0 = s22.w;
    float m1 = s32.w;
    float m2 = s23.w;
    float m3 = s33.w;

	//
	// Calc view dir
	//
	float3 v0 = mul(_WorldToCamera, p0);
	float3 v1 = mul(_WorldToCamera, p1);
	float3 v2 = mul(_WorldToCamera, p2);
	float3 v3 = mul(_WorldToCamera, p3);
	/*
//#ifdef VARYINGS_NEED_POSITION_WS
	float3 viewDir0 = GetWorldSpaceNormalizeViewDir(v0);
	float3 viewDir1 = GetWorldSpaceNormalizeViewDir(v1);
	float3 viewDir2 = GetWorldSpaceNormalizeViewDir(v2);
	float3 viewDir3 = GetWorldSpaceNormalizeViewDir(v3);
#else
	// Unused
	float3 viewDir0 = float3(1.0, 1.0, 1.0); // Avoid the division by 0
	float3 viewDir1 = float3(1.0, 1.0, 1.0); // Avoid the division by 0
	float3 viewDir2 = float3(1.0, 1.0, 1.0); // Avoid the division by 0
	float3 viewDir3 = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif
*/
	/*
	float3 xx = float3(0, -1, 0);
	float3 viewDir0 = xx;
	float3 viewDir1 = xx;
	float3 viewDir2 = xx;
	float3 viewDir3 = xx;
	*/

	float3 viewDir0 = GetWorldSpaceNormalizeViewDir(p0);
	float3 viewDir1 = GetWorldSpaceNormalizeViewDir(p1);
	float3 viewDir2 = GetWorldSpaceNormalizeViewDir(p2);
	float3 viewDir3 = GetWorldSpaceNormalizeViewDir(p3);

	float frenel0 = pow(saturate(1 - dot(n0, viewDir0)), _FresnelExponent);
	float frenel1 = pow(saturate(1 - dot(n1, viewDir1)), _FresnelExponent);
	float frenel2 = pow(saturate(1 - dot(n2, viewDir2)), _FresnelExponent);
	float frenel3 = pow(saturate(1 - dot(n3, viewDir3)), _FresnelExponent);
	/*
	float4 fresnelColor0 = pow(saturate(1 - dot(n0, viewDir0)), _FresnelExponent) * _FresnelColor;
	float4 fresnelColor1 = pow(saturate(1 - dot(n1, viewDir1)), _FresnelExponent) * _FresnelColor;
	float4 fresnelColor2 = pow(saturate(1 - dot(n2, viewDir2)), _FresnelExponent) * _FresnelColor;
	float4 fresnelColor3 = pow(saturate(1 - dot(n3, viewDir3)), _FresnelExponent) * _FresnelColor;
	*/

	float4 blackColor = float4(0,0,0,0);
	float4 fresnelColor0 = lerp(blackColor, _FresnelColor, step(_BinaryThreshold, frenel0) );
	float4 fresnelColor1 = lerp(blackColor, _FresnelColor, step(_BinaryThreshold, frenel1) );
	float4 fresnelColor2 = lerp(blackColor, _FresnelColor, step(_BinaryThreshold, frenel2) );
	float4 fresnelColor3 = lerp(blackColor, _FresnelColor, step(_BinaryThreshold, frenel3) );
	/*
	fresnelColor0 = frenel0 * _FresnelColor;
	fresnelColor1 = frenel1 * _FresnelColor;
	fresnelColor2 = frenel2 * _FresnelColor;
	fresnelColor3 = frenel3 * _FresnelColor;
	*/

    // First triangle
    if (m0 + m1 + m2 > 0.1)
    {
        outStream.Append(VertexOutput(p0, n0, t0, uv0, m0, fresnelColor0));
        outStream.Append(VertexOutput(p1, n1, t1, uv1, m1, fresnelColor0));
        outStream.Append(VertexOutput(p2, n2, t2, uv2, m2, fresnelColor0));
        outStream.RestartStrip();
    }

    // Second triangle
    if (m1 + m2 + m3 > 0.1)
    {
        outStream.Append(VertexOutput(p1, n1, t1, uv1, m1, fresnelColor3));
        outStream.Append(VertexOutput(p3, n3, t3, uv3, m3, fresnelColor3));
        outStream.Append(VertexOutput(p2, n2, t2, uv2, m2, fresnelColor3));
        outStream.RestartStrip();
    }
}
