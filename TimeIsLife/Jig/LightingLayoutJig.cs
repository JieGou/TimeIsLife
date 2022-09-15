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

namespace TimeIsLife.Jig
{
    internal class LightingLayoutJig : DrawJig
    {
        public List<BlockReference> blockReferences;
        private BlockReference blockReference;
        private Point3d basePoint3d;
        private Point3d endPoint3d;
        private ElectricalViewModel viewModel;
        private Polyline polyline;
        private BlockTableRecord btr;
        public LightingLayoutJig(BlockReference blockReference, Point3d basePoint3d, Polyline polyline)
        {
            this.blockReference = blockReference;
            this.basePoint3d = basePoint3d;
            this.viewModel = ElectricalViewModel.electricalViewModel;
            blockReferences = new List<BlockReference>();
            this.polyline = polyline;
            this.btr = (BlockTableRecord)blockReference.BlockTableRecord.GetObject(OpenMode.ForRead);
        }

        private List<Point3d> GetLayoutPoint(Point3d p1, Point3d p3, ElectricalViewModel viewModel, Matrix3d matrix3d, Polyline polyline)
        {
            List<Point3d> list = new List<Point3d>();
            Point3d pointd = new Point3d(p3.X, p1.Y, p1.Z);
            Point3d pointd2 = new Point3d(p1.X, p3.Y, p1.Z);
            int rowCount = viewModel.LightingRow;
            double distance = viewModel.Distance;
            int columnCount = viewModel.LightingColumn;
            if (pointd.IsEqualTo(p1))
            {
                PolyLineUpdate(polyline, p1, pointd, p3, pointd2, matrix3d);
                list = GetLinePoint(p1, p3, rowCount, distance, matrix3d);
            }
            else if (pointd.IsEqualTo(p3))
            {
                PolyLineUpdate(polyline, p1, pointd, p3, pointd2, matrix3d);
                list = GetLinePoint(p1, p3, columnCount, distance, matrix3d);
            }
            else if ((((columnCount - 1) + (2.0 * distance)) > 0.0) && (((rowCount - 1) + (2.0 * distance)) > 0.0))
            {
                PolyLineUpdate(polyline, p1, pointd, p3, pointd2, matrix3d);
                Line3d lined = new Line3d(p1, pointd);
                Line3d lined2 = new Line3d(p1, pointd2);
                double num4 = 1.0 / ((columnCount - 1) + (2.0 * distance));
                double num5 = 1.0 / ((rowCount - 1) + (2.0 * distance));
                int num6 = 0;
                while (true)
                {
                    if (num6 >= columnCount)
                    {
                        break;
                    }
                    int num7 = 0;
                    while (true)
                    {
                        if (num7 >= rowCount)
                        {
                            num6++;
                            break;
                        }
                        Point3d pointd3 = lined.EvaluatePoint((distance + num6) * num4);
                        Point3d pointd4 = lined2.EvaluatePoint((distance + num7) * num5);
                        Point3d pointd5 = new Point3d(pointd3.X, pointd4.Y, p1.Z);
                        list.Add(pointd5.TransformBy(matrix3d));
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

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            try
            {
                Matrix3d matrixd = Application.DocumentManager.MdiActiveDocument.Editor.CurrentUserCoordinateSystem;
                JigPromptPointOptions options1 = new JigPromptPointOptions("\n请选择第二个角点：")
                {
                    Cursor = CursorType.Crosshair,
                    UserInputControls = UserInputControls.NoZeroResponseAccepted |
                                        UserInputControls.Accept3dCoordinates |
                                        UserInputControls.NoNegativeResponseAccepted,
                    BasePoint = basePoint3d.TransformBy(matrixd),
                    UseBasePoint = true
                };
                JigPromptPointOptions options = options1;
                PromptPointResult result = prompts.AcquirePoint(options);
                Point3d pointd = result.Value;
                if (result.Status != PromptStatus.Cancel)
                {
                    if (endPoint3d == pointd)
                    {
                        return SamplerStatus.NoChange;
                    }
                    else
                    {
                        blockReferences.Clear();
                        int num = 0;
                        while (true)
                        {
                            if (num >= (viewModel.LightingRow * viewModel.LightingColumn))
                            {
                                endPoint3d = pointd;
                                Point3d pointd2 = endPoint3d.TransformBy(matrixd.Inverse());
                                List<Point3d> list = GetLayoutPoint(basePoint3d, pointd2, viewModel, matrixd, polyline);
                                if (list.Count == 0)
                                {
                                    int num2 = 0;
                                    while (true)
                                    {
                                        if (num2 >= blockReferences.Count)
                                        {
                                            break;
                                        }

                                        blockReferences[num2].Position = basePoint3d.TransformBy(matrixd);
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
                            item.TransformBy(matrixd);
                            Matrix3d matrixd2 = Matrix3d.Rotation((viewModel.BlockAngle * Math.PI) / 180.0, Vector3d.ZAxis, item.Position);
                            item.TransformBy(matrixd2);
                            item.ScaleFactors = new Scale3d(viewModel.BlockScale);

                            blockReferences.Add(item);
                            num++;
                        }
                    }

                    foreach (var reference in blockReferences)
                    {
                        if (btr.HasAttributeDefinitions)
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
                                    ar.TransformBy(Matrix3d.Rotation(blockReference.Rotation, Vector3d.ZAxis, point3D.TransformBy(matrixd)).Inverse());
                                    ar.TransformBy(Matrix3d.Displacement(point3D.TransformBy(matrixd).GetVectorTo(reference.Position)));
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
            draw.Geometry.Draw(polyline);
            for (int i = 0; i < blockReferences.Count; i++)
            {
                draw.Geometry.Draw(blockReferences[i]);
            }
            return true;
        }
    }
}
