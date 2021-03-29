using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class VoxelData
{
    int dist;
    int[,,,,,] data;
    int[,,] caves;
    int[,,] previousCaves;
    int[] startEnd;

    public ComputeShader generator;
    ComputeBuffer surfaceArray;
    ComputeBuffer caveArray;
    ComputeBuffer oldCaveArray;
    public Texture perlinNoise;

    Cube[] cubes;
    int threadsX;
    int threadsY;
    int threadsZ;

    Stopwatch sw;

    struct Cube
    {
        public int blockType;
    }
    

    //Generate the data that will be used for the mesh
    public void GenerateData(int chunkSize, Vector3 chunksPerAxis, Vector2 noiseScale, float[] surfaceSettings, int[] caveSettings)
    {
        sw = new Stopwatch();

        //Create the data array
        data = new int[(int)chunksPerAxis.x, (int)chunksPerAxis.y, (int)chunksPerAxis.z, chunkSize, chunkSize, chunkSize];

        //Send the perlin noise to the compute shader
        generator.SetTexture(0, "perlinNoise", perlinNoise);

        //Create the cube buffers for the compute shader
        sw.Reset();
        sw.Start();
        CreateCubeBuffers((int)chunksPerAxis.x * chunkSize, (int)chunksPerAxis.y * chunkSize, (int)chunksPerAxis.z * chunkSize, caveSettings[3], surfaceSettings[1], surfaceSettings[2]);
        sw.Stop();
        UnityEngine.Debug.Log("Creating buffers took: " + sw.Elapsed);

        //Calculate the threads
        threadsX = Mathf.CeilToInt((int)chunksPerAxis.x * chunkSize * 0.25f);
        threadsY = Mathf.CeilToInt((int)chunksPerAxis.y * chunkSize * 0.25f);
        threadsZ = Mathf.CeilToInt((int)chunksPerAxis.z * chunkSize * 0.25f);


        /*
        Vector2 invSize = Vector3.zero;
        invSize.x = surfaceSettings[0] / (float)chunkSize * chunksPerAxis.x;
        invSize.y = surfaceSettings[0] / (float)chunkSize * chunksPerAxis.z;
        
        //Generate the cavemap
        sw.Reset();
        sw.Start();
        caves = GenerateCavesMap(chunkSize, chunksPerAxis, caveSettings[0], caveSettings[1], caveSettings[2],
            caveSettings[3], caveSettings[4], surfaceSettings[1], surfaceSettings[2]);
        sw.Stop();
        UnityEngine.Debug.Log("Generating caves took: " + sw.Elapsed);
        */


        /*
        //Find a startpoint and endpoint int the caves for pathfinding
        sw.Reset();
        sw.Start();
        startEnd = RandomStartEnd(caves);
        sw.Stop();
        UnityEngine.Debug.Log("Finding start end took: " + sw.Elapsed);

        //Try to calculate a path through the caves from start to end
        sw.Reset();
        sw.Start();
        if (startEnd != null)
        {
            UnityEngine.Debug.Log("Start point: " + startEnd[0] + ", " + startEnd[1] + ", " + startEnd[2]);
            UnityEngine.Debug.Log("End point: " + startEnd[3] + ", " + startEnd[4] + ", " + startEnd[5]);
            caves = CavePath(startEnd, caves);
        }
        sw.Stop();
        UnityEngine.Debug.Log("Calculating path took: " + sw.Elapsed);
        */


        //Generate a surface map
        sw.Reset();
        sw.Start();
        ComputeSurface(surfaceSettings[1], (int)surfaceSettings[2]);
        sw.Stop();
        UnityEngine.Debug.Log("Generate surface noise took: " + sw.Elapsed);

        //Add grass to the surface
        sw.Reset();
        sw.Start();
        ComputeGrass();
        sw.Stop();
        UnityEngine.Debug.Log("Adding grass took: " + sw.Elapsed);

        //Create the initial caves state
        sw.Reset();
        sw.Start();
        InitialCaves(surfaceSettings[1], (int)surfaceSettings[2], caveSettings[3]);
        sw.Stop();
        UnityEngine.Debug.Log("Initial caves took: " + sw.Elapsed);

        //Compute the cavemap
        sw.Reset();
        sw.Start();
        ComputeCaves(caveSettings[1], caveSettings[2], caveSettings[0]);
        sw.Stop();
        UnityEngine.Debug.Log("Computing caves took: " + sw.Elapsed);

        //Combine surface and caves arrays
        sw.Reset();
        sw.Start();
        CompAddCaves();
        sw.Stop();
        UnityEngine.Debug.Log("Combining arrays took: " + sw.Elapsed);

        //Transform the data from the data buffer to teh data format
        sw.Reset();
        sw.Start();
        BuffferToData(chunksPerAxis, chunkSize);
        sw.Stop();
        UnityEngine.Debug.Log("Reading buffer data took: " + sw.Elapsed);


        /*
        //Add the caves to the chunks
        for (int cx = 0; cx < chunksPerAxis.x; cx++)
        {
            for (int cy = (int)chunksPerAxis.y - 1; cy >= 0; cy--)
            {
                for (int cz = 0; cz < chunksPerAxis.z; cz++)
                {
                    AddCaves(chunkSize, new Vector3(cx, cy, cz));
                }
            }
        }
        */

        //Release all compute buffers
        surfaceArray.Release();
        caveArray.Release();
        oldCaveArray.Release();
    }

    //Create a cube arrays used in the compute shader (int x, y, z determine size, int initialChanceInt determines starting cave map)
    void CreateCubeBuffers(int x, int y, int z, int initialChanceInt, float surfaceHeight, float surfaceRelief)
    {
        //Create a cube buffer
        surfaceArray = new ComputeBuffer(x * y * z, 4);
        //Create an array of cubes to represent the grid
        cubes = new Cube[x * y * z];
        //Create a cube struct for every cube in the scene
        for (int i = 0; i < cubes.Length; i++)
        {
            Cube tempCube = new Cube();
            tempCube.blockType = 0;
            cubes[i] = tempCube;
        }
        //Set the array to the compute buffer
        surfaceArray.SetData(cubes);


        /*
        //Initial chance is an int between 0 and 100, convert this to a float between 0 and 1
        float initialChance = initialChanceInt / 100f;
        float inverseChance = 1 - initialChance;
        //Loop through all cubes keeping track of coördinates
        for (int x2 = 0; x2 < x; x2++)
        {
            for (int y2 = 0; y2 < y; y2++)
            {
                for (int z2 = 0; z2 < z; z2++)
                {
                    //Minimize caves on the surface and fill the caves array with stone and air based on initial chance
                    if ((y2 > y * surfaceHeight - surfaceRelief && Random.value < Mathf.Pow(inverseChance, 1.5f)) || Random.value < initialChance)
                    {
                        //Chance into stone
                        cubes[x2 + y2 * x + z2 * x * y].blockType = 1;
                    }
                }
            }
        }
        */
        //Create the cave compute buffers
        caveArray = new ComputeBuffer(x * y * z, 4);
        oldCaveArray = new ComputeBuffer(x * y * z, 4);

        //Set the calculated starting state of the caves to the cave array
        caveArray.SetData(cubes);

        //Send the variables to the compute shader
        generator.SetInt("xSize", x);
        generator.SetInt("ySize", y);
        generator.SetInt("zSize", z);
    }

    //Generate a surface cube map via a compute shader (float surfaceHeight high is the surface from the total y height, int surface relief how many cubes does the surface go up and down)
    void ComputeSurface(float surfaceHeight, float surfaceRelief)
    {
        //Send the surface height and relief to the compute shader
        generator.SetFloat("surfaceHeight", surfaceHeight);
        generator.SetFloat("surfaceRelief", surfaceRelief);

        //Send the cube array to the compute shader
        generator.SetBuffer(0, "cubes", surfaceArray);

        //Run the surface generator
        generator.Dispatch(0, threadsX, threadsY, threadsZ);
    }


    void InitialCaves(float surfaceHeight, float surfaceRelief, int initialChanceInt)
    {
        //Initial chance is an int between 0 and 100, convert this to a float between 0 and 1
        float initialChance = initialChanceInt / 100f;
        float inverseChance = 1 - initialChance;
        inverseChance = Mathf.Pow(inverseChance, 1.5f);

        generator.SetFloat("surfaceHeight", surfaceHeight);
        generator.SetFloat("surfaceRelief", surfaceRelief);
        generator.SetFloat("initialSolid", initialChance);
        generator.SetFloat("surfaceSolid", inverseChance);
        generator.SetInt("seed", 367);

        generator.SetBuffer(4, "caves2", caveArray);

        generator.Dispatch(4, threadsX, threadsY, threadsZ);
    }
    
    //Generate caves via a compute shader
    void ComputeCaves(int birthCount, int deathCount, int iterations)
    {
        //Send the birthcount and deathcount to the compute shader
        generator.SetInt("birthCount", birthCount);
        generator.SetInt("deathCount", deathCount);

        //Run for the required amount of iterations
        for (int i = 0; i < iterations; i++)
        {
            //Read the current cave array
            caveArray.GetData(cubes);
            //Set the current cave array as the old caves array
            oldCaveArray.SetData(cubes);

            //Send the old and new caves arrays to the compute shader
            generator.SetBuffer(1, "cubes2", caveArray);
            generator.SetBuffer(1, "cubesOld", oldCaveArray);

            //Run an itteration of the cellular Automata
            generator.Dispatch(1, threadsX, threadsY, threadsZ);
        }
    }

    //Add the caves to the surface array
    void CompAddCaves()
    {
        //Send the caves and surface arrays to the compute shader
        generator.SetBuffer(2, "surface", surfaceArray);
        generator.SetBuffer(2, "caves", caveArray);

        //Run the surface and caves 
        generator.Dispatch(2, threadsX, threadsY, threadsZ);
    }

    //Add grass to the surface array
    void ComputeGrass()
    {
        generator.SetInt("seed", 99);

        generator.SetBuffer(3, "surface2", surfaceArray);
        for (int i = 0; i < 5; i++)
        {
            generator.Dispatch(3, threadsX, threadsY, threadsZ);
        }
    }

    //Transform the buffer data to the data stucture used for the rest of the program
    void BuffferToData(Vector3 chunksPerAxis, int chunkSize)
    {
        //Get the data from the surface array
        surfaceArray.GetData(cubes);
        //Loop through all chunks
        for (int cx = 0; cx < (int)chunksPerAxis.x; cx++)
        {
            for (int cy = 0; cy < (int)chunksPerAxis.y; cy++)
            {
                for (int cz = 0; cz < (int)chunksPerAxis.z; cz++)
                {
                    //Loop through all cubes within chunks
                    for (int x = 0; x < chunkSize; x++)
                    {
                        for (int y = 0; y < chunkSize; y++)
                        {
                            for (int z = 0; z < chunkSize; z++)
                            {
                                //Transform the linear buffer array to the 6 dimensional data array
                                data[cx, cy, cz, x, y, z] = cubes[x + cx*chunkSize 
                                    + (y + cy*chunkSize) * (int)chunksPerAxis.x*chunkSize 
                                    + (z + cz*chunkSize) * (int)chunksPerAxis.x*(int)chunksPerAxis.y*chunkSize*chunkSize
                                    ].blockType;
                            }
                        }
                    }
                }
            }
        }
    }







    void GenerateChunk(int chunkSize, Vector3 chunkPos, Vector2 noiseScale, Vector2 invSize, float surfaceHeight, float surfaceRelief)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (y + chunkPos.y * chunkSize
                        <
                        surfaceHeight * data.GetLength(1) * chunkSize - surfaceRelief +
                        Mathf.PerlinNoise((x + chunkPos.x * chunkSize) * noiseScale.x * invSize.x,
                        (z + chunkPos.z * chunkSize) * noiseScale.y * invSize.y) * surfaceRelief)
                    {
                        data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 1;
                    }
                    else
                    {
                        data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 0;
                    }
                }
            }
        }
        AssignGrass(chunkSize, chunkPos);
    }

    //Add caves to chunk
    public void AddCaves(int chunkSize, Vector3 chunkPos)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                { 
                    //1 is rock, 0, 2 and 3 are air, 4 is the path
                    switch(caves[x + (int)chunkPos.x * chunkSize, y + (int)chunkPos.y * chunkSize, z + (int)chunkPos.z * chunkSize])
                    {
                        case 0:
                            data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 0;
                            break;
                        case 2:
                            data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 0;
                            break;
                        case 3:
                            data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 0;
                            break;
                        case 4:
                            data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 9;
                            break;
                    }
                }
            }
        }
    }

    //This function checks if a chunk is empty based on chunk size and position
    public bool EmptyChunk(int chunkSize, Vector3 chunkPos)
    {
        //Loop though all blocks within the chunk
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    //Debug.Log(chunkPos);
                    //If any of the blocks isn't empty return false
                    if (data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] != 0)
                    {
                        return false;
                    }
                }
            }
        }
        //If all blocks are empty return true
        return true;
    }

    public void AssignGrass(int chunkSize, Vector3 chunkPos)
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = chunkSize - 1; y >= 0; y--)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] == 1)
                    {
                        switch (GetNeighbor(chunkSize, chunkPos, x, y, z, Direction.Up))
                        {
                            case 0:
                                data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 2;
                                break;
                            case 2:
                                data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 3;
                                break;
                            case 3:
                                dist = 1 + Mathf.FloorToInt(3.0f * Random.value);
                                int test = GetNeighbor(chunkSize, chunkPos, x, y, z, Direction.Up, dist);
                                switch (test)
                                {
                                    case 0:
                                        data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 3;
                                        break;
                                    case 1:
                                        data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 3;
                                        break;
                                    case 2:
                                        data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 3;
                                        break;
                                    case 3:
                                        data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z] = 1;
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

    //This function generates a cavesmap with the same dimentions as the block map
    int[,,] GenerateCavesMap(int chunkSize, Vector3 chunksPerAxis, int iterations, int birthLimit,
        int deathLimit, float initialChanceInt, int seed, float surfaceHeight, float surfaceRelief)
    {
        Random.InitState(seed);
        caves = new int[(int)chunksPerAxis.x * chunkSize, (int)chunksPerAxis.y * chunkSize, (int)chunksPerAxis.z * chunkSize];
        float initialChance = initialChanceInt / 100f;

        //Generate a random grid
        for (int x = 0; x < caves.GetLength(0); x++)
        {
            for (int y = 0; y < caves.GetLength(1); y++)
            {
                for (int z = 0; z < caves.GetLength(2); z++)
                {
                    if (y > caves.GetLength(1) * surfaceHeight - surfaceRelief 
                        || Random.value < initialChance)
                    {
                        caves[x, y, z] = 1;
                    }
                    else
                    {
                        caves[x, y, z] = 0;
                    }
                }
            }
        }
        //Play the game of life with the cave map iteration times
        while (iterations > 0)
        {
            iterations--;
            //Keep track of the old map as reference
            previousCaves = new int[caves.GetLength(0), caves.GetLength(1), caves.GetLength(2)];

            for (int x = 0; x < caves.GetLength(0); x++)
            {
                for (int y = 0; y < caves.GetLength(1); y++)
                {
                    for (int z = 0; z < caves.GetLength(2); z++)
                    {
                        previousCaves[x, y, z] = caves[x, y, z];
                    }
                }
            }

            //Loop through all cells on the map
            for (int x = 0; x < caves.GetLength(0); x++)
            {
                for (int y = 0; y < caves.GetLength(1); y++)
                {
                    for (int z = 0; z < caves.GetLength(2); z++)
                    {
                        //Set the cell based on surrounding cells and the rules
                        caves[x, y, z] = CheckSurounding(previousCaves, x, y, z, birthLimit, deathLimit);
                    }
                }
            }
        }
        //Return cavesMap
        return caves;
    }

    //This fuction creates a path trough the caves map from startPos to targetPos
    int[,,] CavePath(int[] startEnd, int[,,] cavesMap)
    {
        //Keep a list of origin points for every coordinate
        int[,,,] originPoints = new int[cavesMap.GetLength(0), cavesMap.GetLength(1), cavesMap.GetLength(2), 3];
        
        //Keep a list of active points
        List<int[]> activePointsStart = new List<int[]>();
        //Keep a list of the next active points
        List<int[]> activePointsStart2 = new List<int[]>();
       
        //Keep a list of active points
        List<int[]> activePointsEnd = new List<int[]>();
        //Keep a list of the next active points
        List<int[]> activePointsEnd2 = new List<int[]>();

        //Keep a list of the finished path
        List<int[]> path = new List<int[]>();
        int[] currentPoint = new int[3];

        //have a boolian for the while loop
        bool done = false;

        //Mark the start search as 2
        cavesMap[startEnd[0], startEnd[1], startEnd[2]] = 2;
        //Mark the end search as 3
        cavesMap[startEnd[3], startEnd[4], startEnd[5]] = 3;

        //Add the startPoint as the first active starting point
        activePointsStart.Add(new int[]{ startEnd[0], startEnd[1], startEnd[2]});
        //Add the endPoint as the first active ending point
        activePointsEnd.Add(new int[]{ startEnd[3], startEnd[4], startEnd[5]});

        int[] meeting = new int[] { 0, 0, 0, 0, 0, 0 };

        //Loop until dead end or path is found
        while (!done)
        {
            //If either side has no more active points there is no path possible
            if(activePointsStart.Count == 0 || activePointsEnd.Count == 0)
            {
                UnityEngine.Debug.Log("Test1");
                done = true;
                //Meeting null means no path found
                meeting = null;
            }

            //Path finding
            if (!done)
            {
                //x+
                //Start
                {
                    //For all points in de startpoints list
                    for (int i = 0; i < activePointsStart.Count; i++)
                    {
                        //Don't go x+ beyond the array size
                        if (!(activePointsStart[i][0] == cavesMap.GetLength(0) - 1))
                        {
                            //Check the cavesmap if x+ is air
                            if (cavesMap[activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2]] == 0)
                            {
                                //This coördinate is now part of the start search
                                cavesMap[activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2]] = 2;
                                //Add this point to the list for the next iteration
                                activePointsStart2.Add(new int[] { activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2] });

                                //Save the origin point to recreate the path later
                                originPoints[activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2], 0] = activePointsStart[i][0];
                                originPoints[activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2], 1] = activePointsStart[i][1];
                                originPoints[activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2], 2] = activePointsStart[i][2];
                            }
                            //Check the cavesmap is x+ is the end search
                            if (cavesMap[activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2]] == 3)
                            {
                                //We have found a path
                                done = true;
                                //Save the coördinates of the meeting
                                meeting = new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2],
                                activePointsStart[i][0] + 1, activePointsStart[i][1], activePointsStart[i][2] };
                            }
                        }
                    }
                }
                //End
                {
                    for (int i = 0; i < activePointsEnd.Count; i++)
                    {
                        if (!(activePointsEnd[i][0] == cavesMap.GetLength(0) - 1))
                        {
                            if (cavesMap[activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2]] == 0)
                            {
                                cavesMap[activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2]] = 3;
                                activePointsEnd2.Add(new int[] { activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2] });

                                originPoints[activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2], 0] = activePointsEnd[i][0];
                                originPoints[activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2], 1] = activePointsEnd[i][1];
                                originPoints[activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2], 2] = activePointsEnd[i][2];
                            }
                            if (cavesMap[activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2]] == 2)
                            {
                                done = true;
                                meeting = new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2],
                                activePointsEnd[i][0] + 1, activePointsEnd[i][1], activePointsEnd[i][2] };
                            }
                        }
                    }
                }
                //y+
                //Start
                {
                    for (int i = 0; i < activePointsStart.Count; i++)
                    {
                        if (!(activePointsStart[i][1] == cavesMap.GetLength(1) - 1))
                        {
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2]] == 0)
                            {
                                cavesMap[activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2]] = 2;
                                activePointsStart2.Add(new int[] { activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2] });

                                originPoints[activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2], 0] = activePointsStart[i][0];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2], 1] = activePointsStart[i][1];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2], 2] = activePointsStart[i][2];
                            }
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2]] == 3)
                            {
                                done = true;
                                meeting = new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2],
                                activePointsStart[i][0], activePointsStart[i][1] + 1, activePointsStart[i][2] };
                            }
                        }
                    }
                }
                //End
                {
                    for (int i = 0; i < activePointsEnd.Count; i++)
                    {
                        if (!(activePointsEnd[i][1] == cavesMap.GetLength(1) - 1))
                        {
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2]] == 0)
                            {
                                cavesMap[activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2]] = 3;
                                activePointsEnd2.Add(new int[] { activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2] });

                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2], 0] = activePointsEnd[i][0];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2], 1] = activePointsEnd[i][1];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2], 2] = activePointsEnd[i][2];
                            }
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2]] == 2)
                            {
                                done = true;
                                meeting = new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2],
                                activePointsEnd[i][0], activePointsEnd[i][1] + 1, activePointsEnd[i][2] };
                            }
                        }
                    }
                }
                //z+
                //Start
                {
                    for (int i = 0; i < activePointsStart.Count; i++)
                    {
                        if (!(activePointsStart[i][2] == cavesMap.GetLength(2) - 1))
                        {
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1] == 0)
                            {
                                cavesMap[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1] = 2;
                                activePointsStart2.Add(new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1 });

                                originPoints[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1, 0] = activePointsStart[i][0];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1, 1] = activePointsStart[i][1];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1, 2] = activePointsStart[i][2];
                            }
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1] == 3)
                            {
                                done = true;
                                meeting = new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2],
                                activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] + 1};
                            }
                        }
                    }
                }
                //End
                {
                    for (int i = 0; i < activePointsEnd.Count; i++)
                    {
                        if (!(activePointsEnd[i][2] == cavesMap.GetLength(2) - 1))
                        {
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1] == 0)
                            {
                                cavesMap[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1] = 3;
                                activePointsEnd2.Add(new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1 });

                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1, 0] = activePointsEnd[i][0];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1, 1] = activePointsEnd[i][1];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1, 2] = activePointsEnd[i][2];
                            }
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1] == 2)
                            {
                                done = true;
                                meeting = new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2],
                                activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] + 1};
                            }
                        }
                    }
                }

                //x-
                //Start
                {
                    for (int i = 0; i < activePointsStart.Count; i++)
                    {
                        if (!(activePointsStart[i][0] == 0))
                        {
                            if (cavesMap[activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2]] == 0)
                            {
                                cavesMap[activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2]] = 2;
                                activePointsStart2.Add(new int[] { activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2] });

                                originPoints[activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2], 0] = activePointsStart[i][0];
                                originPoints[activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2], 1] = activePointsStart[i][1];
                                originPoints[activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2], 2] = activePointsStart[i][2];
                            }
                            if (cavesMap[activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2]] == 3)
                            {
                                done = true;
                                meeting = new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2],
                                activePointsStart[i][0] - 1, activePointsStart[i][1], activePointsStart[i][2] };
                            }
                        }
                    }
                }
                //End
                {
                    for (int i = 0; i < activePointsEnd.Count; i++)
                    {
                        if (!(activePointsEnd[i][0] == 0))
                        {
                            if (cavesMap[activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2]] == 0)
                            {
                                cavesMap[activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2]] = 3;
                                activePointsEnd2.Add(new int[] { activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2] });

                                originPoints[activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2], 0] = activePointsEnd[i][0];
                                originPoints[activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2], 1] = activePointsEnd[i][1];
                                originPoints[activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2], 2] = activePointsEnd[i][2];
                            }
                            if (cavesMap[activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2]] == 2)
                            {
                                done = true;
                                meeting = new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2],
                                activePointsEnd[i][0] - 1, activePointsEnd[i][1], activePointsEnd[i][2] };
                            }
                        }
                    }
                }
                //y-
                //Start
                {
                    for (int i = 0; i < activePointsStart.Count; i++)
                    {
                        if (!(activePointsStart[i][1] == 0))
                        {
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2]] == 0)
                            {
                                cavesMap[activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2]] = 2;
                                activePointsStart2.Add(new int[] { activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2] });

                                originPoints[activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2], 0] = activePointsStart[i][0];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2], 1] = activePointsStart[i][1];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2], 2] = activePointsStart[i][2];
                            }
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2]] == 3)
                            {
                                done = true;
                                meeting = new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2],
                                activePointsStart[i][0], activePointsStart[i][1] - 1, activePointsStart[i][2] };
                            }
                        }
                    }
                }
                //End
                {
                    for (int i = 0; i < activePointsEnd.Count; i++)
                    {
                        if (!(activePointsEnd[i][1] == 0))
                        {
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2]] == 0)
                            {
                                cavesMap[activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2]] = 3;
                                activePointsEnd2.Add(new int[] { activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2] });

                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2], 0] = activePointsEnd[i][0];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2], 1] = activePointsEnd[i][1];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2], 2] = activePointsEnd[i][2];
                            }
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2]] == 2)
                            {
                                done = true;
                                meeting = new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2],
                                activePointsEnd[i][0], activePointsEnd[i][1] - 1, activePointsEnd[i][2] };
                            }
                        }
                    }
                }
                //z-
                //Start
                {
                    for (int i = 0; i < activePointsStart.Count; i++)
                    {
                        if (!(activePointsStart[i][2] == 0))
                        {
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1] == 0)
                            {
                                cavesMap[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1] = 2;
                                activePointsStart2.Add(new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1 });

                                originPoints[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1, 0] = activePointsStart[i][0];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1, 1] = activePointsStart[i][1];
                                originPoints[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1, 2] = activePointsStart[i][2];
                            }
                            if (cavesMap[activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1] == 3)
                            {
                                done = true;
                                meeting = new int[] { activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2],
                                activePointsStart[i][0], activePointsStart[i][1], activePointsStart[i][2] - 1};
                            }
                        }
                    }
                }
                //End
                {
                    for (int i = 0; i < activePointsEnd.Count; i++)
                    {
                        if (!(activePointsEnd[i][2] == 0))
                        {
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1] == 0)
                            {
                                cavesMap[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1] = 3;
                                activePointsEnd2.Add(new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1 });

                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1, 0] = activePointsEnd[i][0];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1, 1] = activePointsEnd[i][1];
                                originPoints[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1, 2] = activePointsEnd[i][2];
                            }
                            if (cavesMap[activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1] == 2)
                            {
                                done = true;
                                meeting = new int[] { activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2],
                                activePointsEnd[i][0], activePointsEnd[i][1], activePointsEnd[i][2] - 1};
                            }
                        }
                    }
                }

                //The list of new active points is used for the next loop
                activePointsStart = activePointsStart2;
                activePointsStart2 = new List<int[]>();

                activePointsEnd = activePointsEnd2;
                activePointsEnd2 = new List<int[]>();
            }
        }
        //If no meeting point is found skip the next part
        if (meeting != null)
        {
            UnityEngine.Debug.Log("Meeting point");
            //Get the path from the meeting point back to start or end
            for (int i = 0; i < 2; i++)
            {
                done = false;
                //Add the first meeting point to the path list
                path.Add(new int[] { meeting[0 + 3 * i], meeting[1 + 3 * i], meeting[2 + 3 * i] });
                //The current point is the first meeting point
                (currentPoint[0], currentPoint[1], currentPoint[2])
                    = (meeting[0 + 3 * i], meeting[1 + 3 * i], meeting[2 + 3 * i]);
                //Get the path chain from the first meeting point
                while (!done)
                {
                    //Add the origin of the current point to the path
                    path.Add(new int[] {originPoints[currentPoint[0], currentPoint[1], currentPoint[2], 0],
                    originPoints[currentPoint[0], currentPoint[1], currentPoint[2], 1],
                    originPoints[currentPoint[0], currentPoint[1], currentPoint[2], 2]
                });

                    //The origin is the new current point
                    (currentPoint[0], currentPoint[1], currentPoint[2])
                    = (originPoints[currentPoint[0], currentPoint[1], currentPoint[2], 0],
                    originPoints[currentPoint[0], currentPoint[1], currentPoint[2], 1],
                    originPoints[currentPoint[0], currentPoint[1], currentPoint[2], 2]);

                    //If the new current point is the start or end point 
                    if ((currentPoint[0], currentPoint[1], currentPoint[2])
                        == (startEnd[0], startEnd[1], startEnd[2]) ||
                        (currentPoint[0], currentPoint[1], currentPoint[2])
                        == (startEnd[3], startEnd[4], startEnd[5]))
                    {
                        done = true;
                    }
                }
            }

            //Save the found path as 4 values in the cavesMap
            for (int i = 0; i < path.Count; i++)
            {
                cavesMap[path[i][0], path[i][1], path[i][2]] = 4;
            }
        }
        else
        {
            UnityEngine.Debug.Log("No path found");
        }

        return cavesMap;
    }

    //This function finds a random start and end point for the caves map
    int[] RandomStartEnd(int[,,] cavesMap)
    {
        //The variables
        int x1 = 0;
        int y1 = 0;
        int z1 = 0;
        bool foundStart = false;
        int x2 = cavesMap.GetLength(0) - 1;
        int y2 = 0;
        int z2 = 0;
        bool foundEnd = false;
        int i;

        //Keep track of potential start and end spots
        List<int[]> startSpots = new List<int[]>();
        List<int[]> endSpots = new List<int[]>();

        //Look for potential start and end points until at least 1 start and end spot have been found
        while (!foundStart || !foundEnd)
        {
            //Go trough all posible y and z values and keep track of any open space for the start or end
            for (int y = 0; y < cavesMap.GetLength(1); y++)
            {
                for (int z = 0; z < cavesMap.GetLength(2); z++)
                {
                    //Check for air if the list isn't already created
                    if ((!foundStart) && cavesMap[x1, y, z] == 0)
                    {
                        startSpots.Add(new int[] { y, z });
                    }
                    //Check for air if the list isn't already created
                    if ((!foundEnd) && cavesMap[x2, y, z] == 0)
                    {
                        endSpots.Add(new int[] { y, z });
                    }
                }
            }

            //Check if any startSpots were found 
            if (startSpots.Count > 0)
            {
                //StartSpot found
                foundStart = true;
            }
            else
            {
                //If no startSpots were found increase the x1 value
                x1++;
            }
            if (endSpots.Count > 0)
            {
                //EndSpotFound
                foundEnd = true;
            }
            else
            {
                //If no startSpots were found decrease the x2 value
                x2--;
            }

            //Failsave for the while loop when the end has the same x as the start
            if (x1 == x2)
            {
                foundStart = true;
                foundEnd = true;
                x1 = -1;
                UnityEngine.Debug.Log("No caves found");
            }
        }
        if (x1 != -1)
        {
            //If there were no startSpots found y1 and z1 remain 0
            if (startSpots.Count > 0)
            {
                //Get a random starting position from the list of potential start spots
                i = Random.Range(0, startSpots.Count);
                y1 = startSpots[i][0];
                z1 = startSpots[i][1];
            }
            //If there were no endSpots found y2 and z2 remain 0
            if (endSpots.Count > 0)
            {
                //Get a random ending position from the list of potential end spots
                i = Random.Range(0, endSpots.Count);
                y2 = endSpots[i][0];
                z2 = endSpots[i][1];
            }
        }

        //Return the start and end points
        if (x1 == -1)
        {
            return null;
        }
        else
        {
            return new int[] { x1, y1, z1, x2, y2, z2 };
        }
    }

    
    //This function returns 0: dead or 1: alive based on cell position and previous itiration cavesMap
    int CheckSurounding(int[,,] previousCaves, int x, int y, int z, int birthLimit, int deathLimit)
    {
        int i = 0;

        //Edges count as alive else check the neighbor, count surrounding alive cells
        for (int u = y-1; u <= y + 1; u++)
        {
            if (u == -1 || u == previousCaves.GetLength(1)) { i += 9; }
            else
            {
                for (int v = z - 1; v <= z + 1; v++)
                {
                    if (v == -1 || v == previousCaves.GetLength(2)) { i += 3; }
                    else
                    {
                        for (int w = x - 1; w <= x + 1; w++)
                        {
                            if (w == -1 || w == previousCaves.GetLength(0)) { i += 1; }
                            else
                            {
                                if (!(w == x && u == y && v == z))
                                {
                                    i += previousCaves[w, u, v];
                                }
                            }
                        }
                    }
                }
            }
        }


        //Different check for alive or dead cell
        if (previousCaves[x, y, z] == 0)
        {
            //Dead cell gets reborn if there are enough neighbors
            if (i > birthLimit) { return 1; }
            else {return 0;}
        }
        else
        {
            //Alive cell dies with to few neighbors
            if (i < deathLimit) { return 0; }
            else { return 1; }
        }
    }

    public int GetCell (Vector3 chunkPos, int x, int y, int z)
    {
        return data[(int)chunkPos.x, (int)chunkPos.y, (int)chunkPos.z, x, y, z];
    }

    public int GetNeighbor(int chunkSize, Vector3 chunkPos, int x, int y, int z, Direction dir, int dist = 1)
    {
        int counter;
        DataCoordinate offSetToCheck = offsets[(int)dir];
        DataCoordinate neighborCoord = new DataCoordinate(x + 
            offSetToCheck.x * dist, y + offSetToCheck.y * dist, z + offSetToCheck.z * dist);
        counter = 0;
        while (neighborCoord.x < 0 && counter < data.GetLength(0))
        {
            counter++;
            neighborCoord.x += chunkSize;
            chunkPos.x--;
            if (counter == data.GetLength(0))
            {
                UnityEngine.Debug.Log(chunkPos + " " + neighborCoord);
            }
        }
        counter = 0;
        while (neighborCoord.y < 0 && counter < data.GetLength(0))
        {
            counter++;
            neighborCoord.y += chunkSize;
            chunkPos.y--;
            if (counter == data.GetLength(0))
            {
                UnityEngine.Debug.Log(chunkPos + " " + neighborCoord);
            }
        }
        counter = 0;
        while (neighborCoord.z < 0 && counter < data.GetLength(0))
        {
            counter++;
            neighborCoord.z += chunkSize;
            chunkPos.z--;
            if (counter == data.GetLength(0))
            {
                UnityEngine.Debug.Log(chunkPos + " " + neighborCoord);
            }
        }
        counter = 0;
        while (neighborCoord.x >= chunkSize && counter < data.GetLength(0))
        {
            counter++;
            neighborCoord.x -= chunkSize;
            chunkPos.x++;
            if (counter == data.GetLength(0))
            {
                UnityEngine.Debug.Log(chunkPos + " " + neighborCoord);
            }
        }
        counter = 0;
        while (neighborCoord.y >= chunkSize && counter < data.GetLength(0))
        {
            counter++;
            neighborCoord.y -= chunkSize;
            chunkPos.y++;
            if (counter == data.GetLength(0))
            {
                UnityEngine.Debug.Log(chunkPos + " " + neighborCoord);
            }
        }
        counter = 0;
        while (neighborCoord.z >= chunkSize && counter < data.GetLength(0))
        {
            counter++;
            neighborCoord.z -= chunkSize;
            chunkPos.z++;
            if (counter == data.GetLength(0))
            {
                UnityEngine.Debug.Log(chunkPos + " " + neighborCoord);
            }
        }

        if (chunkPos.x < 0 || chunkPos.x >= data.GetLength(0) ||
            chunkPos.y < 0 || chunkPos.y >= data.GetLength(1) ||
            chunkPos.z < 0 || chunkPos.z >= data.GetLength(2))
        {
            return 0;
        }
        else
        {
            return GetCell(chunkPos, neighborCoord.x, neighborCoord.y, neighborCoord.z);
        }
    }

    struct DataCoordinate
    {
        public int x;
        public int y;
        public int z;

        public DataCoordinate(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    DataCoordinate[] offsets =
    {
        new DataCoordinate(0, 0, 1),
        new DataCoordinate(1, 0, 0),
        new DataCoordinate(0, 0, -1),
        new DataCoordinate(-1, 0, 0),
        new DataCoordinate(0, 1, 0),
        new DataCoordinate(0, -1, 0)
    };
}
