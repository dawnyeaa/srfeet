using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineyFeature : ScriptableRendererFeature {
  private IDPass _idPass;
  private SobelishPass _sobelishPass;
  private JFAPass _jfaPass;
  private StrokeQuadPass _strokeQuadPass;

  [SerializeField] LayerMask _layerMask;
  [SerializeField] Material _sobelishMaterial;
  [SerializeField] Material _strokeQuadMaterial;
  [SerializeField] Material _sobelBlitMaterial;
  [SerializeField] Material _jfaMaterial;
  [SerializeField] [Min(1)] int _outlineWidth;
  // [SerializeField] ComputeShader _jfaComputeShader;
  [SerializeField] ComputeShader _strokeyQuadsComputeShader;
  [SerializeField] Texture2D _poissonTex;

  public override void Create() {
    int SobelOutRT = Shader.PropertyToID("_sobelOutRT");
    _idPass = new IDPass("ID Pass", _layerMask);
    _sobelishPass = new SobelishPass("Sobelish Pass", SobelOutRT) {
      _sobelishMaterial = _sobelishMaterial
    };
    _strokeQuadPass = new StrokeQuadPass(_strokeyQuadsComputeShader, "Strokey Quads Pass", SobelOutRT, _poissonTex) {
      _quadMaterial = _strokeQuadMaterial,
      _sobelBlitMat = _sobelBlitMaterial
    };
    _jfaPass = new JFAPass(_outlineWidth, "JFA Pass", SobelOutRT) {
      _jfaMaterial = _jfaMaterial
    };
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    renderer.EnqueuePass(_idPass);
    renderer.EnqueuePass(_sobelishPass);
    // renderer.EnqueuePass(_strokeQuadPass);
    renderer.EnqueuePass(_jfaPass);
  }

  protected override void Dispose(bool disposing) {
    _strokeQuadPass.Dispose();
    base.Dispose(disposing);
  }
}
