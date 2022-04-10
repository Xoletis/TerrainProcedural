using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class DistortGO : MonoBehaviour
{
    public enum TypeDeDeformationDesVerticesProches {RIGIDE, ADAPTATIVE};

    [Tooltip("le GameObject qui sera placé en chaque sommet")]
    public GameObject PickObj;
    Dictionary<int, List<int>> les_po = new Dictionary<int, List<int>>();
    public enum TypeCalculNormales { CalculCiblé, CalculAutomatique }
    public TypeCalculNormales typeCalculNormales;

    public Material matNonSharedVertice, matSharedVertice, matSelectedVertice, matHiglitedVertice;

    public bool deformeNearVertice = false;

    public TypeDeDeformationDesVerticesProches typeDeDeformationDesVerticesProches;

    public float maxRangeForDeformNearVertice = 3.0f;

    public bool showVerticeDragable = true;

    Dictionary<int, int> VertexVoisinDic = new Dictionary<int, int>();

    void Start()
    {
        Mesh p_mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] p_vertices = p_mesh.vertices;

        const float SEUIL_DISTANCE_VERTICES_SIMILAIRES = 0.01f;
        bool[] bool_vert = new bool[p_vertices.Length];
        // indique si un vertex a été traité / faux par defaut
        for (int i = 0; i < bool_vert.Length; i++) bool_vert[i] = false;
        // l'ensemble des PickingObjects , chacun a un identifiant (int) unique GetInstanceID
        // à chaque PickingObjetc est associé une liste de vertices « similaires »
        // un vertex est jugé similaire d'un autre s'il se trouve à une
        // distance négligeable, i.e. inférieure à un seuil
        // la structure de données les_po permet d’associer une liste de vertices à  
        // un identifiant de pickingObject  
        int index_vert = 0;
        int nb_pickingObjects = 0;

        while (index_vert < p_vertices.Length)          // traiter tous les vertices
        {
            if (!bool_vert[index_vert])
            {
                bool isVerticesSimilaires = false;
                bool_vert[index_vert] = true;
               
                foreach (List<int> item in les_po.Values)
                {
                    if (Vector3.Distance(p_vertices[index_vert], p_vertices[item[0]]) <= SEUIL_DISTANCE_VERTICES_SIMILAIRES)
                    {
                        //Debug.Log(Vector3.Distance(p_vertices[index_vert], p_vertices[item[0]]) + "");
                        item.Add(index_vert);
                        isVerticesSimilaires = true;
                    }
                }
                if (!isVerticesSimilaires)
                {
                    // ajouter le PickObject à la position du vertex
                    GameObject po = Instantiate(PickObj);
                    po.name = "po" + po.GetInstanceID();
                    po.GetComponent<DragVertice>().distort = this;
                    nb_pickingObjects++;
                    //po.transform.position = transform.position + p_vertices[index_vert];
                    // idem ci dessous
                    po.transform.position = transform.TransformPoint(p_vertices[index_vert]);
                    // TransformPoint converts the vertex's local position into world space
                    les_po.Add(po.GetInstanceID(), new List<int> { index_vert });
                    if (!showVerticeDragable)
                    {
                        po.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
            index_vert++;
        }

        foreach(int id in les_po.Keys)
        {
            setColor(GameObject.Find("po"+id), false, false);
        }
    }

    public void setColor(GameObject objet, bool selected, bool passe)
    {
        if (passe)
        {
            objet.GetComponent<Renderer>().material = matHiglitedVertice;
        }
        else if (selected)
        {
            objet.GetComponent<Renderer>().material = matSelectedVertice;
        }
        else if (!selected && !passe)
        {
            if (les_po[objet.GetInstanceID()].Count > 1)
            {
                objet.GetComponent<Renderer>().material = matNonSharedVertice;
            }
            else
            {
                objet.GetComponent<Renderer>().material = matSharedVertice;
            }
        }
    }

    public void MoveVertice(GameObject moveObjet)
    {
        int objetID = moveObjet.GetInstanceID();
        Mesh p_mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] p_vertices = p_mesh.vertices;
        List<int> verticeMoved = new List<int>();

        for (int i = 0; i < les_po[objetID].Count; i++)
        {
            Vector3 depart = p_vertices[les_po[objetID][i]];
            p_vertices[les_po[objetID][i]] = transform.InverseTransformPoint(moveObjet.transform.position);
            Vector3 arrivée = p_vertices[les_po[objetID][i]];
            if (typeCalculNormales == TypeCalculNormales.CalculCiblé) p_mesh.normals[les_po[objetID][i]] = NormalAtVertex(les_po[objetID][i]);

            if (deformeNearVertice)
            {
                bool End = false;
                int actualVertice = les_po[objetID][i];
                int id = 0;
                int actualId = 0;
                VertexVoisinDic.Add(id, actualVertice);
                id++;

                while (!End)
                {
                    List<int> neighbors = GetNeighbors(actualVertice);

                    float intesite = 0.30f;

                    Vector3 dist = (arrivée - depart);

                    foreach (int neighbor in neighbors)
                    {
                        if (typeDeDeformationDesVerticesProches == TypeDeDeformationDesVerticesProches.ADAPTATIVE)
                        {
                            intesite = 1 - (Vector3.Distance(p_vertices[les_po[objetID][i]], p_vertices[neighbor]) / maxRangeForDeformNearVertice);
                        }
                        p_vertices[neighbor] += dist * intesite;
                        if (typeCalculNormales == TypeCalculNormales.CalculCiblé) p_mesh.normals[les_po[objetID][i]] = NormalAtVertex(neighbor);

                        int objectId = 0;

                        foreach (KeyValuePair<int, List<int>> le_po in les_po)
                        {
                            foreach (int vertice in le_po.Value)
                            {
                                if (vertice == neighbor) objectId = le_po.Key;
                            }
                        }

                        GameObject objetVertice = GameObject.Find("po" + objectId);
                        objetVertice.transform.position = transform.TransformPoint(p_vertices[neighbor]);
                        VertexVoisinDic.Add(id, neighbor);
                        id++;
                    }

                    actualId++;
                    if (VertexVoisinDic.ContainsKey(actualId))
                    {
                        actualVertice = VertexVoisinDic[actualId];
                        if(Vector3.Distance(p_vertices[actualVertice], p_vertices[les_po[objetID][i]]) <= maxRangeForDeformNearVertice)
                        {
                            End = true;
                        }
                    }
                    else
                    {
                        End = true;
                    }
                }
                VertexVoisinDic.Clear();
            }
        }

        p_mesh.vertices = p_vertices;
        if (typeCalculNormales == TypeCalculNormales.CalculAutomatique) p_mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = p_mesh;
    }

    private Vector3 NormalAtVertex(int indiceVertex)
    {
        Mesh p_mesh = GetComponent<MeshFilter>().mesh;
        int[] p_triangles = p_mesh.triangles;
        Vector3[] p_normales = p_mesh.normals;
        Vector3[] p_vertice = p_mesh.vertices;
        Vector3 normal, sommeNormal = Vector3.zero;
        int nb = 0;

        Vector3 vertex = p_vertice[indiceVertex];

        for (int i = 0; i < p_triangles.Length; i++)
        {
            if (vertex == p_vertice[p_triangles[i]])
            {
                nb++;
                sommeNormal += p_normales[p_triangles[i]];
            }
        }
        sommeNormal.Normalize();
        if(nb == 0) return Vector3.zero;
        normal = sommeNormal / nb;
        return normal;
    }

    private List<int> GetNeighbors(int indiceVertex)
    {
        Mesh p_mesh = GetComponent<MeshFilter>().mesh;
        int[] p_triangles = p_mesh.triangles;
        Vector3[] p_vertice = p_mesh.vertices;
        List<int> neighbors = new List<int>();

        bool found = false;

        for (int i = 0; i < p_triangles.Length / 3; i++)
        {
            found = false;
            for (int j = 0; j < 3; j++)
            {
                int cur = p_triangles[i * 3 + j];
                if (cur == indiceVertex) found = true;
            }
            if (found)
            {
                for (int j = 0; j < 3; j++)
                {
                    int cur = p_triangles[i * 3 + j];
                    if(neighbors.IndexOf(cur) == -1 && cur != indiceVertex && Vector3.Distance(p_vertice[cur], p_vertice[indiceVertex]) <= maxRangeForDeformNearVertice)
                    {
                        neighbors.Add(cur);
                    }
                }
            }
        }

        return neighbors;
    }
}