using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SobelishAnglePass : ScriptableRenderPass {
  // the material with our sobelish shader on it
  public Material _sobelishMaterial;
  public Material _boxBlurMaterial;
  private ProfilingSampler _profilingSampler;
  // the render target that we're grabbing the id map from
  private static readonly int _idMapId = Shader.PropertyToID("_IDPassRT");
  // making an id for the render target we're gonna draw the sobelish thing to
  private int _renderTargetId;
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier _idMapIdentifier;
  private RenderTargetIdentifier _renderTargetIdentifier;

  private static readonly int _tmpId = Shader.PropertyToID("_tmpRT");
  private RenderTargetIdentifier _tmpRT;

  private int _angleBlurSize;

  public SobelishAnglePass(string profilerTag, int renderTargetId, int angleBlurSize) {
    // set up the profiler so it has a slot in there
    _profilingSampler = new ProfilingSampler(profilerTag);
    _renderTargetId = renderTargetId;

    _angleBlurSize = angleBlurSize;

    // i get to choose when this pass happens!
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    var desc = renderingData.cameraData.cameraTargetDescriptor;

    _idMapIdentifier = new RenderTargetIdentifier(_idMapId);

    cmd.GetTemporaryRT(_renderTargetId, desc);
    _renderTargetIdentifier = new RenderTargetIdentifier(_renderTargetId);
    
    cmd.GetTemporaryRT(_tmpId, desc);
    _tmpRT = new RenderTargetIdentifier(_tmpId);
    
    ConfigureTarget(_renderTargetIdentifier);
    ConfigureClear(ClearFlag.Color, Color.clear);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
    opaqueDesc.depthBufferBits = 0;

    CommandBuffer cmd = CommandBufferPool.Get();
    using (new ProfilingScope(cmd, _profilingSampler)) {
      cmd.Blit(_idMapIdentifier, _renderTargetIdentifier, _sobelishMaterial);

      // blur the sobel direction vectors (in y and z of the render texture)
      cmd.SetGlobalInt("_KernelSize", _angleBlurSize);
      // blur the distance field
      cmd.Blit(_renderTargetIdentifier, _tmpRT, _boxBlurMaterial, 0);
      cmd.Blit(_tmpRT, _renderTargetIdentifier, _boxBlurMaterial, 1);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }

  public override void OnCameraCleanup(CommandBuffer cmd) {
    cmd.ReleaseTemporaryRT(_idMapId);
    cmd.ReleaseTemporaryRT(_tmpId);
  }
}