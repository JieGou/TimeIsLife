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
        public Point3d _point;
        //PrecisionModel precisionModel;
        //GeometryPrecisionReducer precisionReducer;
        //GeometryFactory geometryFactory;
        private List<Polyline> polylines;
        public BasePointJig(List<Polyline> polylines)
        {
            this.polylines = polylines;
            //this.precisionModel = new PrecisionModel(1000d);
            //this.precisionReducer = new GeometryPrecisionReducer(precisionModel);
            //NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices
            //    (
            //    NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
            //    precisionModel,
            //    4326
            //    );
            //this.geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(precisionModel);
        }

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
                //Coordinate coordinate = new Coordinate(_point.X, _point.Y);
                //Point point = geometryFactory.CreatePoint(coordinate);

                //if (point.Within(geometry))
                //{
                //    gridPoints.Add(new double[2] { x, y });
                //}
                return SamplerStatus.OK;
            }
            else
            {
                return SamplerStatus.NoChange;
            }
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            Document document = Application.DocumentManager.CurrentDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d matrix = editor.CurrentUserCoordinateSystem;

            //Point3d ucsPoint3d = _point.TransformBy(matrix.Inverse());
            Vector3d vector3D = Point3d.Origin.TransformBy(matrix).GetVectorTo(_point);
            Matrix3d matrix3D = Matrix3d.Displacement(vector3D);
            foreach (var polyline in polylines)
            {
                polyline.TransformBy(matrix);
                polyline.TransformBy(matrix3D);
                draw.Geometry.Draw(polyline);
                polyline.TransformBy(matrix3D.Inverse());
                polyline.TransformBy(matrix.Inverse());
            }
            return true;
        }
    }
}
