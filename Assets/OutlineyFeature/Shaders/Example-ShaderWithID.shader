Shader "Example/ShaderWithIDPass" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
    _ShadowColor ("Shadow Color", Color) = (1, 1, 1, 1)
    _ShadowHardness ("Shadow Hardness", Range(0, 1)) = 0.9
    _ShadowSize ("Shadow Size", Range(0, 1)) = 0.6

    _IDTex ("ID Texture", 2D) = "white" {}
  }

  SubShader {
    Tags { "RenderType" = "Opaque"
           "RenderPipeline" = "UniversalPipeline"
           "Queue" = "Geometry"
           "UniversalMaterialType" = "Lit" }

    Pass {
      Name "ForwardLit"
      Tags { "LightMode" = "UniversalForward" }

      HLSLPROGRAM
      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      #pragma vertex vert
      #pragma fragment frag

      CBUFFER_START(UnityPerMaterial)
      half4 _ShadowColor;
      float _ShadowHardness;
      float _ShadowSize;

      TEXTURE2D(_MainTex);
      SAMPLER(sampler_MainTex);
      CBUFFER_END

      struct VertexInput {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
        float3 normalOS   : NORMAL;
        float4 tangentOS  : TANGENT;
      };

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
        float3 normalWS   : TEXCOORD1;
      };
      
      float3 _LightDirection;

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        VertexNormalInputs normalInput = GetVertexNormalInputs(i.normalOS, i.tangentOS);

        o.positionCS = vertexInput.positionCS;
        o.normalWS = normalInput.normalWS;
        o.uv = i.uv;

        return o;
      }

      half4 frag(VertexOutput i) : SV_TARGET {
        half4 color;
        float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        float lightDot = (dot(_LightDirection, i.normalWS)*0.5)+0.5;
        float shadowSoftnessOffset = (1-_ShadowHardness)/2;
        float lightMask = smoothstep(_ShadowSize-shadowSoftnessOffset, _ShadowSize+shadowSoftnessOffset, lightDot);

        color.rgb = lerp(lerp(baseTex.rgb, _ShadowColor.rgb, _ShadowColor.a), baseTex.rgb, lightMask);
        color.a = 1;
        return color;
      }

      ENDHLSL
    }

    Pass {
      Name "ID"
      Tags { "LightMode" = "ID" }

      HLSLPROGRAM

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      #pragma vertex vert
      #pragma fragment frag

      CBUFFER_START(UnityPerMaterial)
      TEXTURE2D(_IDTex);
      SAMPLER(sampler_IDTex);
      CBUFFER_END

      struct VertexInput {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
      };

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
        float3 positionOS : TEXCOORD1;
      };

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.uv = i.uv;
        o.positionOS = i.positionOS.xyz;

        return o;
      }

      void frag(VertexOutput i, out half4 ID : SV_TARGET0, out float3 OSPos : SV_TARGET1) {
        half4 color;
        float4 idTex = SAMPLE_TEXTURE2D(_IDTex, sampler_IDTex, i.uv);

        color.rgb = idTex.rgb;
        color.a = 1;
        ID = color;
        OSPos = i.positionOS;
      }

      ENDHLSL
    }

  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
