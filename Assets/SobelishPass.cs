using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SobelishPass : ScriptableRenderPass {
  // the material with our sobelish shader on it
  public Material _sobelishMaterial;
  public Material _osMaterial;
  private ProfilingSampler _profilingSampler;
  // the render target that we're grabbing the id map from
  private static readonly int _idMapId = Shader.PropertyToID("_IDPassRT");
  // making an id for the render target we're gonna draw the sobelish thing to
  private int _renderTargetIdSobel;
  private int _renderTargetIdSobelOS;
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier _idMapIdentifier;
  private RenderTargetIdentifier[] _renderTargetIdentifiers;
  private ShaderTagId _osShaderTag;
  private FilteringSettings _filteringSettings;

  public SobelishPass(string profilerTag, int renderTargetIdSobel, int renderTargetIdSobelOS) {
    // set up the profiler so it has a slot in there
    _profilingSampler = new ProfilingSampler(profilerTag);
    _renderTargetIdSobel = renderTargetIdSobel;
    _renderTargetIdSobelOS = renderTargetIdSobelOS;
    
    _osShaderTag = new ShaderTagId("UniversalForward");
    _filteringSettings = new FilteringSettings(null, ~0);

    // i get to choose when this pass happens!
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _idMapIdentifier = new RenderTargetIdentifier(_idMapId);
  }

  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
    cmd.GetTemporaryRT(_renderTargetIdSobel, cameraTextureDescriptor);
    cmd.GetTemporaryRT(_renderTargetIdSobelOS, cameraTextureDescriptor);
    _renderTargetIdentifiers = new RenderTargetIdentifier[2];
    _renderTargetIdentifiers[0] = new RenderTargetIdentifier(_renderTargetIdSobel);
    _renderTargetIdentifiers[1] = new RenderTargetIdentifier(_renderTargetIdSobelOS);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

    CommandBuffer cmd = CommandBufferPool.Get();
    using (new ProfilingScope(cmd, _profilingSampler)) {
      // DrawingSettings objectSpaceMatSettings = CreateDrawingSettings(_osShaderTag, ref renderingData, SortingCriteria.CommonOpaque);
      // objectSpaceMatSettings.overrideMaterial = _osMaterial;
      // objectSpaceMatSettings.overrideMaterialPassIndex = 0;

      // cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget, renderingData.cameraData.renderer.cameraDepthTarget);
      // cmd.ClearRenderTarget(true, true, Color.black);

      // context.ExecuteCommandBuffer(cmd);
      // cmd.Clear();

      // context.DrawRenderers(renderingData.cullResults, ref objectSpaceMatSettings, ref _filteringSettings);

      // render the sobel
      ConfigureTarget(_renderTargetIdentifiers);
      ConfigureTarget(_idMapIdentifier);
      cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
      cmd.SetGlobalTexture(Shader.PropertyToID("_MainTex"), _idMapIdentifier);
      cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _sobelishMaterial);

      // cmd.Blit(_idMapIdentifier, _renderTargetIdentifiers, _sobelishMaterial);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }
}