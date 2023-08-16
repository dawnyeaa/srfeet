Shader "Custom/ObjectSpace" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
  }

  SubShader {
    Tags { "RenderType" = "Opaque"
           "RenderPipeline" = "UniversalPipeline"
           "Queue" = "Geometry"
           "UniversalMaterialType" = "Lit" }

    Pass {
      Name "OS"
      Tags { "LightMode" = "OS" }

      HLSLPROGRAM

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      #pragma vertex vert
      #pragma fragment frag

      struct VertexInput {
        float4 positionOS : POSITION;
      };

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float4 positionOS : TEXCOORD0;
      };

      VertexOutput vert(VertexInput i) {
        VertexOutput o;

        VertexPositionInputs vertexInput = GetVertexPositionInputs(i.positionOS.xyz);

        o.positionCS = vertexInput.positionCS;
        o.positionOS = i.positionOS;

        return o;
      }

      half4 frag(VertexOutput i) : SV_TARGET {
        half4 color;

        color.rgb = i.positionOS.xyz;
        color.a = 1;
        return color;
      }

      ENDHLSL
    }

  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
