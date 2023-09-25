using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PoissonArrangementTilerWindow : EditorWindow {
  private PoissonArrangementObject poissonPoints;
  private int targetWidth = 1920, targetHeight = 1080;
  private int poissonTexSize = 512;

  [MenuItem("Tools/Poisson Arrangement Tiler")]
  public static void ShowWindow() {
    GetWindow<PoissonArrangementTilerWindow>("Poisson Arrangement Tiler");
  }

  void OnGUI() {
    poissonPoints = (PoissonArrangementObject)EditorGUILayout.ObjectField("Poisson Arrangement", poissonPoints, typeof(PoissonArrangementObject), true);
    if (GUILayout.Button("Create Vector4s")) {
      CreateVec4s();
    }
    targetWidth = Mathf.Max(EditorGUILayout.IntField("Target Width", targetWidth), 1);
    targetHeight = Mathf.Max(EditorGUILayout.IntField("Target Height", targetHeight), 1);
    poissonTexSize = Mathf.Max(EditorGUILayout.IntField("Poisson Arrangement Dimensions", poissonTexSize), 8);
    if (GUILayout.Button("Tile Poisson Points")) {
      TileArrangement();
    }
  }

  public void CreateVec4s() {
    Vector2[] seedPoints = poissonPoints.points;
    Vector4[] vec4Points = new Vector4[seedPoints.Length];
    for (int i = 0; i < seedPoints.Length; ++i) {
      vec4Points[i] = new Vector4(seedPoints[i].x, seedPoints[i].y);
    }
    poissonPoints.points4 = vec4Points;
  }

  public void TileArrangement() {
    Vector4[] seedPoints = poissonPoints.points4;

    var xTileAmount = Mathf.CeilToInt(targetWidth/(float)poissonTexSize);
    var yTileAmount = Mathf.CeilToInt(targetHeight/(float)poissonTexSize);

    Vector4[] _modifiedSeedPoints = new Vector4[xTileAmount*yTileAmount*seedPoints.Length];

    Vector4 right = new(0, 1);
    Vector4 up = new(1, 0);

    Vector4 point;
    for (int i = 0; i < seedPoints.Length; ++i) {
      point = seedPoints[i];
      
      for (int xTile = 0; xTile < xTileAmount; ++xTile) {
        _modifiedSeedPoints[xTile*seedPoints.Length + i] = point + poissonTexSize * xTile * right;
        for (int yTile = 1; yTile < yTileAmount; ++yTile) {
          _modifiedSeedPoints[xTileAmount*seedPoints.Length*yTile+xTile*seedPoints.Length + i] = point + poissonTexSize * yTile * up + poissonTexSize * xTile * right;
        }
      }
    }

    poissonPoints.tiledPoints4 = _modifiedSeedPoints;
  }
}
