using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public static class Kruskal
    {
        public static List<LineString> FindMinimumSpanningTree(List<Point> points, GeometryFactory geometry)
        {
            int count = points.Count;

            // 计算点之间的距离并保存为边
            List<Tuple<int, int, double>> edges = new List<Tuple<int, int, double>>();
            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    double d = Distance(points[i], points[j]);
                    if (d <= 15000)
                    {
                        edges.Add(Tuple.Create(i, j, d));
                    }
                }
            }

            // 按距离排序边
            edges.Sort((a, b) => a.Item3.CompareTo(b.Item3));

            // 初始化并查集
            int[] parent = new int[count];
            for (int i = 0; i < count; i++)
            {
                parent[i] = i;
            }

            // 查找并查集的根节点
            int Find(int x)
            {
                if (parent[x] != x)
                {
                    parent[x] = Find(parent[x]);
                }
                return parent[x];
            }

            // 合并并查集
            bool Union(int x, int y)
            {
                int rootX = Find(x);
                int rootY = Find(y);
                if (rootX == rootY)
                {
                    return false;
                }
                parent[rootX] = rootY;
                return true;
            }

            // 构建最小生成树
            var result = new List<LineString>();
            foreach (var edge in edges)
            {
                int u = edge.Item1;
                int v = edge.Item2;
                if (Union(u, v))
                {
                    result.Add(geometry.CreateLineString(new[] {
                new Coordinate(points[u].X, points[u].Y),
                new Coordinate(points[v].X, points[v].Y) }));
                }
            }
            return result;
        }

        //public static List<LineString> FindMinimumSpanningTree(List<Point> points, GeometryFactory geometry)
        //{
        //    int count = points.Count;
        //    double[,] dist = new double[count, count];

        //    // 初始化距离矩阵
        //    for (int i = 0; i < count; i++)
        //    {
        //        for (int j = 0; j < count; j++)
        //        {
        //            if (i == j)
        //            {
        //                dist[i, j] = 0;
        //            }
        //            else
        //            {
        //                double d = Distance(points[i], points[j]);
        //                dist[i, j] = d <= 15000 ? d : double.PositiveInfinity;
        //            }
        //        }
        //    }

        //    // Floyd-Warshall算法求解所有点对最短路径
        //    for (int k = 0; k < count; k++)
        //    {
        //        for (int i = 0; i < count; i++)
        //        {
        //            for (int j = 0; j < count; j++)
        //            {
        //                if (dist[i, k] + dist[k, j] < dist[i, j])
        //                {
        //                    dist[i, j] = dist[i, k] + dist[k, j];
        //                }
        //            }
        //        }
        //    }

        //    // 构建最小生成树
        //    var visited = new HashSet<int>() { 0 };
        //    var result = new List<LineString>();

        //    while (visited.Count < count)
        //    {
        //        double minDist = double.PositiveInfinity;
        //        int u = -1;
        //        int v = -1;

        //        // 在未访问的点中寻找距离最小的边
        //        foreach (var i in visited)
        //        {
        //            for (int j = 0; j < count; j++)
        //            {
        //                if (!visited.Contains(j) && dist[i, j] < minDist)
        //                {
        //                    minDist = dist[i, j];
        //                    u = i;
        //                    v = j;
        //                }
        //            }
        //        }



        //        if (u != -1 && v != -1)
        //        {
        //            visited.Add(v);
        //            result.Add(geometry.CreateLineString(new[] {
        //                new Coordinate(points[u].X, points[u].Y),
        //                new Coordinate(points[v].X, points[v].Y) }));
        //        }
        //    }
        //    return result;
        //}

        private static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

    }
}
