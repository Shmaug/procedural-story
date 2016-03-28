float4x4 ViewProj;
float4x4 World;

float3 LightDirection = float3(-.5, -.5, 0);
float AmbientBrightness = .5;

bool Textured;
float4 MaterialColor;
texture Tex;
sampler2D texSamp = sampler_state {
	Texture = <Tex>;
};

bool DepthDraw;
float3 SunPos;
float4x4 LightWVP;
float2 DepthPixelSize;
texture DepthTexture;
sampler depthsamp = sampler_state {
	Texture = <DepthTexture>;
	AddressU = Clamp;
	AddressV = Clamp;
	Filter = Point;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 UV : TEXCOORD0;
	float3 Color : COLOR0;
	float light : TEXCOORD1;
	float4 depthCoord : TEXCOORD2;
	float depth : TEXCOORD3;
};

VertexShaderOutput CommonVS(float4 pos, float3 norm, float4 col, float2 uv, float4x4 transform)
{
	VertexShaderOutput output;

	float4 worldpos = mul(pos, transform);
	if (DepthDraw)
		output.Position = mul(worldpos, LightWVP);
	else
		output.Position = mul(worldpos,  ViewProj);
	output.light =
		saturate(max(-dot(normalize(mul(norm, (float3x3)transform)), LightDirection), 0)
		+ AmbientBrightness);
	output.Color = col * MaterialColor;
	output.UV = uv;
	output.depthCoord = mul(mul(pos, transform), LightWVP);

	float num = LightDirection.x * (-LightDirection.x) + LightDirection.y * (-LightDirection.y) + LightDirection.z * (-LightDirection);

	float num2 = LightDirection.x * worldpos.x + LightDirection.y * worldpos.y + LightDirection.z * worldpos.z;
	float D = -(LightDirection.x * SunPos.x + LightDirection.y * SunPos.y + LightDirection.z * SunPos.z);
	float depth = (-D - num2) / num;
	if (depth < 0)
		depth = 0;
	output.depth = depth;

	return output;
}

VertexShaderOutput VBOVS(float4 Position : POSITION0, float3 Normal : NORMAL0, float4 Color : COLOR0)
{
	return CommonVS(Position, Normal, Color, 0, World);
}

VertexShaderOutput ModelVS(float4 Position : POSITION0, float3 Normal : NORMAL0, float2 UV : TEXCOORD0)
{
    return CommonVS(Position, Normal, 1, UV, World);
}

VertexShaderOutput InstancedVS(float4 Position : POSITION0, float3 Normal : NORMAL0, float4x4 transform : BLENDWEIGHT)
{
	return CommonVS(Position, Normal, 1, 0, mul(World, transpose(transform)));
}

float4 DiffusePS(VertexShaderOutput input) : COLOR0
{
	if (DepthDraw)
		return float4(input.depth, 0, 0, 0);
	else {
		float l = input.light;
		float4 tc = float4(1, 1, 1, 1);
		if (Textured) {
			float4 tc = tex2D(texSamp, input.UV);
			clip(tc.a - 1);
		}
		/*
		// shadow map shadows
		float2 coords = (input.depthCoord.xy * float2(1, -1)) / input.depthCoord.w * .5f + float2(.5f, .5f);
		if (input.depthCoord.z > 0 && coords.x >= 0 && coords.y >= 0 && coords.x <= 1 && coords.y <= 1) {
			float d = 0;
			d += tex2D(depthsamp, coords).r;
			if (d < input.depth + .075)
				l *= .25f;
		}
		*/
		
		return float4(saturate(tc.rgb * input.Color * l), 1);
	}
}

technique Model
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 ModelVS();
        PixelShader = compile ps_3_0 DiffusePS();
    }
}
technique VBO
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VBOVS();
		PixelShader = compile ps_3_0 DiffusePS();
	}
}
technique TexturedVBO
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 ModelVS();
		PixelShader = compile ps_3_0 DiffusePS();
	}
}
technique Instanced
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 InstancedVS();
		PixelShader = compile ps_3_0 DiffusePS();
	}
}
