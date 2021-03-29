using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CubeMeshData
{
    //This is the basic mesh data for a cube

    //The 8 vertices that make up a cube
    public static Vector3[] vertices =
    {
        new Vector3(1, 1, 1),
        new Vector3(-1, 1, 1),
        new Vector3(-1, -1, 1),
        new Vector3(1, -1, 1),
        new Vector3(-1, 1, -1),
        new Vector3(1, 1, -1),
        new Vector3(1, -1, -1),
        new Vector3(-1, -1, -1),
    };

    //the 6 faces of the cube in the correct order to make triangles
    public static int[][] faceTriangles =
    {
        new int[] {0, 1, 2, 3},
        new int[] {5, 0, 3, 6},
        new int[] {4, 5, 6, 7},
        new int[] {1, 4, 7, 2},
        new int[] {5, 4, 1, 0},
        new int[] {3, 2, 7, 6}
    };

    //Return the 4 vertices that make up a face on the cube
    public static Vector3[] FaceVertices(int dir, float scale, Vector3 pos)
    {
        //Create an array for the 4 vertices
        Vector3[] fv = new Vector3[4];
        //Loop through the 4 vertices
        for (int i = 0; i < fv.Length; i++)
        {
            //Get the correct vertice based on the given face direction and add the position for correct placement
            fv[i] = (vertices[faceTriangles[dir][i]] * scale) + pos;
        }
        //Return the 4 vertices
        return fv;
    }

    //Overload using Direction instead of an int
    public static Vector3[] FaceVertices(Direction dir, float scale, Vector3 pos)
    {
        return FaceVertices((int)dir, scale, pos);
    }
}
//the 6 face directions of a cube
public enum Direction
{
    North,
    East,
    South,
    West,
    Up,
    Down
}
