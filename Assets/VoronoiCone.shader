Shader "Custom/VoronoiCone" {
  Properties {
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
      #pragma multi_compile_instancing

      struct VertexInput {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
        float4 color      : COLOR;
      };

      float4 _Colors[511];

      VertexOutput vert(VertexInput i, uint instanceID: SV_INSTANCEID) {
        UNITY_SETUP_INSTANCE_ID(i);
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        // god i really wish i understood why this number needs to exist for it to work, but it does and im just gonna use it for now
        float magicRatio = 1/(_ScreenParams.y*0.865);

        float4 ws = (float4(vertexInput.positionWS, 1) * float4(magicRatio, magicRatio, -1, 1));
        o.positionCS = mul(UNITY_MATRIX_P, ws);
        o.positionCS.w = 1;
        o.positionCS += float4(-1, 1, 0, 0);
        o.uv = i.uv;
        o.color = float4(1, 1, 1, 1);

        #ifdef UNITY_INSTANCING_ENABLED
          o.color = _Colors[instanceID];
        #endif

        o.color.b = i.positionOS.b;

        return o;
      }

      float4 frag(VertexOutput i) : SV_TARGET {
        return i.color;
      }

      ENDHLSL
    }

  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
