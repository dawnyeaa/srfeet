using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineyFeature : ScriptableRendererFeature {
  private IDPass _idPass;
  private SobelishPass _sobelishPass;

  [SerializeField] LayerMask _layerMask;
  [SerializeField] Material _sobelishMaterial;

  public override void Create() {
    _idPass = new IDPass("ID Pass", _layerMask);
    _sobelishPass = new SobelishPass("Sobelish Pass", "_JFAPassRT") {
      _sobelishMaterial = _sobelishMaterial
    };
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    renderer.EnqueuePass(_idPass);
    renderer.EnqueuePass(_sobelishPass);
  }
}
