using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Threading;
using MyBox;


public class TerrainProcedural : MonoBehaviour
{
    public enum DrawMode { NoisMap, ColourMap, Mesh, FalloffMap };
    public enum MapMode { Manuel, NoiseMap, NoiseMapInfini };

    public MapMode mapMode;

    [Separator("Génération du Terrain")]
    [ConditionalField(nameof(mapMode), false, MapMode.Manuel)] public int Dimention = 500;
    [ConditionalField(nameof(mapMode), false, MapMode.Manuel)] public int Resolution = 8;
    private Vector3[] p_vertices;
    private int[] p_triangles;
    private Mesh p_mesh;

    Dictionary<int, int> VertexVoisinDic = new Dictionary<int, int>();

    [ConditionalField(nameof(mapMode), false, MapMode.Manuel)] public Disque disque;
    [ConditionalField(nameof(mapMode), false, MapMode.Manuel)] public LayerMask mask;

    [ConditionalField(nameof(mapMode), false, MapMode.Manuel)]
    [SearchableEnum]
    public KeyCode addForMakeHole = KeyCode.LeftControl;

    [ConditionalField(nameof(mapMode), false, MapMode.Manuel)] public float intesiterCouldown = 0.25f;

    bool canChangeIntesiter = true;

    [SerializeField]
    public const int mapChunkSize = 241;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap)] public DrawMode drawMode;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini), Range(0, 6)] public int editorPreviewLOD;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini)] public float noiseScale;

    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini), Range(0, 20)] public int octaves;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini), Range(0, 1)] public float persistance;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini)] public float lacunarity;

    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini)] public int seed;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini)] public Vector2 offset;

    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini), PositiveValueOnly] public float meshHeightMultiplier;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini)] public AnimationCurve meshHeightCurve;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMapInfini), Range(0,7)] public int chantDeVision = 2;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap, MapMode.NoiseMapInfini)] public CollectionWrapper<TerrainType> regions;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMapInfini, MapMode.NoiseMap)] public Noise.NormalizeMode normalizeMode;


    public static float maxViewDist;
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    [HideInInspector]
    public CollectionWrapper<LODInfo> detailsLevel;
    const float scale = 1f;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMapInfini)] public Transform viewer;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMapInfini)] public Material mapMaterial;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap)] public bool useFalloff;

    public static Vector2 viewerPosition;
    Vector2 oldViewerPosition;
    int chunkSize;
    int chunksVisibleInViewDst;

    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap)] public Renderer textureRenderer;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap)] public MeshFilter meshFilter;
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap)] public MeshRenderer meshRenderer;

    [Separator]
    [ConditionalField(nameof(mapMode), false, MapMode.NoiseMap)] public bool autoUpdate;
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLasUpdate = new List<TerrainChunk>();

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Awake()
    {
        falloffMap = FallofGenerator.GenerateFalloffMap(mapChunkSize);
        if (useFalloff)
        {
            useFalloff = false;
        }
    }

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        if (drawMode == DrawMode.NoisMap)
        {
            DrawTexture(TextureGenerator.TextureFromeHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }else if (drawMode == DrawMode.FalloffMap)
        {
            DrawTexture(TextureGenerator.TextureFromeHeightMap(FallofGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if(useFalloff)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Value.Length; i++)
                {
                    if (currentHeight >= regions.Value[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions.Value[i].colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);

    }

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    void Start()
    {
        detailsLevel.Value = new LODInfo[chantDeVision];

        for (int i = 0; i < detailsLevel.Value.Length; i++)
        {
            detailsLevel.Value[i].lod = i * 2;
            detailsLevel.Value[i].visibleDstThreshold = (i + 1) * 200;
        }

        if (mapMode == MapMode.Manuel)
            généréTerrin();
        else if (mapMode == MapMode.NoiseMapInfini)
        {
            maxViewDist = detailsLevel.Value[detailsLevel.Value.Length - 1].visibleDstThreshold;

            chunkSize = TerrainProcedural.mapChunkSize - 1;
            chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDist / chunkSize);

            UpdateVisibleChunks();
        }
    }

    private void Update()
    {
        if (mapMode == MapMode.Manuel)
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                disque.rayon++;
            }
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                disque.rayon--;
            }

            if (Input.GetKey(KeyCode.KeypadPlus) && canChangeIntesiter)
            {
                disque.intesiteDeMonter++;
                if (disque.intesiteDeMonter > 50)
                {
                    disque.intesiteDeMonter = 50;
                }
                canChangeIntesiter = false;
                StartCoroutine(CouldownIntesiter());
            }
            if (Input.GetKey(KeyCode.KeypadMinus) && canChangeIntesiter)
            {
                disque.intesiteDeMonter--;
                if (disque.intesiteDeMonter < 0)
                {
                    disque.intesiteDeMonter = 0;
                }
                canChangeIntesiter = false;
                StartCoroutine(CouldownIntesiter());
            }

            if (Input.GetMouseButton(0) && Input.GetKey(addForMakeHole))
            {
                modifyTerrain(false, false);
            }
            else if (Input.GetMouseButton(0) && !Input.GetKey(addForMakeHole))
            {
                modifyTerrain(true, false);
            }
            else if (Input.GetMouseButton(1))
            {
                modifyTerrain(false, true);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                int actuallMesh = 0;

                while (File.Exists("Assets/CustomTerrain/Custom Terrain " + actuallMesh + ".mesh"))
                {
                    actuallMesh++;
                }
                string title = "Assets/CustomTerrain/Custom Terrain " + actuallMesh + ".mesh";
                AssetDatabase.CreateAsset(p_mesh, title);
                Debug.Log("Enregistrement réussis de " + title);
            }
        }
        else if (mapMode == MapMode.NoiseMapInfini)
        {
            viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

            if((oldViewerPosition - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
            {
                oldViewerPosition = viewerPosition;
                UpdateVisibleChunks();
            }

            if (mapDataThreadInfoQueue.Count > 0)
            {
                for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
                {
                    MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }

            if (meshDataThreadInfoQueue.Count > 0)
            {
                for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
                {
                    MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                    threadInfo.callback(threadInfo.parameter);
                }
            }
        }
    }

    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLasUpdate.Count; i++)
        {
            terrainChunksVisibleLasUpdate[i].SetVisible(false);
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset < chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset < chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChnkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChnkCoord))
                {
                    terrainChunkDictionary[viewedChnkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChnkCoord, new TerrainChunk(viewedChnkCoord, chunkSize, detailsLevel.Value, transform, mapMaterial));
                }
            }
        }
    }

    int[] getNeibourg(int triangleId)
    {

        int[] neigbourg = { triangleId - Resolution, triangleId + 1, triangleId + Resolution, triangleId - 1 };

        return neigbourg;
    }

    void modifyTerrain(bool elevation, bool clear)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask))
        {
            Debug.DrawLine(ray.origin, hit.point);

            if (!clear)
            {
                if (elevation)
                    p_vertices[p_triangles[(hit.triangleIndex) * 3 + 0]] += new Vector3(0, disque.intesiteDeMonter * disque.courbeDeNiveau.Evaluate(1.0f), 0);
                else
                    p_vertices[p_triangles[(hit.triangleIndex) * 3 + 0]] -= new Vector3(0, disque.intesiteDeMonter * disque.courbeDeNiveau.Evaluate(1.0f), 0);
            }
            else
            {
                p_vertices[p_triangles[(hit.triangleIndex) * 3 + 0]] = new Vector3(p_vertices[p_triangles[(hit.triangleIndex) * 3 + 0]].x, 0, p_vertices[p_triangles[(hit.triangleIndex) * 3 + 0]].z);
            }
            int startPoint = p_triangles[(hit.triangleIndex) * 3 + 0];
            int actualpoint = startPoint;

            bool isEnd = false;
            int id = 0;
            int actualId = 0;
            VertexVoisinDic.Add(id, actualpoint);
            id++;

            float courbeActuelle = 0.9f;

            while (!isEnd)
            {
                int[] neigbourg = getNeibourg(actualpoint);

                foreach (int voisin in neigbourg)
                {
                    if (voisin < Resolution * Resolution && voisin > 0 && !VertexVoisinDic.ContainsValue(voisin) && Vector3.Distance(p_vertices[voisin], p_vertices[startPoint]) < disque.rayon)
                    {
                        float courbe = 1 - (Vector3.Distance(p_vertices[voisin], p_vertices[startPoint]) / disque.rayon);
                        if (!clear)
                        {
                            if (elevation)
                                p_vertices[voisin] += new Vector3(0, disque.intesiteDeMonter * disque.courbeDeNiveau.Evaluate(courbe), 0);
                            else
                                p_vertices[voisin] -= new Vector3(0, disque.intesiteDeMonter * disque.courbeDeNiveau.Evaluate(courbe), 0);
                        }
                        else
                        {
                            p_vertices[voisin] = new Vector3(p_vertices[voisin].x, 0, p_vertices[voisin].z);
                        }
                        VertexVoisinDic.Add(id, voisin);
                        id++;
                    }
                }

                actualId++;
                if (VertexVoisinDic.ContainsKey(actualId))
                {
                    //print(actualId);
                    actualpoint = VertexVoisinDic[actualId];
                    courbeActuelle /= disque.rayon;
                    if (courbeActuelle < 0)
                    {
                        courbeActuelle = 0;
                    }
                    if (Vector3.Distance(p_vertices[actualpoint], p_vertices[startPoint]) > disque.rayon)
                    {
                        isEnd = true;
                    }
                }
                else
                {
                    isEnd = true;
                }
            }

            p_mesh.vertices = p_vertices;
            p_mesh.RecalculateNormals();
            GetComponent<MeshFilter>().mesh = p_mesh;
            GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().mesh;

            VertexVoisinDic.Clear();
        }
    }

    IEnumerator CouldownIntesiter()
    {
        yield return new WaitForSeconds(intesiterCouldown);
        canChangeIntesiter = true;
    }

    public void généréTerrin()
    {
        p_mesh = new Mesh();
        p_mesh.Clear();
        p_mesh.name = "Terrain";
        p_vertices = new Vector3[(Resolution * Resolution)];
        float taille = Dimention / Resolution;
        float gapX = 0;
        float gapY = 0;
        p_triangles = new int[(Resolution * Resolution) * 6];
        int actualPoint = 0;


        for (int x = 0; x < Resolution; x++)
        {
            for (int y = 0; y < Resolution; y++)
            {
                p_vertices[actualPoint] = new Vector3(gapX, 0, gapY);
                actualPoint++;
                gapY += taille;
            }
            gapY = 0;
            gapX += taille;
        }

        actualPoint = 0;
        for (int x = 0; x < Resolution - 1; x++)
        {
            for (int y = 0; y < Resolution - 1; y++)
            {
                int[] carrée = new int[]
                {
                x * Resolution + y, (x * Resolution + y) + 1, ((x + 1) * Resolution + y) + 1,
                x * Resolution + y, ((x + 1) * Resolution + y) + 1, (x + 1) * Resolution + y
                };

                foreach (int item in carrée)
                {
                    p_triangles[actualPoint] = item;
                    actualPoint++;
                }
            }
        }

        p_mesh.vertices = p_vertices;
        p_mesh.triangles = p_triangles;
        p_mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = p_mesh;
        GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().mesh;
    }

    private void OnValidate()
    {
        if (Dimention < 2)
        {
            Dimention = 2;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        falloffMap = FallofGenerator.GenerateFalloffMap(mapChunkSize);
    }

    struct MapThreadInfo<T>
    {
        public Action<T> callback;
        public T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        TerrainProcedural mapGenerator = GameObject.FindObjectOfType<TerrainProcedural>();

        LODInfo[] detailsLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailsLevels, Transform parent, Material material)
        {
            this.detailsLevels = detailsLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;
            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detailsLevels.Length];
            for (int i = 0; i < detailsLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailsLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, TerrainProcedural.mapChunkSize, TerrainProcedural.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDist;

                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailsLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailsLevels[i].visibleDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksVisibleLasUpdate.Add(this);
                }
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    public class LODMesh{

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        TerrainProcedural mapGenerator = GameObject.FindObjectOfType<TerrainProcedural>();

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold;
    }
}


[System.Serializable]
public struct Disque
{
    public float rayon;
    public AnimationCurve courbeDeNiveau;
    public int intesiteDeMonter;
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}