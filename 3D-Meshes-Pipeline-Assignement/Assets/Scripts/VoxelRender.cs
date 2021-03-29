using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelRender : MonoBehaviour
{
    Mesh mesh;
    GameObject[,,] chunks;

    public ComputeShader generatorShader;
    public Texture perlinNoise;

    public float blockScale = 1f;

    public GameObject chunkPrefab;
    GameObject tempChunk;
    public int chunkSize;

    public int chunksAxisX;
    public int chunksAxisY;
    public int chunksAxisZ;
    Vector3 chunksPerAxis;
    Vector3 lastChunksPerAxis;

    //Surface generation settings
    [Range(1, 3)]
    public float noiseX;
    [Range(1, 3)]
    public float noiseZ;
    Vector2 noiseScale;

    [Range(0, 2)]
    public float repeat;
    [Range(0,1)]
    public float surfaceHeight;
    public int surfaceRelief;
    float[] surfaceSettings;

    //Cave generation settings
    public int caveItirations;
    [Range(0, 26)]
    public int caveBirth;
    [Range(0, 26)]
    public int caveDeath;
    [Range(0, 100)]
    public int initialCave;
    public int cavesSeed;
    int[] caveSettings;

    Stopwatch stopwatch = new Stopwatch(); 

    public bool update;

    VoxelData data;

    void Awake()
    {
        mesh = GetComponent<MeshFilter>().mesh;
    }

    private void Start()
    {
        chunks = new GameObject[chunksAxisX, chunksAxisY, chunksAxisZ];
        data = new VoxelData();
        lastChunksPerAxis = Vector3.zero;
        data.perlinNoise = perlinNoise;
        data.generator = generatorShader;


        //Compacting variables
        chunksPerAxis = new Vector3(chunksAxisX, chunksAxisY, chunksAxisZ);
        noiseScale = new Vector3(noiseX, noiseZ);
        surfaceSettings = new float[] { repeat, surfaceHeight, surfaceRelief };
        caveSettings = new int[] { caveItirations, caveBirth, caveDeath, initialCave, cavesSeed};

        stopwatch.Reset();
        stopwatch.Start();
        data.GenerateData(chunkSize, chunksPerAxis, noiseScale, surfaceSettings, caveSettings);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Data generation took:" + stopwatch.Elapsed);

        GenerateVoxelChunks(data);
    }



    // Start is called before the first frame update
    void Update()
    {
        if (update)
        {
            update = false;
            //Compacting variables
            chunksPerAxis = new Vector3(chunksAxisX, chunksAxisY, chunksAxisZ);
            noiseScale = new Vector3(noiseX, noiseZ);
            surfaceSettings = new float[] { repeat, surfaceHeight, surfaceRelief };
            caveSettings = new int[] { caveItirations, caveBirth, caveDeath, initialCave, cavesSeed};

            data.GenerateData(chunkSize, chunksPerAxis, noiseScale, surfaceSettings, caveSettings);
            if (chunksPerAxis == lastChunksPerAxis)
            {
                UpdateVoxelChunks(data);
            }
            else
            {
                GenerateVoxelChunks(data);
            }
        }
    }

    //This function generates the vertices and triangles based on the given data
    void GenerateVoxelChunks(VoxelData data)
    {
        for (int x = 0; x < chunks.GetLength(0); x++)
        {
            for (int y = 0; y < chunks.GetLength(1); y++)
            {
                for (int z = 0; z < chunks.GetLength(2); z++)
                {
                    Destroy(chunks[x, y, z]);
                }
            }
        }
        chunks = new GameObject[chunksAxisX, chunksAxisY, chunksAxisZ];

        //Loop trough the x y and z coordinates
        for (int z = 0; z < chunksAxisZ; z++)
        {
            for (int y = 0; y < chunksAxisY; y++)
            {
                for (int x = 0; x < chunksAxisX; x++)
                {
                    tempChunk = Instantiate(chunkPrefab);
                    if (data.EmptyChunk(chunkSize, new Vector3(x, y, z)))
                    {
                        tempChunk.SetActive(false);
                    }
                    else
                    {
                        tempChunk.GetComponent<ChunkRender>().UpdateChunkMesh(data, new Vector3(x, y, z), chunkSize, blockScale);
                    }
                    tempChunk.transform.parent = transform;
                    chunks[x,y,z] = tempChunk;
                }
            }
        }
    }

    void UpdateVoxelChunks(VoxelData data)
    {
        //Loop trough the x y and z coordinates
        //Loop trough the x y and z coordinates
        for (int cz = 0; cz < chunksAxisX; cz++)
        {
            for (int cy = 0; cy < chunksAxisY; cy++)
            {
                for (int cx = 0; cx < chunksAxisZ; cx++)
                {
                    if (data.EmptyChunk(chunkSize, new Vector3(cx, cy, cz)))
                    {
                        chunks[cx, cy, cz].SetActive(false);
                    }
                    else
                    {
                        chunks[cx, cy, cz].SetActive(true);
                        chunks[cx, cy, cz].GetComponent<ChunkRender>().UpdateChunkMesh(data, new Vector3(cx, cy, cz), chunkSize, blockScale);
                    }
                }
            }
        }
    }
}
