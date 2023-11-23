using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PoissonArrangementObject", order = 1)]
public class PoissonArrangementObject : ScriptableObject {
  public Vector4[] points;
}
