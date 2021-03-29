using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    public int size;
    public float r;

    float stepSizeLon;
    float lon;
    float stepSizeLat;
    float lat;

    int count;
    int k;

    float x;
    float y;
    float z;


    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        vertices = new Vector3[size * size];
        triangles = new int[3 * size * size * 2];

        count = 0;

        stepSizeLon = 2 * Mathf.PI / size;
        stepSizeLat = Mathf.PI / size;

        for(int i = 0; i < size; i++)
        {
            lon = -Mathf.PI + i * stepSizeLon;
            for (int j = 0; j < size; j++)
            {
                lat = -Mathf.PI * 0.5f + j * stepSizeLat;
                x = r * Mathf.Sin(lon) * Mathf.Cos(lat);
                y = r * Mathf.Sin(lon) * Mathf.Sin(lat);
                z = r * Mathf.Cos(lon);
                vertices[count] = new Vector3(x, y, z);
                count++;
            }
        }
        k = 0;
        for (int j = 0; j < size - 1; j++)
        {
            for (int i = 0; i < size; i++)
            {
                if (j == size - 1)
                {
                    if (i == size - 1)
                    {
                        //Plane 1
                        triangles[k] = i + j * size;
                        k++;
                        triangles[k] = j * size;
                        k++;
                        triangles[k] = i;
                        k++;
                        //Plane 2
                        triangles[k] = j * size;
                        k++;
                        triangles[k] = 0;
                        k++;
                        triangles[k] = i;
                        k++;
                    }
                    else
                    {
                        //Plane 1
                        triangles[k] = i + j * size;
                        k++;
                        triangles[k] = i + 1 + j * size;
                        k++;
                        triangles[k] = i;
                        k++;
                        //Plane 2
                        triangles[k] = i + 1 + j * size;
                        k++;
                        triangles[k] = i + 1;
                        k++;
                        triangles[k] = i;
                        k++;
                    }
                }
                else
                {
                    if (i == size - 1)
                    {
                        //Plane 1
                        triangles[k] = i + j * size;
                        k++;
                        triangles[k] = j * size;
                        k++;
                        triangles[k] = i + (j + 1) * size;
                        k++;
                        //Plane 2
                        triangles[k] = j * size;
                        k++;
                        triangles[k] = (j + 1) * size;
                        k++;
                        triangles[k] = i + (j + 1) * size;
                        k++;
                    }
                    else
                    {
                        //Plane 1
                        triangles[k] = i + j * size;
                        k++;
                        triangles[k] = i + 1 + j * size;
                        k++;
                        triangles[k] = i + (j + 1) * size;
                        k++;
                        //Plane 2
                        triangles[k] = i + 1 + j * size;
                        k++;
                        triangles[k] = i + 1 + (j + 1) * size;
                        k++;
                        triangles[k] = i + (j + 1) * size;
                        k++;
                    }
                }
            }
        }


        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }

    // Update is called once per frame
    void Update()
    {
        vertices = mesh.vertices;

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}
