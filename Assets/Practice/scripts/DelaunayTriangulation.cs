using System.Collections.Generic;
using Unity.Mathematics;

public static class DelaunayTriangulation
{
    public struct Edge
    {
        public int A;
        public int B;

        public Edge(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    public struct Triangle
    {
        public int A;
        public int B;
        public int C;

        public Triangle(int a, int b, int c)
        {
            A = a;
            B = b;
            C = c;
        }

        public bool ContainsVertex(int v)
        {
            return A == v || B == v || C == v;
        }
    }

    public static List<Triangle> Generate(List<float2> points)
    {
        List<float2> vertices = new List<float2>(points);

        float minX = points[0].x;
        float minY = points[0].y;
        float maxX = points[0].x;
        float maxY = points[0].y;

        foreach (var p in points)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        float dx = maxX - minX;
        float dy = maxY - minY;
        float delta = math.max(dx, dy) * 10f;

        float2 p1 = new float2(minX - delta, minY - delta);
        float2 p2 = new float2(minX - delta, maxY + delta * 2);
        float2 p3 = new float2(maxX + delta * 2, minY - delta);

        int superA = vertices.Count;
        int superB = vertices.Count + 1;
        int superC = vertices.Count + 2;

        vertices.Add(p1);
        vertices.Add(p2);
        vertices.Add(p3);

        List<Triangle> triangles = new List<Triangle>
        {
            new Triangle(superA, superB, superC)
        };

        for (int pointIndex = 0; pointIndex < points.Count; pointIndex++)
        {
            float2 point = vertices[pointIndex];

            List<Triangle> badTriangles = new();
            List<Edge> polygon = new();

            foreach (var triangle in triangles)
            {
                if (PointInCircumcircle(point, triangle, vertices))
                    badTriangles.Add(triangle);
            }

            foreach (var triangle in badTriangles)
            {
                AddEdge(polygon, new Edge(triangle.A, triangle.B));
                AddEdge(polygon, new Edge(triangle.B, triangle.C));
                AddEdge(polygon, new Edge(triangle.C, triangle.A));
            }

            foreach (var triangle in badTriangles)
                triangles.Remove(triangle);

            foreach (var edge in polygon)
            {
                triangles.Add(new Triangle(
                    edge.A,
                    edge.B,
                    pointIndex));
            }
        }

        triangles.RemoveAll(t =>
            t.ContainsVertex(superA) ||
            t.ContainsVertex(superB) ||
            t.ContainsVertex(superC));

        return triangles;
    }

    private static void AddEdge(List<Edge> edges, Edge edge)
    {
        for (int i = edges.Count - 1; i >= 0; i--)
        {
            if ((edges[i].A == edge.B && edges[i].B == edge.A) ||
                (edges[i].A == edge.A && edges[i].B == edge.B))
            {
                edges.RemoveAt(i);
                return;
            }
        }

        edges.Add(edge);
    }

   private static bool PointInCircumcircle(
    float2 p,
    Triangle t,
    List<float2> vertices)
{
    float2 a = vertices[t.A];
    float2 b = vertices[t.B];
    float2 c = vertices[t.C];

    float orientation =
        (b.x - a.x) * (c.y - a.y) -
        (b.y - a.y) * (c.x - a.x);

    float ax = a.x - p.x;
    float ay = a.y - p.y;

    float bx = b.x - p.x;
    float by = b.y - p.y;

    float cx = c.x - p.x;
    float cy = c.y - p.y;

    float det =
        (ax * ax + ay * ay) * (bx * cy - cx * by)
      - (bx * bx + by * by) * (ax * cy - cx * ay)
      + (cx * cx + cy * cy) * (ax * by - bx * ay);

    return orientation > 0f
        ? det > 0f
        : det < 0f;
}
}