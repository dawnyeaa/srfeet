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
  private Texture2D _strokeTex;
  private float _strokeDensity;
  private float _strokeSize;
  private float _strokeWidth;
  private float _strokeHeight;
  private Vector4 _strokeRandomSizeBounds;
  private Vector4[] _poissonPointsArray;
  private PoissonArrangementObject _poissonPointsArrangement;

  private ComputeBuffer _poissonPoints;

  private int _scanSize;

  public StrokeQuadPass(ComputeShader strokeQuadCompute, 
                        string profilerTag, 
                        int renderTargetId, 
                        int voronoiTexId, 
                        PoissonArrangementObject poissonPoints, 
                        int scanSize, 
                        Texture2D strokeTexture,
                        float strokeDensity,
                        float strokeSize,
                        float strokeWidth,
                        float strokeHeight,
                        Vector2 strokeRandomWidthBounds,
                        Vector2 strokeRandomHeightBounds) {
    _profilingSampler = new ProfilingSampler(profilerTag);
    _inputRenderTargetId = renderTargetId;
    _voronoiTexId = voronoiTexId;
    _strokeQuadCompute = strokeQuadCompute;

    _scanSize = scanSize;

    _poissonPointsArrangement = poissonPoints;
    // _poissonPointsArray = poissonPoints.tiledPoints4;

    _strokeTex = strokeTexture;

    _strokeDensity = strokeDensity;

    _strokeSize = strokeSize;
    _strokeWidth = strokeWidth;
    _strokeHeight = strokeHeight;
    _strokeRandomSizeBounds = new(strokeRandomWidthBounds.x, strokeRandomWidthBounds.y, strokeRandomHeightBounds.x, strokeRandomHeightBounds.y);

    _strokeyQuadsKernel = _strokeQuadCompute.FindKernel("StrokeyQuads");

    _quadPointsId = Shader.PropertyToID("_quadPoints");

    _quadPoints = new ComputeBuffer(poissonPoints.points.Length, sizeof(uint)*2 + sizeof(float) + sizeof(uint)*2, ComputeBufferType.Append);

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

    if (_poissonPoints == null)
      _poissonPoints = new ComputeBuffer(_poissonPointsArrangement.points.Length, Marshal.SizeOf(typeof(Vector4)));
    _poissonPoints.SetData(_poissonPointsArrangement.points);
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
      cmd.SetComputeFloatParam(_strokeQuadCompute, "_pointDensity", _strokeDensity);
      cmd.SetComputeIntParam(_strokeQuadCompute, "_RTWidth", camRTDesc.width);
      cmd.SetComputeIntParam(_strokeQuadCompute, "_RTHeight", camRTDesc.height);

      cmd.SetComputeBufferParam(_strokeQuadCompute, _strokeyQuadsKernel, _quadPointsId, _quadPoints);

      cmd.DispatchCompute(_strokeQuadCompute, _strokeyQuadsKernel,
                          Mathf.CeilToInt(_poissonPointsArrangement.points.Length * 2f / 64f),
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
      properties.SetTexture(Shader.PropertyToID("_MainTex"), _strokeTex);
      properties.SetFloat(Shader.PropertyToID("_Size"), _strokeSize);
      properties.SetFloat(Shader.PropertyToID("_Width"), _strokeWidth);
      properties.SetFloat(Shader.PropertyToID("_Height"), _strokeHeight);
      properties.SetVector(Shader.PropertyToID("_RandSizeBounds"), _strokeRandomSizeBounds);

      cmd.DrawProceduralIndirect(Matrix4x4.identity, _quadMaterial, 0, MeshTopology.Triangles, _drawArgsBuffer, 0, properties);

    }

    context.ExecuteCommandBuffer(cmd);

    cmd.Clear();
    CommandBufferPool.Release(cmd);
  }

  public override void OnCameraCleanup(CommandBuffer cmd) {
    cmd.ReleaseTemporaryRT(_inputRenderTargetId);
    cmd.ReleaseTemporaryRT(_voronoiTexId);
  }

  public void Dispose() {
    _quadPoints?.Dispose();
    _drawArgsBuffer?.Dispose();
  }
}