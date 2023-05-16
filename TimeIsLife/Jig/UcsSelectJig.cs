using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace TimeIsLife.Jig
{
    class UcsSelectJig : DrawJig
    {
        Point3d _startPoint3d;
        public Point3d endPoint3d;
        Polyline _polyline;
        public UcsSelectJig(Point3d startPoint3d, Polyline polyline)
        {
            this._startPoint3d = startPoint3d;
            this._polyline = polyline;
        }
        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Editor editor = document.Editor;

            Matrix3d matrixd = editor.CurrentUserCoordinateSystem;
            JigPromptPointOptions options = new JigPromptPointOptions("\n请选择第二个角点：")
            {
                Cursor = CursorType.Crosshair,
                UserInputControls = UserInputControls.NoZeroResponseAccepted |
                                    UserInputControls.Accept3dCoordinates |
                                    UserInputControls.NoNegativeResponseAccepted,
                UseBasePoint = true
            };
            PromptPointResult result = prompts.AcquirePoint(options);
            if (result.Status == PromptStatus.Cancel)
            {
                return SamplerStatus.Cancel;
            }
            Point3d tempPoint3d = result.Value;
            if (tempPoint3d != endPoint3d)
            {
                endPoint3d = tempPoint3d;
                //将WCS点转化为UCS点
                Point3d uscEndPoint3d = endPoint3d.TransformBy(matrixd.Inverse());
                _polyline.Normal = Vector3d.ZAxis;
                _polyline.Elevation = 0.0;
                _polyline.SetPointAt(0, new Point2d(_startPoint3d.X, _startPoint3d.Y));
                _polyline.SetPointAt(1, new Point2d(_startPoint3d.X, uscEndPoint3d.Y));
                _polyline.SetPointAt(2, new Point2d(uscEndPoint3d.X, uscEndPoint3d.Y));
                _polyline.SetPointAt(3, new Point2d(uscEndPoint3d.X, _startPoint3d.Y));
                _polyline.TransformBy(matrixd);
                return SamplerStatus.OK;
            }
            else
            {
                return SamplerStatus.NoChange;
            }
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            draw.Geometry.Draw(_polyline);
            return true;
        }
    }
}
