using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TimeIsLife.ViewModel;

using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using TimeIsLife.ViewModel.LayoutViewModel;
using Autodesk.AutoCAD.ApplicationServices;

namespace TimeIsLife.Jig
{
    internal class LightingLayoutJig : DrawJig
    {
        private Document document;
        private Database database;
        private Editor editor;
        private Matrix3d _ucsToWcsMatrix3d;

        void Initialize()
        {
            document = Application.DocumentManager.CurrentDocument;
            database = document.Database;
            editor = document.Editor;
            _ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;
        }

        public List<BlockReference> blockReferences;
        private BlockReference blockReference;
        private Point3d _basePoint3d;
        private Point3d _endPoint3d;
        private Polyline _polyline;
        private BlockTableRecord _blockTableRecord;

        //构造函数，传入参数
        public LightingLayoutJig(BlockReference blockReference, Point3d basePoint3d, Polyline polyline, BlockTableRecord blockTableRecord)
        {
            Initialize();
            this.blockReference = blockReference;
            this._basePoint3d = basePoint3d;

            blockReferences = new List<BlockReference>();
            this._polyline = polyline;
            this._blockTableRecord = blockTableRecord;
        }

        protected override SamplerStatus Sampler(JigPrompts jigPrompts)
        {
            try
            {
                JigPromptPointOptions jigPromptPointOptions = new JigPromptPointOptions("\n请选择第二个角点：")
                {
                    Cursor = CursorType.Crosshair,
                    UserInputControls = UserInputControls.NoZeroResponseAccepted |
                                        UserInputControls.Accept3dCoordinates |
                                        UserInputControls.NoNegativeResponseAccepted,
                    BasePoint = _basePoint3d.TransformBy(_ucsToWcsMatrix3d),
                    UseBasePoint = true
                };

                PromptPointResult promptPointResult = jigPrompts.AcquirePoint(jigPromptPointOptions);
                Point3d tempEndPointd = promptPointResult.Value;
                if (promptPointResult.Status != PromptStatus.Cancel)
                {
                    if (_endPoint3d == tempEndPointd)
                    {
                        return SamplerStatus.NoChange;
                    }
                    else
                    {
                        blockReferences.Clear();
                        int num = 0;
                        while (true)
                        {
                            if (num >= (LightingRecLayoutViewModel.Instance.LightingRow * LightingRecLayoutViewModel.Instance.LightingColumn))
                            {
                                _endPoint3d = tempEndPointd;
                                Point3d _wcsEndPoint3d = _endPoint3d.TransformBy(_ucsToWcsMatrix3d.Inverse());
                                List<Point3d> list = GetLayoutPoint(_basePoint3d, _wcsEndPoint3d, _ucsToWcsMatrix3d, _polyline);
                                if (list.Count == 0)
                                {
                                    int num2 = 0;
                                    while (true)
                                    {
                                        if (num2 >= blockReferences.Count)
                                        {
                                            break;
                                        }

                                        blockReferences[num2].Position = _basePoint3d.TransformBy(_ucsToWcsMatrix3d);
                                        num2++;
                                    }
                                }
                                else if (list.Count < blockReferences.Count)
                                {
                                    int num3 = 0;
                                    while (true)
                                    {
                                        if (num3 >= blockReferences.Count)
                                        {
                                            break;
                                        }

                                        blockReferences[num3].Position = list[0];
                                        num3++;
                                    }
                                }
                                else
                                {
                                    int num4 = 0;
                                    while (true)
                                    {
                                        if (num4 >= blockReferences.Count)
                                        {
                                            break;
                                        }

                                        blockReferences[num4].Position = list[num4];
                                        num4++;
                                    }
                                }

                                break;
                            }


                            BlockReference item = (BlockReference)blockReference.Clone();
                            item.Rotation = 0;
                            item.TransformBy(_ucsToWcsMatrix3d);
                            Matrix3d matrixd2 = Matrix3d.Rotation((LightingLayoutSettingViewModel.Instance.BlockAngle * Math.PI) / 180.0, Vector3d.ZAxis, item.Position);
                            item.TransformBy(matrixd2);
                            item.ScaleFactors = new Scale3d(LightingLayoutSettingViewModel.Instance.BlockScale);

                            blockReferences.Add(item);
                            num++;
                        }
                    }

                    foreach (var reference in blockReferences)
                    {
                        if (_blockTableRecord.HasAttributeDefinitions)
                        {
                            foreach (var attri in reference.AttributeCollection)
                            {
                                AttributeReference ar = null;
                                if (attri is ObjectId)
                                {
                                    ar = ((ObjectId)attri).GetObject(OpenMode.ForWrite) as AttributeReference;
                                }
                                else if (attri is AttributeReference)
                                {
                                    ar = (AttributeReference)attri;
                                }

                                if (ar != null)
                                {

                                    Point3d point3D = blockReference.Position;
                                    ar.TransformBy(Matrix3d.Rotation(blockReference.Rotation, Vector3d.ZAxis, point3D.TransformBy(_ucsToWcsMatrix3d)).Inverse());
                                    ar.TransformBy(Matrix3d.Displacement(point3D.TransformBy(_ucsToWcsMatrix3d).GetVectorTo(reference.Position)));
                                }
                            }
                        }
                    }


                }
                else
                {
                    return SamplerStatus.Cancel;
                }
            }
            catch
            {
            }
            return 0;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            draw.Geometry.Draw(_polyline);
            for (int i = 0; i < blockReferences.Count; i++)
            {
                draw.Geometry.Draw(blockReferences[i]);
            }
            return true;
        }

        private List<Point3d> GetLayoutPoint(Point3d _basePoint3d, Point3d _wcsEndPoint3d, Matrix3d _ucsToWcsMatrix3d, Polyline _polyline)
        {
            List<Point3d> list = new List<Point3d>();
            Point3d cornerPoint1 = new Point3d(_wcsEndPoint3d.X, _basePoint3d.Y, 0);
            Point3d cornerPoint2 = new Point3d(_basePoint3d.X, _wcsEndPoint3d.Y, 0);
            int row = LightingRecLayoutViewModel.Instance.LightingRow;
            double distance = LightingLayoutSettingViewModel.Instance.Distance;
            int column = LightingRecLayoutViewModel.Instance.LightingColumn;

            //竖直
            if (cornerPoint1.IsEqualTo(_basePoint3d))
            {
                PolyLineUpdate(_polyline, _basePoint3d, cornerPoint1, _wcsEndPoint3d, cornerPoint2, _ucsToWcsMatrix3d);
                list = GetLinePoint(_basePoint3d, _wcsEndPoint3d, row, distance, _ucsToWcsMatrix3d);
            }
            //水平
            else if (cornerPoint1.IsEqualTo(_wcsEndPoint3d))
            {
                PolyLineUpdate(_polyline, _basePoint3d, cornerPoint1, _wcsEndPoint3d, cornerPoint2, _ucsToWcsMatrix3d);
                list = GetLinePoint(_basePoint3d, _wcsEndPoint3d, column, distance, _ucsToWcsMatrix3d);
            }
            else if ((((column - 1) + (2.0 * distance)) > 0.0) && (((row - 1) + (2.0 * distance)) > 0.0))
            {
                PolyLineUpdate(_polyline, _basePoint3d, cornerPoint1, _wcsEndPoint3d, cornerPoint2, _ucsToWcsMatrix3d);
                Line3d lined = new Line3d(_basePoint3d, cornerPoint1);
                Line3d lined2 = new Line3d(_basePoint3d, cornerPoint2);
                double num4 = 1.0 / ((column - 1) + (2.0 * distance));
                double num5 = 1.0 / ((row - 1) + (2.0 * distance));
                int num6 = 0;
                while (true)
                {
                    if (num6 >= column)
                    {
                        break;
                    }
                    int num7 = 0;
                    while (true)
                    {
                        if (num7 >= row)
                        {
                            num6++;
                            break;
                        }
                        Point3d pointd3 = lined.EvaluatePoint((distance + num6) * num4);
                        Point3d pointd4 = lined2.EvaluatePoint((distance + num7) * num5);
                        Point3d pointd5 = new Point3d(pointd3.X, pointd4.Y, _basePoint3d.Z);
                        list.Add(pointd5.TransformBy(_ucsToWcsMatrix3d));
                        num7++;
                    }
                }
            }
            return list;
        }

        private List<Point3d> GetLinePoint(Point3d p1, Point3d p3, int n, double d, Matrix3d matrix3d)
        {
            List<Point3d> list = new List<Point3d>();
            Line3d lined = new Line3d(p1, p3);
            if (((n - 1) + (2.0 * d)) > 0.0)
            {
                double num2 = 1.0 / ((n - 1) + (2.0 * d));
                int num3 = 0;
                while (true)
                {
                    if (num3 >= n)
                    {
                        break;
                    }
                    Point3d pointd = lined.EvaluatePoint((d + num3) * num2);
                    list.Add(pointd.TransformBy(matrix3d));
                    num3++;
                }
            }
            return list;
        }

        private void PolyLineUpdate(Polyline polyline, Point3d p1, Point3d p2, Point3d p3, Point3d p4, Matrix3d matrix3d)
        {

            polyline.Normal = Vector3d.ZAxis;
            polyline.Elevation = 0;
            polyline.SetPointAt(0, new Point2d(p1.X, p1.Y));
            polyline.SetPointAt(1, new Point2d(p2.X, p2.Y));
            polyline.SetPointAt(2, new Point2d(p3.X, p3.Y));
            polyline.SetPointAt(3, new Point2d(p4.X, p4.Y));
            polyline.TransformBy(matrix3d);
        }
    }
}
