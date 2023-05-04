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
            List<Point2d> points = new List<Point2d>();

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
        public static List<Coordinate> GetCoordinates(this Polyline polyline)
        {
            List<Coordinate> points = new List<Coordinate>();

            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                points.Add(new Coordinate(polyline.GetPoint3dAt(i).X, polyline.GetPoint3dAt(i).Y));
            }
            return points;
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

        public static string GetXValues(this Polyline polyline, double segmentLength = 300)
        {
            List<double> xValues = new List<double>();
            ProcessPolyline(polyline, segmentLength, (point) => xValues.Add(point.X));
            return string.Join(",", xValues.ToArray());
        }

        public static string GetYValues(this Polyline polyline, double segmentLength = 300)
        {
            List<double> yValues = new List<double>();
            ProcessPolyline(polyline, segmentLength, (point) => yValues.Add(point.Y));
            return string.Join(",", yValues.ToArray());
        }

        public static string GetZValues(this Polyline polyline, double segmentLength = 300)
        {
            List<double> zValues = new List<double>();
            ProcessPolyline(polyline, segmentLength, (point) => zValues.Add(point.Z));
            return string.Join(",", zValues.ToArray());
        }

        private static void ProcessPolyline(Polyline polyline, double segmentLength, Action<Point3d> processPoint)
        {
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                if (polyline.GetSegmentType(i) == SegmentType.Arc)
                {
                    CircularArc3d arc = polyline.GetArcSegmentAt(i);
                    double arcLength = arc.GetLength(0, 1, 1e-3);
                    int numSegments = (int)Math.Ceiling(arcLength / segmentLength);

                    for (int j = 0; j <= numSegments; j++)
                    {
                        double param = j / numSegments;
                        Point3d pointOnArc = arc.EvaluatePoint(param);
                        processPoint(pointOnArc);
                    }
                }
                else
                {
                    processPoint(polyline.GetPoint3dAt(i));
                }
            }
        }
    }
}
