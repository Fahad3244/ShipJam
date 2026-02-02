using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RopeMeshGenerator : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float ropeWidth = 0.1f;
    public int segments = 20;

    private Mesh mesh;

    private void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        Vector3 dir = (endPoint.position - startPoint.position).normalized;
        Vector3 perpendicular = Vector3.Cross(dir, Vector3.up).normalized * ropeWidth;

        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 point = Vector3.Lerp(startPoint.position, endPoint.position, t);

            vertices[i * 2] = point + perpendicular;
            vertices[i * 2 + 1] = point - perpendicular;

            uv[i * 2] = new Vector2(t, 0);
            uv[i * 2 + 1] = new Vector2(t, 1);

            if (i < segments)
            {
                int idx = i * 6;
                int vi = i * 2;

                triangles[idx] = vi;
                triangles[idx + 1] = vi + 2;
                triangles[idx + 2] = vi + 1;

                triangles[idx + 3] = vi + 1;
                triangles[idx + 4] = vi + 2;
                triangles[idx + 5] = vi + 3;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }
}
