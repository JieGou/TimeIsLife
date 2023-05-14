using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;

using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

using static TimeIsLife.CADCommand.FireAlarmCommand1;

using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace TimeIsLife.Jig
{
    internal class BasePointJig : DrawJig
    {
        private Document document;
        private Database database;
        private Editor editor;
        private Matrix3d ucsToWcsMatrix3d;

        void Initialize()
        {
            document = Application.DocumentManager.CurrentDocument;
            database = document.Database;
            editor = document.Editor;
            ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;
        }

        public BasePointJig(List<Polyline> polylines)
        {
            Initialize();
            this.polylines = polylines;
        }

        public Point3d _point;
        private List<Polyline> polylines;

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions promptOptions = new JigPromptPointOptions("\n选择对齐基点:");
            promptOptions.UserInputControls = UserInputControls.Accept3dCoordinates | UserInputControls.NoZeroResponseAccepted | UserInputControls.NoNegativeResponseAccepted;
            promptOptions.Cursor = CursorType.Crosshair;

            PromptPointResult result = prompts.AcquirePoint(promptOptions);
            Point3d tempPoint = result.Value;

            if (result.Status == PromptStatus.Cancel)
            {
                return SamplerStatus.Cancel;
            }

            if (_point != tempPoint)
            {
                _point = tempPoint;
                return SamplerStatus.OK;
            }
            else
            {
                return SamplerStatus.NoChange;
            }
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            Vector3d vector3D = Point3d.Origin.TransformBy(ucsToWcsMatrix3d).GetVectorTo(_point);
            Matrix3d matrix3D = Matrix3d.Displacement(vector3D);
            foreach (var polyline in polylines)
            {
                polyline.TransformBy(ucsToWcsMatrix3d);
                polyline.TransformBy(matrix3D);
                draw.Geometry.Draw(polyline);
                polyline.TransformBy(matrix3D.Inverse());
                polyline.TransformBy(ucsToWcsMatrix3d.Inverse());
            }
            return true;
        }
    }
}
