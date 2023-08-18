using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class IDPass : ScriptableRenderPass {
  private readonly ProfilingSampler _profilingSampler;
  // i think this is the variable thats gonna store our filter decided by the layer mask
  private FilteringSettings _filteringSettings;
  // this is the pass to find in shaders to run
  private static readonly ShaderTagId _shaderTag = new("ID");
  
  private readonly bool _useMSAA;
  private readonly int _msaaSamples;

  // making an id for the render target we're gonna draw the id map to
  private readonly int _renderTargetId;
  private readonly int _osRTId;
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier[] _renderTargetIdentifiers;
  private RenderTargetIdentifier _depthBufTemp;

  public IDPass(string profilerTag, LayerMask layerMask, bool useMSAA, int msaaSamples, int idRTId, int osRTId) {
    // set up the profiler so it has a slot in there
    _profilingSampler = new ProfilingSampler(profilerTag);

    // set up that filter from the layer mask i mentioned earlier
    _filteringSettings = new FilteringSettings(null, layerMask);

    _useMSAA = useMSAA;
    _msaaSamples = msaaSamples;

    _renderTargetId = idRTId;
    _osRTId = osRTId;

    // i get to choose when this pass happens!
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    RenderTextureDescriptor blitTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
    blitTargetDescriptor.colorFormat = RenderTextureFormat.ARGB32;
    if (_useMSAA) blitTargetDescriptor.msaaSamples = _msaaSamples;
    cmd.GetTemporaryRT(_renderTargetId, blitTargetDescriptor);
    cmd.GetTemporaryRT(_osRTId, blitTargetDescriptor);
    cmd.GetTemporaryRT(Shader.PropertyToID("_depthBufTemp"), renderingData.cameraData.cameraTargetDescriptor);
    _renderTargetIdentifiers = new RenderTargetIdentifier[2];
    _renderTargetIdentifiers[0] = new RenderTargetIdentifier(_renderTargetId);
    _renderTargetIdentifiers[1] = new RenderTargetIdentifier(_osRTId);
    _depthBufTemp = new RenderTargetIdentifier(Shader.PropertyToID("_depthBufTemp"));
    ConfigureClear(ClearFlag.All, Color.clear);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    // some settings we need for drawing the draw renderers
    var drawingSettings = CreateDrawingSettings(_shaderTag, ref renderingData, SortingCriteria.CommonOpaque);
    
    var cmd = CommandBufferPool.Get();
    // make sure its shown in its own profiler step
    using (new ProfilingScope(cmd, _profilingSampler)) {
      ConfigureTarget(_renderTargetIdentifiers);
      cmd.SetRenderTarget(_renderTargetIdentifiers, _depthBufTemp);
      // do the rendering!!!
      context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
      cmd.ReleaseTemporaryRT(Shader.PropertyToID("_depthBufTemp"));
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();
    CommandBufferPool.Release(cmd);      
  }
}