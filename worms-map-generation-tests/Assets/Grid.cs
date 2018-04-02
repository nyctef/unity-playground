using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Grid : MonoBehaviour
{
    public int Width = 10, Height = 5;

    private Vector3[] vertices;

    private void Awake()
    {
        StartCoroutine(Generate());
    }

    private IEnumerator Generate()
    {
        var wait = new WaitForSeconds(0.05f);

        var mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "Procedural Grid";

        vertices = new Vector3[(Width + 1) * (Height + 1)];
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0, y = 0; y <= Height; y++)
            for (int x = 0; x <= Width; x++, i++)
            {
                vertices[i] = new Vector3(x, y);
                uv[i] = new Vector2((float)x / Width, (float)y / Height);
            }

        mesh.vertices = vertices;

        int[] triangles = new int[Width*Height*6];
        for (int ti = 0, vi = 0, y = 0; y < Height; y++, vi++)
        {
            for (int x = 0; x < Width; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + Width + 1;
                triangles[ti + 5] = vi + Width + 2;
                //yield return wait;
            }
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.uv = uv;

        yield break;
    }

    private void OnDrawGizmos()
    {
        if (vertices == null)
        {
            return;
        }

        Gizmos.color = Color.black;
        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], 0.1f);
        }
    }
}