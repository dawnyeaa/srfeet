using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PoissonArrangementObject", order = 1)]
public class PoissonArrangementObject : ScriptableObject {
  public Vector2[] points;
  public Vector4[] points4;
  public Vector2[] tiledPoints;
  public Vector4[] tiledPoints4;
}