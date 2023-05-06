using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;

namespace TimeIsLife.Model
{
    public static class GrahamScan
    {
        public static List<LineString> FindConvexHull(List<Point> points, GeometryFactory geometry)
        {
            int count = points.Count;
            var result = new List<LineString>();

            int p = 0;
            for (int i = 1; i < count; i++)
            {
                if (points[i].Y < points[p].Y || (points[i].Y == points[p].Y && points[i].X < points[p].X))
                {
                    p = i;
                }
            }
            Point pivot = points[p];
            points.RemoveAt(p);

            points.Sort((a, b) =>
            {
                double angleA = Math.Atan2(a.Y - pivot.Y, a.X - pivot.X);
                double angleB = Math.Atan2(b.Y - pivot.Y, b.X - pivot.X);
                if (angleA < angleB) return -1;
                if (angleA > angleB) return 1;

                double distA = Math.Sqrt(Math.Pow(a.X - pivot.X, 2) + Math.Pow(a.Y - pivot.Y, 2));
                double distB = Math.Sqrt(Math.Pow(b.X - pivot.X, 2) + Math.Pow(b.Y - pivot.Y, 2));
                return distA.CompareTo(distB);
            });

            var hull = new Stack<Point>();
            hull.Push(pivot);
            hull.Push(points[0]);
            hull.Push(points[1]);

            for (int i = 2; i < count - 1; i++)
            {
                while (hull.Count >= 2)
                {
                    Point top = hull.Pop();
                    Point nextToTop = hull.Peek();
                    double direction = (top.X - nextToTop.X) * (points[i].Y - nextToTop.Y) - (top.Y - nextToTop.Y) * (points[i].X - nextToTop.X);
                    if (direction >= 0)
                    {
                        hull.Push(top);
                        break;
                    }
                }
                hull.Push(points[i]);
            }

            Point first = null;
            Point prev = null;
            while (hull.Count > 0)
            {
                Point point = hull.Pop();
                if (first == null)
                {
                    first = point;
                }
                else
                {
                    result.Add(geometry.CreateLineString(new[] {
                    new Coordinate(prev.X, prev.Y),
                    new Coordinate(point.X, point.Y) }));
                }
                prev = point;
            }
            result.Add(geometry.CreateLineString(new[] {
            new Coordinate(prev.X, prev.Y),
            new Coordinate(first.X, first.Y) }));

            return result;
        }
    }
}
