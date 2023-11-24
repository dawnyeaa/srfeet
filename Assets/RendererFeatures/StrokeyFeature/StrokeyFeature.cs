using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StrokeyFeature : ScriptableRendererFeature {
  private IDPass _idPass;
  private SobelishAnglePass _sobelishPass;
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
  [SerializeField] Texture2D _strokeTexture;
  [SerializeField] float _strokeSize = 0.16f;
  [SerializeField] float _strokeWidth = 14.89f;
  [SerializeField] float _strokeHeight = 2.46f;
  [SerializeField] Vector2 _strokeRandomWidthBounds = new(0.2f, 1.1f);
  [SerializeField] Vector2 _strokeRandomHeightBounds = new(0.7f, 1.4f);

  [SerializeField] float _strokeDensity = 1;

  public override void Create() {
    int SobelOutRT = Shader.PropertyToID("_sobelOutRT");
    int VoronoiOutRT = Shader.PropertyToID("_voronoiOutRT");
    _idPass = new IDPass("ID Pass", _layerMask);
    _sobelishPass = new SobelishAnglePass("Sobelish Pass", SobelOutRT, _angleBlurSize) {
      _sobelishMaterial = _sobelishMaterial,
      _boxBlurMaterial = _boxBlurMaterial
    };
    _voronoiPass = new VoronoiPass("Voronoi Pass", VoronoiOutRT, _poissonPoints, _strokeDensity) {
      _voronoiMesh = _voronoiMesh,
      _voronoiMaterial = _voronoiMaterial
    };
    _strokeQuadPass = new StrokeQuadPass(_strokeyQuadsComputeShader, 
                                        "Strokey Quads Pass", 
                                        SobelOutRT, 
                                        VoronoiOutRT, 
                                        _poissonPoints, 
                                        _pointScanSize,
                                        _strokeTexture,
                                        _strokeDensity,
                                        _strokeSize,
                                        _strokeWidth,
                                        _strokeHeight,
                                        _strokeRandomWidthBounds,
                                        _strokeRandomHeightBounds) {
      _quadMaterial = _strokeQuadMaterial,
      _sobelBlitMat = _sobelBlitMaterial
    };
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    if (renderingData.cameraData.isPreviewCamera) return;
    if (renderingData.cameraData.isSceneViewCamera) return;
    renderer.EnqueuePass(_idPass);
    renderer.EnqueuePass(_sobelishPass);
    renderer.EnqueuePass(_voronoiPass);
    renderer.EnqueuePass(_strokeQuadPass);
  }

  protected override void Dispose(bool disposing) {
    _voronoiPass.Dispose();
    _strokeQuadPass.Dispose();
    base.Dispose(disposing);
  }
}
