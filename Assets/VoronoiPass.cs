using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VoronoiPass : ScriptableRenderPass {
  private ProfilingSampler _profilingSampler;
  public Mesh _voronoiConeMesh;
  public Material _voronoiConeMat;
  private int _voronoiRenderTargetId;
  private PoissonArrangementObject _poissonPoints;
  private Vector4[] _modifiedSeedPoints;
  private Vector4[] _seedPoints;
  private const int SEEDPOINTSSIZE = 2048;

  private RenderTargetIdentifier _voronoiRenderTarget;

  private Vector4[] _coneColors;
  private Matrix4x4[] _coneMatrices;

  public VoronoiPass(string profilerTag, int voronoiRenderTargetId, PoissonArrangementObject poissonPoints) {
    _profilingSampler = new ProfilingSampler(profilerTag);

    _voronoiRenderTargetId = voronoiRenderTargetId;
    _poissonPoints = poissonPoints;

    _seedPoints = poissonPoints.tiledPoints4;

    renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
  }

  public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
    RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
    desc.colorFormat = RenderTextureFormat.ARGBFloat;

    cmd.GetTemporaryRT(_voronoiRenderTargetId, desc);

    _voronoiRenderTarget = new RenderTargetIdentifier(_voronoiRenderTargetId);

    ConfigureTarget(_voronoiRenderTarget);
    ConfigureClear(ClearFlag.All, Color.clear);
  }

  public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
    var cmd = CommandBufferPool.Get();

    using (new ProfilingScope(cmd, _profilingSampler)) {
      var batchCount = Mathf.CeilToInt(_seedPoints.Length / 511f);
      uint id = 0;

      for (int batch = 0; batch < batchCount; ++batch) {
        var batchSize = (batch == batchCount-1) ? _seedPoints.Length % 511 : 511;
        MaterialPropertyBlock properties = new();
        _coneColors = new Vector4[batchSize];
        _coneMatrices = new Matrix4x4[batchSize];

        for (int i = 0; i < batchSize; ++i) {
          Vector3 position = _seedPoints[batch*511 + i];
          Quaternion rotation = Quaternion.identity;
          Vector3 scale = new(200, 200, 1);

          _coneMatrices[i] = Matrix4x4.TRS(position, rotation, scale);
          _coneColors[i] = new Vector4(id++, 0, 1, 1);

          // ok new idea
          // for each poisson point
          // check inside either box or circle with diameter of 50px (for this current density) and find the smallest masked distance (if it exists)
          // badabing badaboom

          // OH GOD THE POSITIONS OF THE CONES PROBABLY DOESNT LINE UP WITH THE POSITIONS IN THE BUFFER AND BEING READ AND SHIT AAAAAAAAAA

          // Random.InitState((int)position.x);
          // var r = Random.value;
          // Random.InitState((int)position.y);
          // var g = Random.value;
          // _coneColors[i] = new Vector4(r, g, 1, 1);
        }

        properties.SetVectorArray("_Colors", _coneColors);
        cmd.DrawMeshInstanced(_voronoiConeMesh, 0, _voronoiConeMat, 0, _coneMatrices, batchSize, properties);
      }
    }

    context.ExecuteCommandBuffer(cmd);
    cmd.Clear();

    CommandBufferPool.Release(cmd);
  }

  public override void OnCameraCleanup(CommandBuffer cmd) {

  }
}