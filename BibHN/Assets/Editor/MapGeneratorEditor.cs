using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainProcedural))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainProcedural mapGen = (TerrainProcedural)target;

        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }

        if (mapGen.mapMode == TerrainProcedural.MapMode.NoiseMap)
        {
            if (GUILayout.Button("Generate"))
            {
                mapGen.DrawMapInEditor();
            }
        }
    }
}
