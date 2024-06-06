using System;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.GraphicsSystem;

namespace TimeIsLife.Jig
{
    internal class QuickUcsJig : DrawJig
    {
        private Point3d currentPoint;
        private Entity highlightEntity;
        public QuickUcsJig()
        {
            currentPoint = Point3d.Origin;
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            JigPromptPointOptions jigOpts = new JigPromptPointOptions
            {
                Cursor = CursorType.EntitySelect
            };

            PromptPointResult result = prompts.AcquirePoint(jigOpts);

            if (result.Status != PromptStatus.OK)
            {
                return SamplerStatus.Cancel;
            }

            Point3d newPoint = result.Value;

            if (newPoint.DistanceTo(currentPoint) < Tolerance.Global.EqualPoint)
            {
                return SamplerStatus.NoChange;
            }

            currentPoint = newPoint;
            return SamplerStatus.OK;
        }

        protected override bool WorldDraw(WorldDraw draw)
        {
            if (highlightEntity != null)
            {
                highlightEntity.Dispose();
            }

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            Point3dCollection pickBoxPoints = GetPickBoxPoints(currentPoint);
            PromptSelectionResult selectionResult = ed.SelectCrossingWindow(pickBoxPoints[0], pickBoxPoints[2]);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                DBObject topLevelObject = GetTopLevelObject(selectionResult, tr);

                if (topLevelObject is Entity entity)
                {
                    highlightEntity = entity.Clone() as Entity;
                    highlightEntity.Highlight();
                }

                tr.Commit();
            }

            return true;
        }

        private DBObject GetTopLevelObject(PromptSelectionResult selectionResult, Transaction tr)
        {
            double maxZ = double.MinValue;
            DBObject topLevelObject = null;

            foreach (SelectedObject selObj in selectionResult.Value)
            {
                if (selObj != null)
                {
                    DBObject obj = tr.GetObject(selObj.ObjectId, OpenMode.ForRead);
                    if (obj is Entity ent && ent.Bounds.HasValue)
                    {
                        double z = ent.Bounds.Value.MaxPoint.Z;
                        if (z > maxZ)
                        {
                            maxZ = z;
                            topLevelObject = obj;
                        }
                    }
                }
            }

            return topLevelObject;
        }

        public void Highlight(Entity entity)
        {
            if (highlightEntity != null)
            {
                highlightEntity.Dispose();
            }

            highlightEntity = entity.Clone() as Entity;
            highlightEntity.Highlight();
        }

        public void RemoveHighlight()
        {
            if (highlightEntity != null)
            {
                highlightEntity.Unhighlight();
                highlightEntity.Dispose();
                highlightEntity = null;
            }
        }

        private Point3dCollection GetPickBoxPoints(Point3d pickedPoint)
        {
            int pickBoxSize = Convert.ToInt32(Application.GetSystemVariable("PICKBOX"));
            ViewTableRecord currentView = Application.DocumentManager.MdiActiveDocument.Editor.GetCurrentView();
            double viewWidth = currentView.Width;
            double viewHeight = currentView.Height;

            Manager manager = Application.DocumentManager.MdiActiveDocument.GraphicsManager;
            double pixelWidth = manager.DeviceIndependentDisplaySize.Width;
            double pixelHeight = manager.DeviceIndependentDisplaySize.Height;

            double lengthPerPixelX = viewWidth / pixelWidth * pickBoxSize * 2;
            double lengthPerPixelY = viewHeight / pixelHeight * pickBoxSize * 2;

            Point3d pickBoxPoint1 = new Point3d(pickedPoint.X - lengthPerPixelX / 2, pickedPoint.Y - lengthPerPixelY / 2, 0);
            Point3d pickBoxPoint2 = new Point3d(pickedPoint.X + lengthPerPixelX / 2, pickedPoint.Y - lengthPerPixelY / 2, 0);
            Point3d pickBoxPoint3 = new Point3d(pickedPoint.X + lengthPerPixelX / 2, pickedPoint.Y + lengthPerPixelY / 2, 0);
            Point3d pickBoxPoint4 = new Point3d(pickedPoint.X - lengthPerPixelX / 2, pickedPoint.Y + lengthPerPixelY / 2, 0);

            return new Point3dCollection(new Point3d[] { pickBoxPoint1, pickBoxPoint2, pickBoxPoint3, pickBoxPoint4 });
        }
    }
}

