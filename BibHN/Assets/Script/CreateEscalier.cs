using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class CreateEscalier : MonoBehaviour
{

    public float LargeurEscalier;
    public int HauteurEscalier;
    public float HauteurMarche;
    public float ProfondeurMarche;
    Mesh p_mesh;
    Vector3[] p_vertices;
    int[] p_triangles;

    void Start()
    {
        p_mesh = new Mesh();
        p_mesh.name = "MyEscalier";

        p_vertices = new Vector3[6 * HauteurEscalier];

        int indice = 0;
        int hauteur = 0;
        for (int i = 0; i < HauteurEscalier; i++)
        {
            p_vertices[indice++] = new Vector3(ProfondeurMarche, hauteur, -LargeurEscalier);
            p_vertices[indice++] = new Vector3(ProfondeurMarche, hauteur, LargeurEscalier);
            p_vertices[indice++] = new Vector3(-ProfondeurMarche, hauteur, LargeurEscalier);
            p_vertices[indice++] = new Vector3(-ProfondeurMarche, hauteur, -LargeurEscalier);
            p_vertices[indice++] = new Vector3(-ProfondeurMarche, hauteur + HauteurMarche, LargeurEscalier);
            p_vertices[indice++] = new Vector3(-ProfondeurMarche, hauteur + HauteurMarche, -LargeurEscalier);
        }

        p_triangles = new int[12 * HauteurEscalier];
        indice = 0;
        for (int i = 0; i < HauteurEscalier; i++)
        {
            p_triangles[indice++] = i;
            p_triangles[indice++] = i+2;
            p_triangles[indice++] = i+1;
            //p_triangles[indice++] = i+3;
            //p_triangles[indice++] = i+2;
            //p_triangles[indice++] = i;
            //p_triangles[indice++] = i+3;
            //p_triangles[indice++] = i+4;
            //p_triangles[indice++] = i+2;
            //p_triangles[indice++] = i+3;
            //p_triangles[indice++] = i+5;
            //p_triangles[indice++] = i+4;
        }

        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;
        GetComponent<MeshFilter>().mesh = p_mesh;
        GetComponent<MeshFilter>().mesh.RecalculateNormals();
    }
}
