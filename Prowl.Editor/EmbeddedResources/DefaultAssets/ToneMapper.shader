﻿Shader "Default/ToneMapper"

Pass "ToneMapper"
{
    Tags { "RenderOrder" = "Composite" }

	DepthStencil
	{
		DepthTest Off
		DepthWrite Off
	}
    
    Blend Override

    // Rasterizer culling mode
    Cull None

    HLSLPROGRAM
        #pragma vertex Vertex
        #pragma fragment Fragment

        #include "Prowl.hlsl"


		struct Attributes
		{
			float3 position : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Varyings
		{
			float4 position : SV_POSITION;
			float2 uv : TEXCOORD0;
		};


		Texture2D<float4> _MainTex;
		SamplerState sampler_MainTex;

        float _Contrast;
        float _Saturation;


        Varyings Vertex(Attributes input)
        {
			Varyings output = (Varyings)0;

			output.position = float4(input.position.xyz, 1.0);
			output.uv = input.uv;

            return output;
        }

        // TODO: A Keyword/Feature to switch between different tonemapping operators
		#define MELON

#ifdef MELON
		// Shifts red to Yellow, green to Yellow, blue to Cyan,
		float3 HueShift(float3 In)
		{
			float A = max(In.x, In.y);
			return float3(A, max(A, In.z), In.z);
		}

		float Cmax(float3 In)
		{
			return max(max(In.r, In.g), In.b);
		}

		// Made by TripleMelon
		float3 MelonTonemap(float3 color)
		{
			// remaps the colors to [0-1] range
			// tested to be as close ti ACES contrast levels as possible
			color = pow(color, float3(1.56, 1.56, 1.56));
			color = color/(color + 0.84);

			// governs the transition to white for high color intensities
			float factor = Cmax(color) * 0.15; // multiply by 0.15 to get a similar look to ACES
			factor = factor / (factor + 1); // remaps the factor to [0-1] range
			factor *= factor; // smooths the transition to white

			// shift the hue for high intensities (for a more pleasing look).
			color = lerp(color, HueShift(color), factor); // can be removed for more neutral colors
			color = lerp(color, float3(1.0, 1.0, 1.0), factor); // shift to white for high intensities

		    return color;
		}
#endif

#ifdef ACES
		const float3x3 ACESInputMat = float3x3
		(
		    0.59719, 0.35458, 0.04823,
		    0.07600, 0.90834, 0.01566,
		    0.02840, 0.13383, 0.83777
		);

		// ODT_SAT => XYZ => D60_2_D65 => sRGB
		const float3x3 ACESOutputMat = float3x3
		(
		     1.60475, -0.53108, -0.07367,
		    -0.10208,  1.10813, -0.00605,
		    -0.00327, -0.07276,  1.07602
		);

		float3 RRTAndODTFit(float3 v)
		{
		    float3 a = v * (v + 0.0245786f) - 0.000090537f;
		    float3 b = v * (0.983729f * v + 0.4329510f) + 0.238081f;
		    return a / b;
		}

		float3 ACESFitted(float3 color)
		{
		    color = color * ACESInputMat;

		    // Apply RRT and ODT
		    color = RRTAndODTFit(color);

		    color = color * ACESOutputMat;

		    return color;
		}
#endif

#ifdef REINHARD
		float3 lumaBasedReinhardToneMapping(float3 color)
		{
			float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
			float toneMappedLuma = luma / (1.0 + luma);
			color *= toneMappedLuma / luma;
			return color;
		}
#endif

#ifdef FILMIC
		float3 filmicToneMapping(float3 color)
		{
			color = max((float3)0, color - (float3)0.004);
			color = (color * (6.2 * color + .5)) / (color * (6.2 * color + 1.7) + 0.06);
			return color;
		}
#endif

#ifdef UNCHARTED
		float3 Uncharted2ToneMapping(float3 color)
		{
			float A = 0.15;
			float B = 0.50;
			float C = 0.10;
			float D = 0.20;
			float E = 0.02;
			float F = 0.30;
			float W = 11.2;
			float exposure = 2.0;
			color *= exposure;
			color = ((color * (A * color + C * B) + D * E) / (color * (A * color + B) + D * F)) - E / F;
			float white = ((W * (A * W + C * B) + D * E) / (W * (A * W + B) + D * F)) - E / F;
			color /= white;
			return color;
		}
#endif


		float3 Tonemap(float3 color)
		{
#ifdef MELON
			color = MelonTonemap(color);
#elif ACES
			color = ACESFitted(color);
#elif REINHARD
			color = lumaBasedReinhardToneMapping(color);
#elif UNCHARTED
			color = Uncharted2ToneMapping(color);
#elif FILMIC
			color = filmicToneMapping(color);
#endif

		    // Clamp to [0, 1]
		  	color = clamp(color, 0.0, 1.0);

			return color;
		}

		float4x4 SaturationMatrix(float saturation)
		{
			float3 luminance = float3(0.3086f, 0.6094f, 0.0820f);
			
			float oneMinusSat = 1.0 - saturation;
			
			float3 red = luminance.x * oneMinusSat;
			red += float3(saturation, 0, 0);
			
			float3 green = luminance.y * oneMinusSat;
			green += float3(0, saturation, 0);
			
			float3 blue = luminance.z * oneMinusSat;
			blue += float3(0, 0, saturation);
			
			return float4x4(
				red.x, red.y, red.z, 0,
				green.x, green.y, green.z, 0,
				blue.x, blue.y, blue.z, 0,
				0, 0, 0, 1
			);
		}

        float4 Fragment(Varyings input) : SV_TARGET
        {
			float3 baseColor = _MainTex.Sample(sampler_MainTex, input.uv).rgb;

            float3 tonemappedColor = Tonemap(baseColor);

			float4 adjustedColor = mul(tonemappedColor, SaturationMatrix(_Saturation));
			
			float t = 0.5 - _Contrast * 0.5; 
			adjustedColor.rgb = adjustedColor.rgb * _Contrast + t;
			
            // Gamma correction
            //float3 gcColor = pow(adjustedColor, (float3)1.0 / 2.2);
            float3 gcColor = LinearToGammaSpace(adjustedColor);
			
			gcColor = saturate(gcColor);
			
            return float4(gcColor, 1.0);
        }
    ENDHLSL
}
