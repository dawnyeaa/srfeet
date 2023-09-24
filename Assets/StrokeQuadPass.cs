using System.Runtime.InteropServices;
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
  private int _voronoiTexId;
  // an identifier SPECIFICALLY for the command buffer
  private RenderTargetIdentifier _inputRenderTargetIdentifier;
  private RenderTargetIdentifier _voronoiTexIdentifier;
  private int _quadPointsId;
  private int _strokeyQuadsKernel;
  private Vector4[] _poissonPointsArray;

  private ComputeBuffer _poissonPoints;

  private int _scanSize;

  public StrokeQuadPass(ComputeShader strokeQuadCompute, string profilerTag, int renderTargetId, int voronoiTexId, PoissonArrangementObject poissonPoints, int scanSize) {
    _profilingSampler = new ProfilingSampler(profilerTag);
    _inputRenderTargetId = renderTargetId;
    _voronoiTexId = voronoiTexId;
    _strokeQuadCompute = strokeQuadCompute;

    _scanSize = scanSize;

    _poissonPointsArray = poissonPoints.tiledPoints4;

    _strokeyQuadsKernel = _strokeQuadCompute.FindKernel("StrokeyQuads");

    _quadPointsId = Shader.PropertyToID("_quadPoints");

    _poissonPoints = new ComputeBuffer(_poissonPointsArray.Length, Marshal.SizeOf(typeof(Vector4)));
    _poissonPoints.SetData(_poissonPointsArray);

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

  public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
    _quadPoints.SetCounterValue(0);
  }
  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    _inputRenderTargetIdentifier = new RenderTargetIdentifier(_inputRenderTargetId);
    _voronoiTexIdentifier = new RenderTargetIdentifier(_voronoiTexId);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    var cmd = CommandBufferPool.Get();
    cmd.Clear();

    RenderTextureDescriptor camRTDesc = renderingData.cameraData.cameraTargetDescriptor;
    RenderTexture camRT = renderingData.cameraData.camera.activeTexture;
    
    using (new ProfilingScope(cmd, _profilingSampler)) {
      cmd.SetComputeTextureParam(_strokeQuadCompute, _strokeyQuadsKernel, _inputRenderTargetId, _inputRenderTargetIdentifier);
      cmd.SetComputeTextureParam(_strokeQuadCompute, _strokeyQuadsKernel, "_voronoiTex", _voronoiTexIdentifier);
      cmd.SetComputeBufferParam(_strokeQuadCompute, _strokeyQuadsKernel, Shader.PropertyToID("_poissonPoints"), _poissonPoints);
      cmd.SetComputeIntParam(_strokeQuadCompute, "_scanSize", _scanSize);
      cmd.SetComputeIntParam(_strokeQuadCompute, "_RTWidth", camRTDesc.width);
      cmd.SetComputeIntParam(_strokeQuadCompute, "_RTHeight", camRTDesc.height);

      cmd.SetComputeBufferParam(_strokeQuadCompute, _strokeyQuadsKernel, _quadPointsId, _quadPoints);

      cmd.DispatchCompute(_strokeQuadCompute, _strokeyQuadsKernel,
                          Mathf.CeilToInt(_poissonPointsArray.Length / 64f),
                          1,
                          1);
      
      cmd.CopyCounterValue(_quadPoints, _drawArgsBuffer, sizeof(uint));

      // _sobelBlitMat.SetTexture(Shader.PropertyToID("_Screen"), camRT);
      // cmd.Blit(_inputRenderTargetIdentifier, camRT, _sobelBlitMat, 0);
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