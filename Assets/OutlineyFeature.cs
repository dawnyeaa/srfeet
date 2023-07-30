using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutlineyFeature : ScriptableRendererFeature {
  private IDPass _idPass;
  private SobelishPass _sobelishPass;
  private JFAPass _jfaPass;

  [SerializeField] LayerMask _layerMask;
  [SerializeField] Material _sobelishMaterial;
  [SerializeField] ComputeShader _jfaComputeShader;

  public override void Create() {
    int JFAInputRT = Shader.PropertyToID("_JFAInputRT");
    _idPass = new IDPass("ID Pass", _layerMask);
    _sobelishPass = new SobelishPass("Sobelish Pass", JFAInputRT) {
      _sobelishMaterial = _sobelishMaterial
    };
    _jfaPass = new JFAPass(_jfaComputeShader, "Init", "Jump", "JFA Pass", JFAInputRT);
  }

  public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
    renderer.EnqueuePass(_idPass);
    renderer.EnqueuePass(_sobelishPass);
    renderer.EnqueuePass(_jfaPass);
  }
}
