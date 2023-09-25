Shader "OutlineyFeature/AngleBoxBlur" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
  }

  SubShader {
    Tags { "RenderType" = "Opaque"
           "RenderPipeline" = "UniversalPipeline"
           "Queue" = "Geometry"
           "UniversalMaterialType" = "Lit" }
    // ZWrite Off Cull Off

    HLSLINCLUDE

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    struct VertexInput {
      float4 positionOS : POSITION;
      float2 uv         : TEXCOORD0;
    };

    struct VertexOutput {
      float4 positionCS : SV_POSITION;
      float2 uv         : TEXCOORD0;
    };

    VertexOutput vert(VertexInput i) {
      VertexOutput o;

      VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

      o.positionCS = vertexInput.positionCS;
      o.uv = i.uv;
      return o;
    }

    float DecodeAlpha(float4 input) {
      return step(0.9999, input.r);
    }
      
    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);
    float4 _MainTex_TexelSize;
    int _KernelSize;
    ENDHLSL

    Pass {
      Name "BlurX"
      Tags { "LightMode" = "UniversalForward" }

      HLSLPROGRAM

      #pragma vertex vert
      #pragma fragment frag_horizontal

      float4 frag_horizontal(VertexOutput i) : SV_TARGET {
        float4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        if (DecodeAlpha(center) > 0) {
          float3 sum = float3(0, 0, 0);

          int upper = (_KernelSize - 1) / 2;
          int lower = -upper;
          float4 currValue = float4(0, 0, 0, 0);
          int skippedValues = 0;

          [unroll(100)]
          for (int x = lower; x <= upper; ++x) {
            currValue = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(_MainTex_TexelSize.x * x, 0));
            if (DecodeAlpha(currValue) > 0) {
              sum += currValue.xyz;
            }
            else {
              ++skippedValues;
            }
          }

          sum /= ((upper*2)+1)-skippedValues;
          
          return float4(sum, center.a);
        }
        return float4(0, 0, 0, center.a);
      }
      ENDHLSL
    }

    Pass {
      Name "BlurY"
      Tags { "LightMode" = "UniversalForward" }

      HLSLPROGRAM

      #pragma vertex vert
      #pragma fragment frag_vertical

      float4 frag_vertical(VertexOutput i) : SV_TARGET {
        float4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        if (DecodeAlpha(center) > 0) {
          float3 sum = float3(0, 0, 0);

          int upper = (_KernelSize - 1) / 2;
          int lower = -upper;
          float4 currValue = float4(0, 0, 0, 0);
          int skippedValues = 0;

          [unroll(100)]
          for (int y = lower; y <= upper; ++y) {
            currValue = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + float2(0, _MainTex_TexelSize.y * y));
            if (DecodeAlpha(currValue) > 0) {
              sum += currValue.xyz;
            }
            else {
              ++skippedValues;
            }
          }

          sum /= ((upper*2)+1)-skippedValues;

          return float4(sum, center.a);
        }
        return float4(0, 0, 0, center.a);
      }
      ENDHLSL
    }
  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}