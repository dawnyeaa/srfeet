Shader "Custom/Sobelish" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
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

      #pragma vertex vert
      #pragma fragment frag
      
      TEXTURE2D(_MainTex);
      SAMPLER(sampler_MainTex);
      float4 _MainTex_TexelSize;

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

      float3 actuallySobel(float2 uv, float stepx, float stepy) {
        float tleft  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(-stepx, -stepy)));
        float top    = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(0, -stepy)));
        float tright = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, -stepy)));
        float left   = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(-stepx, 0)));
        float right  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, 0)));
        float bleft  = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(-stepx, stepy)));
        float bottom = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(0, stepy)));
        float bright = intensity(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + half2(stepx, stepy)));

        float x = (3*tleft + 3*bleft + (10*left)) - (3*tright + 3*bright + (10*right));
        float y = (3*tleft + 3*tright + (10*top)) - (3*bleft + 3*bright + (10*bottom));
        float mag = sqrt((x*x) + (y*y));
        float ang = atan2(y, x);
        return float3(mag, cos(ang)*0.5+0.5, sin(ang)*0.5+0.5);
      }

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.uv = i.uv;
        return o;
      }

      half4 frag(VertexOutput i) : SV_TARGET {
        half4 color = half4(actuallySobel(i.uv, _MainTex_TexelSize.x, _MainTex_TexelSize.y), 0);
        return color;
      }

      ENDHLSL
    }
  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
