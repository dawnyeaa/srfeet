Shader "OutlineyFeature/4DFOutline" {
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
      Name "Step"
      Tags { "LightMode" = "UniversalForward" }

      HLSLPROGRAM

      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

      #pragma vertex vert
      #pragma fragment frag

      float _lineThickness;
      float _Softness;

      float4 _LineColor;
      float _ModulationStrength;

      float _FPS;
      float4 _LineBoilOffsetDir;
      
      TEXTURE2D(_MainTex);
      SAMPLER(sampler_MainTex);
      float4 _MainTex_TexelSize;
      
      TEXTURE2D(_Screen);
      SAMPLER(sampler_Screen);
      float4 _Screen_TexelSize;
      
      TEXTURE3D(_ModulateTex);
      SAMPLER(sampler_ModulateTex);
      float _ModulateTex_S;
      float3 _ModulateTex_T;

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
        float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
        float3 modulateTexPos = mainTex.xyz;
        float mask = step(mainTex.a, 99);
        float4 screen = SAMPLE_TEXTURE2D(_Screen, sampler_Screen, i.uv);
        float secondsPerFrame = 1/_FPS;
        float softness = _Softness + 0.0001;
        float modulation = (SAMPLE_TEXTURE3D(_ModulateTex, sampler_ModulateTex, (modulateTexPos*_ModulateTex_S)+_ModulateTex_T+floor(_Time.y/secondsPerFrame)*_LineBoilOffsetDir).r*2)-1;
        float outlineStep = 1-smoothstep((_lineThickness-softness)*_MainTex_TexelSize.y, (_lineThickness+softness)*_MainTex_TexelSize.y, mainTex.a+(modulation*_ModulationStrength));
        return lerp(screen, _LineColor, outlineStep*mask*_LineColor.a);
      }
      ENDHLSL
    }
  }
  FallBack "Hidden/Universal Render Pipeline/FallbackError"
}