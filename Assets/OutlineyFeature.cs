using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineyFeature : ScriptableRendererFeature {
  private IDPass _idPass;
  private SobelishPass _sobelishPass;
  private JFAPass _jfaPass;
  private VoronoiPass _voronoiPass;
  private StrokeQuadPass _strokeQuadPass;

  [SerializeField] LayerMask _layerMask;
  [SerializeField] Material _sobelishMaterial;
  [SerializeField] Material _strokeQuadMaterial;
  [SerializeField] Material _sobelBlitMaterial;
  [SerializeField] Material _boxBlurMaterial;
  [SerializeField] Mesh _voronoiMesh;
  [SerializeField] Material _voronoiMaterial;
  // [SerializeField] ComputeShader _jfaComputeShader;
  [SerializeField] ComputeShader _strokeyQuadsComputeShader;
  [SerializeField] int _angleBlurSize = 3;
  [SerializeField] Texture2D _poissonTex;
  [SerializeField] PoissonArrangementObject _poissonPoints;
  [SerializeField] int _pointScanSize;

  public override void Create() {
    int SobelOutRT = Shader.PropertyToID("_sobelOutRT");
    int VoronoiOutRT = Shader.PropertyToID("_voronoiOutRT");
    _idPass = new IDPass("ID Pass", _layerMask);
    _sobelishPass = new SobelishPass("Sobelish Pass", SobelOutRT, _angleBlurSize) {
      _sobelishMaterial = _sobelishMaterial,
      _boxBlurMaterial = _boxBlurMaterial
    };
    _voronoiPass = new VoronoiPass("Voronoi Pass", VoronoiOutRT, _poissonPoints) {
      _voronoiMesh = _voronoiMesh,
      _voronoiMaterial = _voronoiMaterial
    };
    _strokeQuadPass = new StrokeQuadPass(_strokeyQuadsComputeShader, "Strokey Quads Pass", SobelOutRT, VoronoiOutRT, _poissonPoints, _pointScanSize) {
      _quadMaterial = _strokeQuadMaterial,
      _sobelBlitMat = _sobelBlitMaterial
    };
    // _jfaPass = new JFAPass(_jfaComputeShader, "Init", "Jump", "JFA Pass", JFAInputRT);
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    renderer.EnqueuePass(_idPass);
    renderer.EnqueuePass(_sobelishPass);
    renderer.EnqueuePass(_voronoiPass);
    renderer.EnqueuePass(_strokeQuadPass);
    // renderer.EnqueuePass(_jfaPass);
  }

  protected override void Dispose(bool disposing) {
    _voronoiPass.Dispose();
    _strokeQuadPass.Dispose();
    base.Dispose(disposing);
  }
}
