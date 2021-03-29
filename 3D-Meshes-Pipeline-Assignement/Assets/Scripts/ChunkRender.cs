using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkRender : MonoBehaviour
{
    Mesh mesh;
    List<Vector3> vertices;
    List<int> triangles;
    List<Vector2> uvs;

    float adjScale;
    Vector2 uvO;

    //Update the mesh of this chunk
    public void UpdateChunkMesh(VoxelData data, Vector3 chunkPos, int chunkSize, float cubeSize)
    {
        mesh = GetComponent<MeshFilter>().mesh;
        adjScale = cubeSize * 0.5f;
        GenerateChunk(data, chunkPos, chunkSize, cubeSize);
        UpdateMesh();
    }

    //This function generates the vertices and triangles based on the given data
    void GenerateChunk(VoxelData data, Vector3 chunkPos, int chunkSize, float cubeSize)
    {
        //Create a new list of vertices
        vertices = new List<Vector3>();
        //Create a new list of triangles
        triangles = new List<int>();

        uvs = new List<Vector2>();

        //Loop trough the x y and z coordinates
        for (int z = 0; z < chunkSize; z++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    //If the cell is empty skip this cube
                    if (data.GetCell(chunkPos, x, y, z) == 0)
                    {
                        continue;
                    }
                    //If the cell isn't empty make a cube at this location
                    MakeCube(chunkSize, chunkPos, adjScale, new Vector3((float)x * cubeSize, (float)y * cubeSize, (float)z * cubeSize), x, y, z, data);
                }
            }
        }
    }

    //This function updates the mesh based on the calculated vertices and triangles
    void UpdateMesh()
    {
        if (vertices.Count == 0)
        {
            gameObject.SetActive(false);
        }
        //When updating the mesh, clear the current mesh
        mesh.Clear();

        //Add the calculated vertices as an array
        mesh.vertices = vertices.ToArray();
        //Add the calculated triangles as an array
        mesh.triangles = triangles.ToArray();

        mesh.uv = uvs.ToArray();

        mesh.Optimize();
        //Recalculate the normals for lighting
        mesh.RecalculateNormals();
    }

    //Create a voxel cubewith the correct scale at the given location based on the data
    void MakeCube(int chunkSize, Vector3 chunkPos, float tempScale, Vector3 cubePos, int x, int y, int z, VoxelData data)
    {
        cubePos.x += chunkPos.x * chunkSize;
        cubePos.y += chunkPos.y * chunkSize;
        cubePos.z += chunkPos.z * chunkSize;


        //Check all 6 sides
        for (int i = 0; i < 6; i++)
        {
            //If there is no neighbor in the direction of that size create a cube face
            if (data.GetNeighbor(chunkSize, chunkPos, x, y, z, (Direction)i) == 0)
            {
                //Create a cube face
                MakeFace((Direction)i, tempScale, cubePos, data.GetCell(chunkPos, x,y,z));
            }
        }
    }

    //Create a cube face by adding 4 vertices and 2 triangles
    void MakeFace(Direction dir, float faceScale, Vector3 facePos, int blockType)
    {
        //Add the vertices for the cube face at the correct position and scale
        vertices.AddRange(CubeMeshData.FaceVertices(dir, faceScale, facePos));
        //Take a count after adding the 4 new vertices
        int vCount = vertices.Count;

        //Use the count to create 2 triangles with the last 4 added vertices
        triangles.Add(vCount - 4);
        triangles.Add(vCount - 4 + 1);
        triangles.Add(vCount - 4 + 2);
        triangles.Add(vCount - 4);
        triangles.Add(vCount - 4 + 2);
        triangles.Add(vCount - 4 + 3);

        switch (blockType)
        {
            case 1:
                uvO = new Vector2(0 ,6 * 0.125f);
                break;
            case 2:
                if (dir == Direction.Up)
                {
                    uvO = new Vector2(1 * 0.125f, 7 * 0.125f);
                }
                else if (dir == Direction.Down)
                {
                    uvO = new Vector2(1 * 0.125f, 6 * 0.125f);
                }
                else
                {
                    uvO = new Vector2(0, 7 * 0.125f);
                }
                break;
            case 3:
                uvO = new Vector2(1 * 0.125f, 6 * 0.125f);
                break;
            case 9:
                uvO = new Vector2(1 * 0.125f, 5 * 0.125f);
                break;
        }

        uvs.Add(new Vector2(0.125f, 0.125f) + uvO);
        uvs.Add(new Vector2(0, 0.125f) + uvO);
        uvs.Add(new Vector2(0, 0) + uvO);
        uvs.Add(new Vector2(0.125f,0) + uvO);


    }
}
