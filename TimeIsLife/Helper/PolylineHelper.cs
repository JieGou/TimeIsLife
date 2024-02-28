using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using DotNetARX;

using NetTopologySuite.Geometries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Helper
{
    internal static class PolylineHelper
    {
        /// <summary>
        /// Polyline转换为NTS的二维Polygon
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="geometryFactory"></param>
        /// <returns></returns>
        public static Polygon ToPolygon(this Polyline polyline, GeometryFactory geometryFactory)
        {
            int n = polyline.NumberOfVertices;
            var coordinates = new Coordinate[n + 1];

            for (int i = 0; i < n + 1; i++)
            {
                coordinates[i] = new Coordinate(polyline.GetPoint3dAt(i % n).X, polyline.GetPoint3dAt(i % n).Y);
            }

            return geometryFactory.CreatePolygon(coordinates);
        }

        /// <summary>
        /// 获取三维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Point3dCollection GetPoint3dCollection(this Polyline polyline)
        {
            Point3dCollection points = new Point3dCollection();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint3dAt(i));
            }

            return points;
        }

        /// <summary>
        /// 获取三维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Point3dCollection GetPoint3dCollection(this Polyline polyline, Matrix3d ucsToWcsMatrix3d)
        {
            Point3dCollection points = new Point3dCollection();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(polyline.GetPoint3dAt(i).TransformBy(ucsToWcsMatrix3d.Inverse()));
            }

            return points;
        }

        /// <summary>
        /// 获取二维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static Point2dCollection GetPoint2DCollection(this Polyline polyline)
        {
            Point2dCollection points = new Point2dCollection();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(new Point2d(polyline.GetPoint3dAt(i).X, polyline.GetPoint3dAt(i).Y));
            }

            return points;
        }

        /// <summary>
        /// 获取二维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Point2d> GetPoint2Ds(this Polyline polyline)
        {
            List<Point2d> point2ds = new List<Point2d>();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                point2ds.Add(new Point2d(polyline.GetPoint3dAt(i).X, polyline.GetPoint3dAt(i).Y));
            }

            return point2ds;
        }

        /// <summary>
        /// 获取二维点集
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Coordinate> GetCoordinates(this Polyline polyline)
        {
            List<Coordinate> coordinates = new List<Coordinate>();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                coordinates.Add(new Coordinate(polyline.GetPoint3dAt(i).X, polyline.GetPoint3dAt(i).Y));
            }

            return coordinates;
        }

        /// <summary>
        /// 判断多边形1是否在多边形2的内部，true为在内部，false为不在内部
        /// </summary>
        /// <param name="roomPolyline">多边形1</param>
        /// <param name="slabPolyline">多边形2</param>
        /// <returns></returns>
        public static bool IsPolylineInPolyline(this Polyline Polyline1, Polyline Polyline2)
        {
            bool bo = true;
            for (int i = 0; i < Polyline1.NumberOfVertices; i++)
            {
                if (!Polyline1.GetPoint2dAt(i).IsInPolygon2(Polyline2.GetPoint2DCollection().ToArray().ToList()))
                {
                    bo = false;
                }
            }

            return bo;
        }

        public static string GetXValues(this Polyline polyline, int numSegments = 10)
        {
            List<double> xValues = new List<double>();
            ProcessPolyline(polyline, numSegments, (point) => xValues.Add(point.X));
            return string.Join(",", xValues.ToArray());
        }

        public static string GetYValues(this Polyline polyline, int numSegments = 10)
        {
            List<double> yValues = new List<double>();
            ProcessPolyline(polyline, numSegments, (point) => yValues.Add(point.Y));
            return string.Join(",", yValues.ToArray());
        }

        public static string GetZValues(this Polyline polyline, int numSegments = 10)
        {
            List<double> zValues = new List<double>();
            ProcessPolyline(polyline, numSegments, (point) => zValues.Add(point.Z));
            return string.Join(",", zValues.ToArray());
        }

        /// <summary>
        /// 处理多段线内的弧线段为近似直线段，基于指定的分段数量
        /// </summary>
        /// <param name="polyline">要处理的多段线</param>
        /// <param name="numSegments">弧线段的分段数量，默认为10</param>
        /// <param name="processPoint">对每个生成点进行处理的操作</param>
        private static void ProcessPolyline(Polyline polyline, int numSegments, Action<Point3d> processPoint)
        {
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                if (polyline.GetSegmentType(i) == SegmentType.Arc)
                {
                    CircularArc3d arc = polyline.GetArcSegmentAt(i);
                    // 不再基于弧长和分段长度计算分段数，而是直接使用numSegments参数

                    for (int j = 0; j <= numSegments; j++)
                    {
                        double param = (double)j / numSegments; // 确保正确的浮点数计算
                        Point3d pointOnArc = arc.EvaluatePoint(param);
                        processPoint(pointOnArc);
                    }
                }
                else
                {
                    // 对于非弧线段，直接处理该点
                    processPoint(polyline.GetPoint3dAt(i));
                }
            }
        }
    }
}
