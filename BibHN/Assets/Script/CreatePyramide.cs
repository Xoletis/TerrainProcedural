using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CreatePyramide : MonoBehaviour
{
    public float c;
    public float h;

    private Vector3[] p_vertices;
    private Vector3[] p_normals;
    private int[] p_triangles;
    private Mesh p_mesh;

    private Color32[] p_couleurs;

    private void Start()
    {
        float cc = c / 2.0f;
        p_mesh = new Mesh();
        p_mesh.Clear();
        p_mesh.name = "MyMeshPyramide";
        p_vertices = new Vector3[5];
        p_vertices[0] = new Vector3(-cc, 0, -cc);
        p_vertices[1] = new Vector3(-cc, 0, +cc);
        p_vertices[2] = new Vector3(+cc, 0, +cc);
        p_vertices[3] = new Vector3(+cc, 0, -cc);
        p_vertices[4] = new Vector3(0, h, 0);

        p_triangles = new int[]
        {
            0,2,1,
            0,3,2,
            0,1,4,
            1,2,4,
            2,3,4,
            3,0,4
        };

        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;

        p_couleurs = new Color32[p_vertices.Length];
        p_couleurs[0] = new Color32(255, 0, 0, 0);
        p_couleurs[1] = new Color32(0, 255, 0, 0);
        p_couleurs[2] = new Color32(0, 0, 255, 0);
        p_mesh.colors32 = p_couleurs;

        GetComponent<MeshFilter>().mesh = p_mesh;
        GetComponent<MeshFilter>().mesh.RecalculateNormals();
    }
}
