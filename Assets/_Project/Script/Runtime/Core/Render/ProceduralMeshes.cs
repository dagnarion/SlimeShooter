using System.Collections.Generic;
using UnityEngine;

/// <summary>Mesh placeholder sinh bằng code (thay bằng model thật sau).</summary>
public static class ProceduralMeshes
{
    /// <summary>
    /// Máng nước chạy theo đường băng chuyền. Submesh 0 = lòng máng (UV.x = quãng đường / width),
    /// submesh 1 = thành máng hai bên.
    /// </summary>
    public static Mesh Channel(IConveyorPath path, float width, float floorY, float wallHeight, float wallThickness, float sampleStep = 0.25f)
    {
        int samples = Mathf.Max(16, Mathf.CeilToInt(path.Length / sampleStep));
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var floor = new List<int>();
        var walls = new List<int>();
        float half = width * 0.5f;
        float top = floorY + wallHeight;

        // Mỗi mẫu: 0 floorIn, 1 floorOut, 2 wallInTop, 3 wallInTopInner, 4 wallOutTop, 5 wallOutTopOuter, 6 wallInBottomInner, 7 wallOutBottomOuter
        const int stride = 8;
        for (int i = 0; i <= samples; i++)
        {
            float d = path.Length * i / samples;
            Vector3 p = path.Evaluate(d, out Vector3 forward);
            Vector3 inward = RectConveyorPath.Inward(forward);
            p.y = floorY;
            float u = d / width;

            Vector3 inEdge = p + inward * half;
            Vector3 outEdge = p - inward * half;
            vertices.Add(inEdge); uvs.Add(new Vector2(u, 1f));
            vertices.Add(outEdge); uvs.Add(new Vector2(u, 0f));
            vertices.Add(new Vector3(inEdge.x, top, inEdge.z)); uvs.Add(new Vector2(u, 1f));
            vertices.Add(new Vector3(inEdge.x, top, inEdge.z) + inward * wallThickness); uvs.Add(new Vector2(u, 1f));
            vertices.Add(new Vector3(outEdge.x, top, outEdge.z)); uvs.Add(new Vector2(u, 0f));
            vertices.Add(new Vector3(outEdge.x, top, outEdge.z) - inward * wallThickness); uvs.Add(new Vector2(u, 0f));
            vertices.Add(inEdge + inward * wallThickness + Vector3.down * 0.05f); uvs.Add(new Vector2(u, 1f));
            vertices.Add(outEdge - inward * wallThickness + Vector3.down * 0.05f); uvs.Add(new Vector2(u, 0f));

            if (i == 0) continue;
            int a = (i - 1) * stride;
            int b = i * stride;
            // Unity: mặt trước = thứ tự đỉnh theo chiều kim đồng hồ khi nhìn từ phía mặt đó.
            Quad(floor, a + 1, a + 0, b + 0, b + 1);          // lòng máng (nhìn từ trên)
            Quad(walls, a + 0, a + 2, b + 2, b + 0);          // thành trong, mặt hướng lòng máng
            Quad(walls, a + 2, a + 3, b + 3, b + 2);          // thành trong, mặt trên
            Quad(walls, b + 6, b + 3, a + 3, a + 6);          // thành trong, mặt hướng board
            Quad(walls, b + 1, b + 4, a + 4, a + 1);          // thành ngoài, mặt hướng lòng máng
            Quad(walls, a + 5, a + 4, b + 4, b + 5);          // thành ngoài, mặt trên
            Quad(walls, a + 7, a + 5, b + 5, b + 7);          // thành ngoài, mặt hướng ra ngoài
        }

        var mesh = new Mesh { name = "ConveyorChannel" };
        mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(floor, 0);
        mesh.SetTriangles(walls, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Vòng xuyến (phao) nằm trên mặt phẳng XZ.</summary>
    public static Mesh Torus(float radius, float tube, int segments = 24, int sides = 12)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var triangles = new List<int>();
        for (int s = 0; s <= segments; s++)
        {
            float a = s / (float)segments * Mathf.PI * 2f;
            var center = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            var radial = center.normalized;
            for (int t = 0; t <= sides; t++)
            {
                float b = t / (float)sides * Mathf.PI * 2f;
                Vector3 normal = radial * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b);
                vertices.Add(center + normal * tube);
                normals.Add(normal);
            }
        }
        int ring = sides + 1;
        for (int s = 0; s < segments; s++)
        for (int t = 0; t < sides; t++)
        {
            int a = s * ring + t;
            int b = (s + 1) * ring + t;
            Quad(triangles, a, a + 1, b + 1, b);
        }

        var mesh = new Mesh { name = "Torus" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void Quad(List<int> triangles, int a, int b, int c, int d)
    {
        triangles.Add(a); triangles.Add(b); triangles.Add(c);
        triangles.Add(a); triangles.Add(c); triangles.Add(d);
    }
}
