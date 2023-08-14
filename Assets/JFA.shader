Shader "Custom/JFA" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
  }

  SubShader {
    Tags { "RenderType" = "Opaque"
           "RenderPipeline" = "UniversalPipeline"
           "Queue" = "Geometry"
           "UniversalMaterialType" = "Lit" }
    ZWrite Off Cull Off

    Pass {
      Name "Init"
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

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.uv = i.uv;
        return o;
      }

      half4 frag(VertexOutput i) : SV_TARGET {
        float stepped = step(0.5, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r);
        half4 color = lerp(half4(-1, -1, -1, -1), half4(i.uv, 0, 0), stepped);
        return color;
      }

      ENDHLSL
    }

    Pass {
      Name "Jump"
      Tags { "LightMode" = "UniversalForward" }

      HLSLPROGRAM

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      #pragma vertex vert
      #pragma fragment frag

      uint _jumpDistance;
      
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

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.uv = i.uv;
        return o;
      }

      float ScreenDist(float2 v) {
        float ratio = _MainTex_TexelSize.x / _MainTex_TexelSize.y;
        v.x /= ratio;
        return dot(v, v);
      }

      float2 JFA(float2 fragCoord) {
        float bestDistance = 9999.0;
        float2 bestCoord = float2(0, 0);

        for (int y = -1; y <= 1; ++y) {
          for (int x = -1; x <= 1; ++x) {
            float2 sampleCoord = fragCoord + int2(x, y) * _MainTex_TexelSize.xy * _jumpDistance;
            float2 seedCoord = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleCoord).xy;
            float dist = ScreenDist(seedCoord - fragCoord);
            if ((seedCoord.x != -1 || seedCoord.y != -1) && dist < bestDistance) {
              bestDistance = dist;
              bestCoord = seedCoord;
            }
          }
        }

        return bestCoord;
      }

      half4 frag(VertexOutput i) : SV_TARGET {
        half4 color = half4(JFA(i.uv), 0, 0);
        return color;
      }

      ENDHLSL
    }

    Pass {
      Name "Outline"
      Tags { "LightMode" = "UniversalForward" }

      HLSLPROGRAM

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      #pragma vertex vert
      #pragma fragment frag

      float _lineThickness;
      float _softness;
      
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

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.uv = i.uv;
        return o;
      }

      half4 frag(VertexOutput i) : SV_TARGET {
        float aspect = _MainTex_TexelSize.z/_MainTex_TexelSize.w;
        float2 seedpos = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).xy;
        float2 pos = i.uv;
        float stepy = step(0.01, length(pos - seedpos));
        half4 color = half4(stepy, stepy, stepy, stepy);
        return color;
      }

      ENDHLSL
    }
  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
