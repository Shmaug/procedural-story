sampler samp : register(s0);
float4x4 Proj;

float2 PixelSize;
float2 CameraPosition;

int LightCount = 0;
float3 LightColors[16];
float3 LightPositions[16];

float4 AmbientLight = float4(.05, .05, .05, 1);

void SpriteVS(inout float2 texCoord : TEXCOORD0, inout float4 position : POSITION0)
{
	position = mul(position, Proj);
}

float4 LightPS(float2 coords : TEXCOORD0) : COLOR0
{
	float2 worldPos = CameraPosition + ((coords - float2(.5, .5)) / PixelSize);

	float4 light = AmbientLight;
	for (int i = 0; i < LightCount; i++) {
		float d = 1 - (distance(worldPos, LightPositions[i].xy) / LightPositions[i].z);
		if (d < 1 && d > 0)
			light += float4(LightColors[i] * pow(d, 3), 1);
	}
	return tex2D(samp, coords);// * light;
}

technique Shadow
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 SpriteVS();
		PixelShader = compile ps_3_0 LightPS();
	}
}
