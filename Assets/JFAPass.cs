using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class JFAPass : ScriptableRenderPass {
  ProfilingSampler _profilingSampler;
  public Material _jfaMaterial;
  private int _inputRenderTargetId;
  private RenderTargetIdentifier _inputRenderTargetIdentifier;

  private int _outlineWidth;
  int _tmpId1, _tmpId2;
  RenderTargetIdentifier _tmpRT1, _tmpRT2;
  public JFAPass(int outlineWidth, string profilerTag, int renderTargetId) {
    _profilingSampler = new ProfilingSampler(profilerTag);
    _inputRenderTargetId = renderTargetId;
    _outlineWidth = outlineWidth;
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _inputRenderTargetIdentifier = new RenderTargetIdentifier(_inputRenderTargetId);
  }

  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
    _tmpId1 = Shader.PropertyToID("tmpBlurRT1");
    _tmpId2 = Shader.PropertyToID("tmpBlurRT2");
    cmd.GetTemporaryRT(_tmpId1, cameraTextureDescriptor);
    cmd.GetTemporaryRT(_tmpId2, cameraTextureDescriptor);

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
      cmd.Blit(_inputRenderTargetIdentifier, _tmpRT1, _jfaMaterial, 0);

      for (int jump = maxPassSize; jump >= 1; jump /= 2) {
        cmd.SetGlobalInt(Shader.PropertyToID("_jumpDistance"), jump);
        cmd.Blit(_tmpRT1, _tmpRT2, _jfaMaterial, 1);

        // ping pong
        var rttmp = _tmpRT1;
        _tmpRT1 = _tmpRT2;
        _tmpRT2 = rttmp;
      }
      
      cmd.SetGlobalFloat(Shader.PropertyToID("_lineThickness"), _outlineWidth);
      // final pass
      cmd.Blit(_tmpRT1, renderingData.cameraData.renderer.cameraColorTarget, _jfaMaterial, 2);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }
}