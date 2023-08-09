using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class StrokeQuadPass : ScriptableRenderPass {
  ComputeBuffer drawArgsBuffer;
  ComputeBuffer quadPoints;

  ProfilingSampler _profilingSampler;
  ComputeShader _strokeQuadCompute;
  
  private int _inputRenderTargetId;
  private int _strokeyQuadsKernel;
  public StrokeQuadPass(ComputeShader strokeQuadCompute, string profilerTag, int renderTargetId) {
    _profilingSampler = new ProfilingSampler(profilerTag);
    _inputRenderTargetId = renderTargetId;
    _strokeQuadCompute = strokeQuadCompute;

    _strokeyQuadsKernel = _strokeQuadCompute.FindKernel("StrokeyQuads");

    quadPoints = new ComputeBuffer(1000, sizeof(uint)*2, ComputeBufferType.Append);

    drawArgsBuffer = new ComputeBuffer(4, sizeof(uint), ComputeBufferType.IndirectArguments);

    drawArgsBuffer.SetData(new uint[] {
      6, // vertices per instance
      0, // instance count
      0, // byte offset of first vertex
      0 // byte offset of first instance
    });
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    var cmd = CommandBufferPool.Get();
    cmd.Clear();

    cmd.SetComputeTextureParam(_strokeQuadCompute, _strokeyQuadsKernel, _inputRenderTargetId, renderingData.cameraData.camera.activeTexture);
  }
}