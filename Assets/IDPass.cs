using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class IDPass : ScriptableRenderPass {
  private ProfilingSampler _profilingSampler;
  // i think this is the variable thats gonna store our filter decided by the layer mask
  private FilteringSettings _filteringSettings;
  // this is the pass to find in shaders to run
  private static readonly ShaderTagId _shaderTag = new ShaderTagId("ID");
  // making an id for the render target we're gonna draw the id map to
  private static readonly int _renderTargetId = Shader.PropertyToID("_IDPassRT");
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier _renderTargetIdentifier;

  public IDPass(string profilerTag, LayerMask layerMask) {
    // set up the profiler so it has a slot in there
    _profilingSampler = new ProfilingSampler(profilerTag);

    // set up that filter from the layer mask i mentioned earlier
    _filteringSettings = new FilteringSettings(null, layerMask);

    // i get to choose when this pass happens!
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
    blitTargetDescriptor.colorFormat = RenderTextureFormat.ARGB32;
    cmd.GetTemporaryRT(_renderTargetId, blitTargetDescriptor);
    _renderTargetIdentifier = new RenderTargetIdentifier(_renderTargetId);
    ConfigureTarget(_renderTargetIdentifier);
    ConfigureClear(ClearFlag.All, Color.clear);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    // some settings we need for drawing the draw renderers
    var drawingSettings = CreateDrawingSettings(_shaderTag, ref renderingData, SortingCriteria.CommonOpaque);
    
    var cmd = CommandBufferPool.Get();
    // make sure its shown in its own profiler step
    using (new ProfilingScope(cmd, _profilingSampler)) {
      // do the rendering!!!
      context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();
    CommandBufferPool.Release(cmd);      
  }
}