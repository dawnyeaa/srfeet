using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SobelishPass : ScriptableRenderPass {
  // the material with our sobelish shader on it
  public Material _sobelishMaterial;
  private ProfilingSampler _profilingSampler;
  // the render target that we're grabbing the id map from
  private static readonly int _idMapId = Shader.PropertyToID("_IDPassRT");
  // making an id for the render target we're gonna draw the sobelish thing to
  private int _renderTargetId;
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier _idMapIdentifier;
  private RenderTargetIdentifier _renderTargetIdentifier;

  public SobelishPass(string profilerTag, int renderTargetId) {
    // set up the profiler so it has a slot in there
    _profilingSampler = new ProfilingSampler(profilerTag);
    _renderTargetId = renderTargetId;

    // i get to choose when this pass happens!
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _idMapIdentifier = new RenderTargetIdentifier(_idMapId);
  }

  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
    cmd.GetTemporaryRT(_renderTargetId, cameraTextureDescriptor);
    _renderTargetIdentifier = new RenderTargetIdentifier(_renderTargetId);
    
    ConfigureTarget(_renderTargetIdentifier);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
    opaqueDesc.depthBufferBits = 0;

    CommandBuffer cmd = CommandBufferPool.Get();
    using (new ProfilingScope(cmd, _profilingSampler)) {
      cmd.SetRenderTarget(_renderTargetIdentifier);
      cmd.ClearRenderTarget(true, true, Color.clear);

      cmd.Blit(_idMapIdentifier, _renderTargetIdentifier, _sobelishMaterial);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }
}