using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ActionCode.Attributes;

public enum Samples {
  x2,
  x4,
  x8
}

public class OutlineyFeature : ScriptableRendererFeature {
  private IDPass _idPass;
  private SobelishPass _sobelishPass;
  private JFAPass _jfaPass;
  // private StrokeQuadPass _strokeQuadPass;


  [Header("ID Pass")]
  [SerializeField] LayerMask _layerMask;
  [SerializeField] bool _useMSAA;
  [SerializeField, ShowIf("_useMSAA", LogicalOperatorType.Equals, true)] Samples _mSAASamples;

  [Header("Sobelish Pass")]
  [SerializeField] Material _sobelishMaterial;

  [Header("JFA + Outline Pass")]
  [SerializeField] Material _jfaMaterial;
  [SerializeField] Material _osSobelBlurMaterial;
  [SerializeField] Material _dfOutlineMaterial;
  [SerializeField, Range(3, 100)] int _blurSize = 3;
  [Space(10)]
  [SerializeField, Min(1)] int _outlineWidth = 5;
  [SerializeField, ColorUsage(true, false)] Color _outlineColor = Color.black;
  [SerializeField, Required] Texture3D _outlineWobbleTexture;
  [SerializeField] float _outlineWobbleTextureScale = 1;
  [SerializeField] Vector3 _outlineWobbleTextureOffset = new Vector3(0, 0, 0);
  [SerializeField, Min(0)] float _outlineWobbleStrength = 1;
  [SerializeField, Range(0, 144)] int _outlineWobbleFPS = 6;
  [SerializeField, ShowIf("_outlineWobbleFPS", LogicalOperatorType.GreaterThan, 0)] Vector3 _outlineWobbleTexScrollDir = new Vector3(0, 1.1f, 0);
  // [SerializeField] Material _strokeQuadMaterial;
  // [SerializeField] Material _sobelBlitMaterial;
  // [SerializeField] ComputeShader _strokeyQuadsComputeShader;
  // [SerializeField] Texture2D _poissonTex;

  public override void Create() {
    int IDRT = Shader.PropertyToID("_IDPassRT");
    int OSPosRT = Shader.PropertyToID("_osRT");
    int SobelOutRT = Shader.PropertyToID("_sobelOutRT");
    int SobelOutOSPosRT = Shader.PropertyToID("_sobelOSPosRT");
    float outlineWobbleMultiplier = 0.01f;
    _idPass = new IDPass("ID Pass", _layerMask, _useMSAA, (int)Mathf.Pow(2, (int)_mSAASamples+1), IDRT, OSPosRT);
    _sobelishPass = new SobelishPass("Sobelish Pass", IDRT, OSPosRT, SobelOutRT, SobelOutOSPosRT) {
      _sobelishMaterial = _sobelishMaterial
    };
    _jfaPass = new JFAPass("JFA Pass", 
                           _blurSize, 
                           _outlineWidth, 
                           _outlineColor, 
                           _outlineWobbleTexture, 
                           _outlineWobbleTextureScale, 
                           _outlineWobbleTextureOffset, 
                           _outlineWobbleStrength*outlineWobbleMultiplier, 
                           _outlineWobbleFPS, 
                           _outlineWobbleTexScrollDir, 
                           SobelOutRT, SobelOutOSPosRT) {
      _jfaMaterial = _jfaMaterial,
      _boxBlurMaterial = _osSobelBlurMaterial,
      _dfOutlineMaterial = _dfOutlineMaterial
    };
    // _strokeQuadPass = new StrokeQuadPass(_strokeyQuadsComputeShader, "Strokey Quads Pass", SobelOutRT, _poissonTex) {
    //   _quadMaterial = _strokeQuadMaterial,
    //   _sobelBlitMat = _sobelBlitMaterial
    // };
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    renderer.EnqueuePass(_idPass);
    renderer.EnqueuePass(_sobelishPass);
    renderer.EnqueuePass(_jfaPass);
    // renderer.EnqueuePass(_strokeQuadPass);
  }
}
