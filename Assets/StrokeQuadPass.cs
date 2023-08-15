using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StrokeQuadPass : ScriptableRenderPass {
  ComputeBuffer _drawArgsBuffer;
  ComputeBuffer _quadPoints;

  ProfilingSampler _profilingSampler;
  ComputeShader _strokeQuadCompute;
  public Material _quadMaterial;
  public Material _sobelBlitMat;
  // the id that we're gonna store for our input and output render target
  private int _inputRenderTargetId;
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier _inputRenderTargetIdentifier;
  private int _quadPointsId;
  private int _strokeyQuadsKernel;

  private RenderTargetIdentifier _camRT;

  private Texture2D _poissonTex;
  public StrokeQuadPass(ComputeShader strokeQuadCompute, string profilerTag, int renderTargetId, Texture2D poissonTex) {
    _profilingSampler = new ProfilingSampler(profilerTag);
    _inputRenderTargetId = renderTargetId;
    _strokeQuadCompute = strokeQuadCompute;
    _poissonTex = poissonTex;

    _strokeyQuadsKernel = _strokeQuadCompute.FindKernel("StrokeyQuads");

    _quadPointsId = Shader.PropertyToID("_quadPoints");

    _quadPoints = new ComputeBuffer(1000, sizeof(uint)*2 + sizeof(float), ComputeBufferType.Append);

    _drawArgsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);

    _drawArgsBuffer.SetData(new uint[] {
      6, // vertices per instance
      0, // instance count
      0, // byte offset of first vertex
      0 // byte offset of first instance
    });

    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public void SetTarget(RenderTargetIdentifier rt) {
    _camRT = rt;
  }

  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
    _quadPoints.SetCounterValue(0);
  }
  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _inputRenderTargetIdentifier = new RenderTargetIdentifier(_inputRenderTargetId);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    var cmd = CommandBufferPool.Get();
    cmd.Clear();

    RenderTextureDescriptor camRTDesc = renderingData.cameraData.cameraTargetDescriptor;
    
    using (new ProfilingScope(cmd, _profilingSampler)) {
      cmd.SetComputeTextureParam(_strokeQuadCompute, _strokeyQuadsKernel, _inputRenderTargetId, _inputRenderTargetIdentifier);
      cmd.SetComputeTextureParam(_strokeQuadCompute, _strokeyQuadsKernel, Shader.PropertyToID("_poissonTex"), _poissonTex);
      cmd.SetComputeFloatParam(_strokeQuadCompute, Shader.PropertyToID("_poissonSize"), _poissonTex.width);
      cmd.SetComputeBufferParam(_strokeQuadCompute, _strokeyQuadsKernel, _quadPointsId, _quadPoints);

      cmd.DispatchCompute(_strokeQuadCompute, _strokeyQuadsKernel,
                          Mathf.CeilToInt(camRTDesc.width / 32),
                          Mathf.CeilToInt(camRTDesc.height / 32),
                          1);
      
      cmd.CopyCounterValue(_quadPoints, _drawArgsBuffer, sizeof(uint));

      cmd.SetGlobalTexture(Shader.PropertyToID("_Screen"), renderingData.cameraData.renderer.cameraColorTarget);
      cmd.Blit(_inputRenderTargetIdentifier, renderingData.cameraData.renderer.cameraColorTarget, _sobelBlitMat, 0);
      // cmd.SetRenderTarget(camRT);

      MaterialPropertyBlock properties = new();
      properties.SetBuffer(_quadPointsId, _quadPoints);
      properties.SetFloat(Shader.PropertyToID("_WidthRatio"), renderingData.cameraData.camera.aspect);
      properties.SetFloat(Shader.PropertyToID("_ScreenSizeX"), camRTDesc.width);
      properties.SetFloat(Shader.PropertyToID("_ScreenSizeY"), camRTDesc.height);

      cmd.DrawProceduralIndirect(Matrix4x4.identity, _quadMaterial, 0, MeshTopology.Triangles, _drawArgsBuffer, 0, properties);

    }

    context.ExecuteCommandBuffer(cmd);

    cmd.Clear();
    CommandBufferPool.Release(cmd);
  }

  public override void OnCameraCleanup(CommandBuffer cmd) {}

  public void Dispose() {
    _quadPoints?.Dispose();
    _drawArgsBuffer?.Dispose();
  }
}