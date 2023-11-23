using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VoronoiPass : ScriptableRenderPass {
  private ProfilingSampler _profilingSampler;
  public Mesh _voronoiMesh;
  public Material _voronoiMaterial;

  private Vector4[] _points;
  private ComputeBuffer _poissonPointsBuffer;

  private int _voronoiRenderTargetId;

  private RenderTargetIdentifier _voronoiRenderTarget;

  public VoronoiPass(string profilerTag, int voronoiRenderTargetId, PoissonArrangementObject poissonPoints) {
    _profilingSampler = new ProfilingSampler(profilerTag);

    _voronoiRenderTargetId = voronoiRenderTargetId;

    _points = poissonPoints.points;

    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
    desc.colorFormat = RenderTextureFormat.ARGBFloat;

    cmd.GetTemporaryRT(_voronoiRenderTargetId, desc);

    _voronoiRenderTarget = new RenderTargetIdentifier(_voronoiRenderTargetId);

    if (_poissonPointsBuffer == null)
      _poissonPointsBuffer = new ComputeBuffer(_points.Length, Marshal.SizeOf(typeof(Vector4)));
    _poissonPointsBuffer.SetData(_points);

    ConfigureTarget(_voronoiRenderTarget);
    ConfigureClear(ClearFlag.All, Color.clear);
  }

  public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
                                   MaterialPropertyBlock properties = null, int shaderPassId = 0) {
    commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    var cmd = CommandBufferPool.Get();

    using (new ProfilingScope(cmd, _profilingSampler)) {
      MaterialPropertyBlock properties = new();

      properties.SetInt("_PositionsSize", _points.Length);
      properties.SetBuffer("_Positions", _poissonPointsBuffer);
      cmd.DrawMesh(_voronoiMesh, Matrix4x4.identity, _voronoiMaterial, 0, 0, properties);
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }

  public override void OnCameraCleanup(CommandBuffer cmd) {
  }

  public void Dispose() {
    _poissonPointsBuffer?.Dispose();
  }
}