Shader "Custom/JFA" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
    _ModulateTex ("Modulation tex", 2D) = "black" {}
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

      float4 frag(VertexOutput i) : SV_TARGET {
        float stepped = step(0.1, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).r);
        float4 color = lerp(float4(-1, -1, -1, 0), float4(i.uv, 0, 1), stepped);
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
        float2 bestCoord = float2(-1, -1);

        for (int y = -1; y <= 1; ++y) {
          for (int x = -1; x <= 1; ++x) {
            float2 sampleCoord = fragCoord + int2(x, y) * _MainTex_TexelSize.xy * _jumpDistance;
            float4 seedInfo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleCoord);
            float2 seedCoord = seedInfo.xy;
            float dist = ScreenDist(seedCoord - fragCoord);
            if (seedInfo.a < 0.1) continue;
            if (dist < bestDistance) {
              bestDistance = dist;
              bestCoord = seedCoord;
            }
          }
        }

        return bestCoord;
      }

      float4 frag(VertexOutput i) : SV_TARGET {
        float2 jfaoutput = JFA(i.uv);
        float4 color = float4(jfaoutput, 0, (sign(jfaoutput.x)*0.5)+0.5);
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
      
      TEXTURE2D(_Screen);
      SAMPLER(sampler_Screen);
      float4 _Screen_TexelSize;
      
      TEXTURE2D(_ModulateTex);
      SAMPLER(sampler_ModulateTex);

      struct VertexInput {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
      };

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
      };

      float ScreenDist(float2 v) {
        float ratio = _MainTex_TexelSize.x / _MainTex_TexelSize.y;
        v.x /= ratio;
        return length(v);
      }

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.uv = i.uv;
        return o;
      }

      half4 frag(VertexOutput i) : SV_TARGET {
        float aspect = _MainTex_TexelSize.z*_MainTex_TexelSize.y;
        float4 seedinfo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        float2 seedpos = seedinfo.xy;
        float mask = step(0.1, seedinfo.a);
        float2 pos = i.uv;
        float stepy = step(ScreenDist(pos - seedpos)+(SAMPLE_TEXTURE2D(_ModulateTex, sampler_ModulateTex, i.uv*float2(8,8))*0.01), _lineThickness*_MainTex_TexelSize.y);
        return lerp(SAMPLE_TEXTURE2D(_Screen, sampler_Screen, i.uv), float4(0, 0, 0, 1), stepy*mask);
      }

      ENDHLSL
    }
  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
