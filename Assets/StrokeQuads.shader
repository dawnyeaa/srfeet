Shader "Custom/RenderFeature/StrokeQuads" {
  Properties {
    _MainTex ("Texture", 2D) = "white" {}
    _Size ("size", float) = 1
    _Width ("width", float) = 1
    _Height ("height", float) = 1
  }

  SubShader {
    Pass {
      Name "StrokeQuadDraw"

      Cull Off

      ZTest Always
      ZWrite Off

      Blend SrcAlpha OneMinusSrcAlpha

      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 5.0

      #include "StrokePoint.hlsl"

      #define PI 3.1415926535

      struct VertexOutput {
        float4 positionCS : SV_POSITION;
        float2 uv         : TEXCOORD0;
      };

      StructuredBuffer<StrokePoint> _quadPoints;

      Texture2D<float4> _MainTex;
      SamplerState sampler_MainTex;
      
      float _WidthRatio;
      float _ScreenSizeX;
      float _ScreenSizeY;
      float _Size;
      float _Width;
      float _Height;

      static float2 uvByVertexID[6] = {
        float2(0.0, 1.0),
        float2(1.0, 1.0),
        float2(0.0, 0.0),
        float2(0.0, 0.0),
        float2(1.0, 1.0),
        float2(1.0, 0.0)
      };
      static float angleByVertexID[6] = {
        0.5 * PI,
        0.0 * PI,
        1.0 * PI,
        1.0 * PI,
        0.0 * PI,
        1.5 * PI
      };

      float2 PositionFromStrokePoint(StrokePoint p, int vertexID) {
        float rotation = angleByVertexID[vertexID];

        float s = sin(rotation);
        float c = cos(rotation);
        float2x2 rMatrix = float2x2(c, -s, s, c);
        rMatrix *= 0.5;
        rMatrix += 0.5;
        rMatrix = rMatrix * 2 - 1;
        float size = _Size * 0.12;
        float2 offset = mul(size.xx, rMatrix);
        offset.x *= _Width;
        offset.y *= _Height;
        
        float secrotation = (p.angle+0.25)*2*PI;
        s = sin(secrotation);
        c = cos(secrotation);
        rMatrix = float2x2(c, -s, s, c);
        offset = mul(offset, rMatrix);

        offset.x /= _WidthRatio;

        float2 middle = float2(
          (float(p.middle.x) / _ScreenSizeX) * 2.0 - 1.0,
          (1.0 - (float(p.middle.y) / _ScreenSizeY)) * 2.0 - 1.0
        );

        return middle + offset;
      }

      VertexOutput vert(uint vertexID : SV_VERTEXID, uint instanceID : SV_INSTANCEID) {
        VertexOutput o;

        StrokePoint strokePoint = _quadPoints[instanceID];
        float2 pos = PositionFromStrokePoint(strokePoint, vertexID);

        o.positionCS = float4(pos.x, pos.y, 0.5, 1.0);
        o.uv = uvByVertexID[vertexID];
        return o;
      }

      float4 frag(VertexOutput i) : SV_TARGET {
        float4 tex = _MainTex.Sample(sampler_MainTex, i.uv);
        return tex;
      }
      ENDHLSL
    }
  }
}