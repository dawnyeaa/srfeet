using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class ConeGeneratorWindow : EditorWindow {
  int coneSides = 6;
  float coneRadius = 0.5f;
  float coneHeight = 1;

  [MenuItem("Tools/Cone Generator")]
  public static void ShowWindow() {
    GetWindow<ConeGeneratorWindow>("Cone Generator");
  }

  void OnGUI() {
    coneSides = Mathf.Max(EditorGUILayout.IntField("Cone Sides", coneSides), 3);
    coneRadius = Mathf.Max(EditorGUILayout.FloatField("Cone Radius", coneRadius), Mathf.Epsilon);
    coneHeight = Mathf.Max(EditorGUILayout.FloatField("Cone Height", coneHeight), Mathf.Epsilon);
    if (GUILayout.Button("Create Cone")) {
      CreateCone();
    }
  }

  private void CreateCone() {
    var mesh = new Mesh {
      name = $"cone-{coneSides}"
    };

    var verts = new Vector3[coneSides+1];
    var tris = new int[coneSides * 3];

    verts[0] = Vector3.zero;
    for (int i = 0; i < coneSides; ++i) {
      var ang = i * (Mathf.PI * 2) / coneSides;
      verts[i+1] = new(Mathf.Cos(ang) * coneRadius, Mathf.Sin(ang) * coneRadius, coneHeight);

      tris[i*3] = 0;
      tris[(i*3)+1] = (i+2 > coneSides) ? 1 : i+2;
      tris[(i*3)+2] = i+1;
    }

    mesh.vertices = verts;
    mesh.triangles = tris;

    AssetDatabase.DeleteAsset($"Assets/{mesh.name}.asset");
    AssetDatabase.CreateAsset(mesh, $"Assets/{mesh.name}.asset");
  }
}