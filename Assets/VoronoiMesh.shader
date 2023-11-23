Shader "Custom/VoronoiMesh" {
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
      
      Cull Off

      HLSLPROGRAM
      #pragma prefer_hlslcc gles
      #pragma exclude_renderers d3d11_9x
      #pragma target 2.0

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing

      int _PositionsSize;
      StructuredBuffer<float4> _Positions;

      struct VertexInput {
        float4 positionOS : POSITION;
        float2 uv         : TEXCOORD0;
      };

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
        float4 color      : COLOR;
      };

      VertexOutput vert(VertexInput i, uint vid : SV_VERTEXID) {
        VertexOutput o;

        uint quadID = vid / 4;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        uint index = quadID / 2;
        float2 quadPos = quadID % 2 == 0 ? _Positions[index].xy : _Positions[index].zw;

        uint size = 5000;
        uint pointSize = 100;

        float2 pos = (i.uv*pointSize+(quadPos*2-size))/_ScreenParams.xy;
        o.positionCS = float4(pos, 0, 1);
        o.uv = i.uv;
        o.color = float4(1, 0, 0, 1);

        return o;
      }

      void frag(VertexOutput i, out float4 color : SV_TARGET0, out float depth : SV_DEPTH) {
        color = float4(0, 0, 1-length(i.uv), 1);
        depth = 1-length(i.uv);
      }

      ENDHLSL
    }

  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
