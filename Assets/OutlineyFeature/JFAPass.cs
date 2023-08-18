using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class JFAPass : ScriptableRenderPass {
  #region propertyNames
  private static readonly int _jumpProperty = Shader.PropertyToID("_jumpDistance");
  private static readonly int _osSobelProperty = Shader.PropertyToID("_OSSobel");
  private static readonly int _kernelSizeProperty = Shader.PropertyToID("_KernelSize");
  private static readonly int _screenProperty = Shader.PropertyToID("_Screen");
  private static readonly int _lineThicknessProperty = Shader.PropertyToID("_lineThickness");
  private static readonly int _lineColorProperty = Shader.PropertyToID("_LineColor");
  private static readonly int _modTexProperty = Shader.PropertyToID("_ModulateTex");
  private static readonly int _modTexSProperty = Shader.PropertyToID("_ModulateTex_S");
  private static readonly int _modTexTProperty = Shader.PropertyToID("_ModulateTex_T");
  private static readonly int _modStrengthProperty = Shader.PropertyToID("_ModulationStrength");
  private static readonly int _modFPSProperty = Shader.PropertyToID("_FPS");
  private static readonly int _lineBoilOffsetDirProperty = Shader.PropertyToID("_LineBoilOffsetDir");
  #endregion

  ProfilingSampler _profilingSampler;
  public Material _jfaMaterial;
  public Material _boxBlurMaterial;
  public Material _dfOutlineMaterial;
  private int _inputRenderTargetId;
  private int _osSobelRTId;
  private RenderTargetIdentifier _inputRenderTargetIdentifier;
  private RenderTargetIdentifier _osSobelTex;

  private int _outlineWidth;
  private int _blurSize;
  private Color _outlineColor;
  private Texture _outlineWobbleTex;
  private float _outlineWobbleTexScale;
  private Vector3 _outlineWobbleTexOffset;
  private float _outlineWobbleStrength;
  private float _outlineWobbleFPS;
  private Vector3 _outlineWobbleTexScrollDir;
  private static readonly int _tmpId1 = Shader.PropertyToID("tmpJFART1"), _tmpId2 = Shader.PropertyToID("tmpJFART2");
  RenderTargetIdentifier _tmpRT1, _tmpRT2;
  public JFAPass(string profilerTag, 
                 int blurSize, 
                 int outlineWidth,
                 Color outlineColor,
                 Texture3D outlineWobbleTexture,
                 float outlineWobbleTextureScale,
                 Vector3 outlineWobbleTextureOffset,
                 float outlineWobbleStrength,
                 int outlineWobbleFPS,
                 Vector3 outlineWobbleTexScrollDir, 
                 int renderTargetId, int osSobelRTId) {
    _profilingSampler = new ProfilingSampler(profilerTag);

    _blurSize = blurSize;
    _outlineWidth = outlineWidth;
    _outlineColor = outlineColor;
    _outlineWobbleTex = outlineWobbleTexture;
    _outlineWobbleTexScale = outlineWobbleTextureScale;
    _outlineWobbleTexOffset = outlineWobbleTextureOffset;
    _outlineWobbleStrength = outlineWobbleStrength;
    _outlineWobbleFPS = outlineWobbleFPS;
    _outlineWobbleTexScrollDir = outlineWobbleTexScrollDir;

    _inputRenderTargetId = renderTargetId;
    _osSobelRTId = osSobelRTId;
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _inputRenderTargetIdentifier = new RenderTargetIdentifier(_inputRenderTargetId);
    _osSobelTex = new RenderTargetIdentifier(_osSobelRTId);
  }

  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
    RenderTextureDescriptor desc = cameraTextureDescriptor;
    desc.colorFormat = RenderTextureFormat.ARGBFloat;
    cmd.GetTemporaryRT(_tmpId1, desc);
    cmd.GetTemporaryRT(_tmpId2, desc);

    _tmpRT1 = new RenderTargetIdentifier(_tmpId1);
    _tmpRT2 = new RenderTargetIdentifier(_tmpId2);

    ConfigureTarget(_tmpRT1);
    ConfigureTarget(_tmpRT2);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    var cmd = CommandBufferPool.Get();
    cmd.Clear();
    int passes = Mathf.FloorToInt(Mathf.Log(_outlineWidth, 2));
    var maxPassSize = (int)Mathf.Pow(2,passes);

    using (new ProfilingScope(cmd, _profilingSampler)) {

      // init the JFA
      cmd.Blit(_inputRenderTargetIdentifier, _tmpRT1, _jfaMaterial, 0);

      // do all the JFA passes
      for (int jump = maxPassSize; jump >= 1; jump /= 2) {
        cmd.SetGlobalInt(_jumpProperty, jump);
        cmd.Blit(_tmpRT1, _tmpRT2, _jfaMaterial, 1);

        // ping pong
        (_tmpRT2, _tmpRT1) = (_tmpRT1, _tmpRT2);
      }

      cmd.SetGlobalTexture(_osSobelProperty, _osSobelTex);
      
      // create the distance field from the JFA
      cmd.Blit(_tmpRT1, _tmpRT2, _jfaMaterial, 2);


      cmd.SetGlobalInt(_kernelSizeProperty, _blurSize);
      // blur the distance field
      cmd.Blit(_tmpRT2, _tmpRT1, _boxBlurMaterial, 0);
      cmd.Blit(_tmpRT1, _tmpRT2, _boxBlurMaterial, 1);

      // turn it into outline on screen
      cmd.Blit(renderingData.cameraData.renderer.cameraColorTarget, _tmpRT1);
      cmd.SetGlobalTexture(_screenProperty, _tmpRT1);
      cmd.SetGlobalFloat(_lineThicknessProperty, _outlineWidth);
      cmd.SetGlobalColor(_lineColorProperty, _outlineColor);
      cmd.SetGlobalTexture(_modTexProperty, _outlineWobbleTex);
      cmd.SetGlobalFloat(_modTexSProperty, _outlineWobbleTexScale);
      cmd.SetGlobalVector(_modTexTProperty, _outlineWobbleTexOffset);
      cmd.SetGlobalFloat(_modStrengthProperty, _outlineWobbleStrength);
      cmd.SetGlobalFloat(_modFPSProperty, _outlineWobbleFPS);
      cmd.SetGlobalVector(_lineBoilOffsetDirProperty, _outlineWobbleTexScrollDir);
      cmd.Blit(_tmpRT2, renderingData.cameraData.renderer.cameraColorTarget, _dfOutlineMaterial);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }
}