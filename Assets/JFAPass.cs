using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class JFAPass : ScriptableRenderPass {
  private ComputeShader _jfaComputeShader;
  private string _initKernelName, _jumpKernelName;
  private ProfilingSampler _profilingSampler;
  // the id that we're gonna store for our input and output render target
  private int _inputRenderTargetId;
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier _inputRenderTargetIdentifier;
  private int _tmpId;
  private RenderTargetIdentifier _tmpRT;
  private int _renderTargetWidth, _renderTargetHeight;

  public JFAPass(ComputeShader jfaComputeShader, string initKernelName, string jumpKernelName, string profilerTag, int renderTargetId) {
    _profilingSampler = new ProfilingSampler(profilerTag);
    _inputRenderTargetId = renderTargetId;
    _jfaComputeShader = jfaComputeShader;
    _initKernelName = initKernelName;
    _jumpKernelName = jumpKernelName;
    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _tmpId = Shader.PropertyToID("_JFAPoints");

    var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
    _inputRenderTargetIdentifier = new RenderTargetIdentifier(_inputRenderTargetId);
    cameraTargetDescriptor.enableRandomWrite = true;

    cmd.GetTemporaryRT(_tmpId, cameraTargetDescriptor);
    _tmpRT = new RenderTargetIdentifier(_tmpId);

    
    _renderTargetWidth = cameraTargetDescriptor.width;
    _renderTargetHeight = cameraTargetDescriptor.height;
  }

  public override void OnCameraCleanup(CommandBuffer cmd) {
    cmd.ReleaseTemporaryRT(_tmpId);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    var cmd = CommandBufferPool.Get();
    var initKernel = _jfaComputeShader.FindKernel(_initKernelName);
    var jumpKernel = _jfaComputeShader.FindKernel(_jumpKernelName);

    _jfaComputeShader.GetKernelThreadGroupSizes(initKernel, out uint xGroupSize, out uint yGroupSize, out _);

    using (new ProfilingScope(cmd, _profilingSampler)) {
      cmd.Blit(_inputRenderTargetIdentifier, _tmpRT);
      cmd.SetComputeTextureParam(_jfaComputeShader, initKernel, _tmpId, _tmpRT);
      cmd.SetComputeIntParam(_jfaComputeShader, "_ResultWidth", _renderTargetWidth);
      cmd.SetComputeIntParam(_jfaComputeShader, "_ResultHeight", _renderTargetHeight);
    
      cmd.DispatchCompute(_jfaComputeShader, initKernel, Mathf.CeilToInt(_renderTargetWidth / xGroupSize), Mathf.CeilToInt(_renderTargetHeight / yGroupSize), 1);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }
}