using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class JFAPass : ScriptableRenderPass {
  ProfilingSampler _profilingSampler;
  public Material _jfaMaterial;
  public Material _boxBlurMaterial;
  public Material _dfOutlineMaterial;
  private int _inputRenderTargetId;
  private int _osSobelTexId;
  private RenderTargetIdentifier _inputRenderTargetIdentifier;
  private RenderTargetIdentifier _osSobelTex;

  private int _outlineWidth;
  int _tmpId1, _tmpId2;
  RenderTargetIdentifier _tmpRT1, _tmpRT2;
  public JFAPass(int outlineWidth, string profilerTag, int renderTargetId, int osSobelTexId) {
    _profilingSampler = new ProfilingSampler(profilerTag);
    _inputRenderTargetId = renderTargetId;
    _osSobelTexId = osSobelTexId;
    _outlineWidth = outlineWidth;
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _inputRenderTargetIdentifier = new RenderTargetIdentifier(_inputRenderTargetId);
    _osSobelTex = new RenderTargetIdentifier(_osSobelTexId);
  }

  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
    RenderTextureDescriptor desc = cameraTextureDescriptor;
    desc.colorFormat = RenderTextureFormat.ARGBFloat;
    _tmpId1 = Shader.PropertyToID("tmpJFART1");
    _tmpId2 = Shader.PropertyToID("tmpJFART2");
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

      cmd.Blit(_inputRenderTargetIdentifier, _tmpRT1, _jfaMaterial, 0);

      for (int jump = maxPassSize; jump >= 1; jump /= 2) {
        cmd.SetGlobalInt(Shader.PropertyToID("_jumpDistance"), jump);
        cmd.Blit(_tmpRT1, _tmpRT2, _jfaMaterial, 1);

        // ping pong
        (_tmpRT2, _tmpRT1) = (_tmpRT1, _tmpRT2);
      }

      cmd.SetGlobalTexture(Shader.PropertyToID("_OSSobel"), _osSobelTex);
      
      // create the distance field
      cmd.Blit(_tmpRT1, _tmpRT2, _jfaMaterial, 2);

      // blur the distance field
      cmd.Blit(_tmpRT2, _tmpRT1, _boxBlurMaterial, 0);
      cmd.Blit(_tmpRT1, _tmpRT2, _boxBlurMaterial, 1);

      // turn it into outline on screen
      cmd.Blit(renderingData.cameraData.renderer.cameraColorTarget, _tmpRT1);
      cmd.SetGlobalTexture(Shader.PropertyToID("_Screen"), _tmpRT1);
      cmd.SetGlobalFloat(Shader.PropertyToID("_lineThickness"), _outlineWidth);
      cmd.Blit(_tmpRT2, renderingData.cameraData.renderer.cameraColorTarget, _dfOutlineMaterial);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }
}