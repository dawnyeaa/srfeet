Shader "OutlineyFeature/1Sobelish" {
  Properties {
  }

  SubShader {
    Tags { "RenderType" = "Opaque"
           "RenderPipeline" = "UniversalPipeline"
           "Queue" = "Geometry"
           "UniversalMaterialType" = "Lit" }
    // ZWrite Off Cull Off

    Pass {
      Name "ForwardLit"
      Tags { "LightMode" = "UniversalForward" }

      HLSLPROGRAM

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

      #pragma vertex vert
      #pragma fragment frag
      
      TEXTURE2D(_MainTex);
      SAMPLER(sampler_MainTex);
      float4 _MainTex_TexelSize;
      
      TEXTURE2D(_OSTex);
      SAMPLER(sampler_OSTex);
      float4 _OSTex_TexelSize;

      static float2 uvOffset[9] = {
        float2(0, 0),
        float2(-1, -1),
        float2(0, -1),
        float2(1, -1),
        float2(-1, 0),
        float2(1, 0),
        float2(-1, 1),
        float2(0, 1),
        float2(1, 1)
      };

      struct VertexInput {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
      };

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
      };

      float intensity(in float4 color) {
        return sqrt((color.x*color.x) + (color.y*color.y) + (color.z*color.z));
      }

      float sobelish(float2 uv, float stepx, float stepy) {
        float current = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(0, 0)));
        float right   = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, 0)));
        float bottom  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(0, stepy)));
        float bright  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, stepy)));

        float x = current - bright;
        float y = right - bottom;
        float mag = sqrt((x*x) + (y*y));
        return mag;
      }

      float2 actuallySobel(float2 uv, float stepx, float stepy) {
        float tleft  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(-stepx, -stepy)));
        float top    = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(0, -stepy)));
        float tright = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, -stepy)));
        float left   = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(-stepx, 0)));
        float right  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, 0)));
        float bleft  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(-stepx, stepy)));
        float bottom = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(0, stepy)));
        float bright = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, stepy)));

        float x = (1*tleft + 1*bleft + (2*left)) - (1*tright + 1*bright + (2*right));
        float y = (1*tleft + 1*tright + (2*top)) - (1*bleft + 1*bright + (2*bottom));
        float mag = sqrt((x*x) + (y*y));
        return float2(mag, (atan2(y, x)/(2*PI))+0.5);
      }

      float4 objectSpacePos(float2 uv, float stepx, float stepy, float sobelValue) {
        float depthValues[9] = {
          SampleSceneDepth(uv),
          SampleSceneDepth(uv + half2(-stepx, -stepy)),
          SampleSceneDepth(uv + half2(0, -stepy)),
          SampleSceneDepth(uv + half2(stepx, -stepy)),
          SampleSceneDepth(uv + half2(-stepx, 0)),
          SampleSceneDepth(uv + half2(stepx, 0)),
          SampleSceneDepth(uv + half2(-stepx, stepy)),
          SampleSceneDepth(uv + half2(0, stepy)),
          SampleSceneDepth(uv + half2(stepx, stepy))
        };

        // float2 uvOffsetAvg = float2(0, 0);

        // if (sobelValue > 0) {
        //   float inflectionDepth = depthValues[0] > 0 ? depthValues[0] : 3.402823466e+38F;
        //   float2 uvOffsetTotal = float2(0, 0);
        //   int closerDepthsCount = 0;
        //   for (int i = 1; i < 9; ++i) {
        //     if (depthValues[i] > 0 && depthValues[i] <= inflectionDepth) {
        //       uvOffsetTotal += uvOffset[i];
        //       ++closerDepthsCount;
        //     }
        //   }
        //   if (closerDepthsCount > 0) {
        //     uvOffsetAvg = uvOffsetTotal/closerDepthsCount;
        //   }
        // }

        // float3 osPos = SAMPLE_TEXTURE2D(_OSTex, sampler_OSTex, uv + uvOffsetAvg * float2(stepx, stepy)).xyz;

        float bestDepth = 1000;
        int bestDepthIndex = 0;
        for (int i = 1; i < 9; ++i) {
          if (depthValues[i] > 0 && depthValues[i] < bestDepth) {
            bestDepth = depthValues[i];
            bestDepthIndex = i;
          }
        }

        float3 osPos = SAMPLE_TEXTURE2D(_OSTex, sampler_OSTex, uv + uvOffset[bestDepthIndex] * float2(stepx, stepy)).xyz;
        
        return float4(osPos, sobelValue);
      }

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.uv = i.uv;
        return o;
      }

      void frag(VertexOutput i, out half4 GRT0 : SV_TARGET0, out float4 GRT1 : SV_TARGET1) {
        half4 color = half4(actuallySobel(i.uv, _MainTex_TexelSize.x, _MainTex_TexelSize.y), 0, 0);
        GRT0 = color;
        GRT1 = objectSpacePos(i.uv, _MainTex_TexelSize.x, _MainTex_TexelSize.y, color.r);
      }

      ENDHLSL
    }
  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
