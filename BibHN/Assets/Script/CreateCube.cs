using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CreateCube : MonoBehaviour
{
    public enum TypeCube { Cube24, Cube8, None }
    public TypeCube typeCube;
    public float width = 1.0f;
    private Color32[] p_couleurs;
    Mesh p_mesh;
    Vector3 p0, p1, p2, p3, p4, p5, p6, p7;
    Vector3[] p_vertices, p_normals;

    int[] p_triangles;

    void Awake()
    {
        float w = -width / 2.0f;
        float W = width / 2.0f;
        p0 = new Vector3(w, w, w);
        p1 = new Vector3(w, W, w);
        p2 = new Vector3(W, W, w);
        p3 = new Vector3(W, w, w);
        p4 = new Vector3(w, w, W);
        p5 = new Vector3(w, W, W);
        p6 = new Vector3(W, W, W);
        p7 = new Vector3(W, w, W);

        switch (typeCube)
        {
            case TypeCube.Cube8:
                CreatCube8();
                break;
            case TypeCube.Cube24:
                CreerCube24(); break;
            case TypeCube.None: break;
        }
    }

    void CreerCube24()
    {
        p_mesh = new Mesh();
        p_mesh.name = "MyProceduralCube24";

        p_vertices = new Vector3[]{
            p0,p1,p2,p3,  // devant
            p4,p5,p1,p0,  // gauche
            p3,p2,p6,p7,  // Droite
            p7,p6,p5,p4,  // Derrière
            p1,p5,p6,p2,  // Dessus
            p4,p0,p3,p7   // dessous
            };

        p_triangles = new int[12 * 3];
        int index = 0;
        for (int i = 0; i < 6; i++)   // 6 faces à 2 triangles
        {   // triangle 1
            p_triangles[index++] = i * 4;
            p_triangles[index++] = i * 4 + 1;
            p_triangles[index++] = i * 4 + 3;
            // triangle 2
            p_triangles[index++] = i * 4 + 1;
            p_triangles[index++] = i * 4 + 2;
            p_triangles[index++] = i * 4 + 3;
        }

        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;

        GetComponent<MeshFilter>().mesh = p_mesh;
        GetComponent<MeshFilter>().mesh.RecalculateNormals();
    }

    public void CreatCube8()
    {
        p_mesh = new Mesh();
        p_mesh.name = "MyProceduralCube8";

        p_vertices = new Vector3[]
        {
            p0, p1, p2, p3, p4, p5, p6, p7
        };

        p_triangles = new int[]
        {
            0,1,2,0,2,3, //devent
            3,2,6,3,6,7, //droite
            6,5,7,5,4,7, //dériére
            0,5,1,5,0,4, //gauche
            2,1,5,2,5,6, //dessus
            0,3,4,4,3,7 //dessous
        };

        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;

        GetComponent<MeshFilter>().mesh = p_mesh;
        GetComponent<MeshFilter>().mesh.RecalculateNormals();
    }
}
