using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        //转换块属性值为单行文字

        [CommandMethod("F10_TagToDbtext")]
        public void F10_TagToDbtext()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Prompt the user to select block references with attributes
                PromptSelectionOptions opts = new PromptSelectionOptions();
                opts.MessageForAdding = "选择带有属性的块: ";
                TypedValue[] filterList = new TypedValue[]
                {
            new TypedValue((int)DxfCode.Start, "INSERT")
                };
                SelectionFilter filter = new SelectionFilter(filterList);
                PromptSelectionResult selRes = ed.GetSelection(opts, filter);

                if (selRes.Status != PromptStatus.OK)
                {
                    return;
                }

                SelectionSet selSet = selRes.Value;

                foreach (SelectedObject selObj in selSet)
                {
                    if (selObj != null)
                    {
                        BlockReference blkRef = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as BlockReference;
                        if (blkRef == null) continue;

                        AttributeCollection attCol = blkRef.AttributeCollection;

                        foreach (ObjectId attId in attCol)
                        {
                            AttributeReference attRef = tr.GetObject(attId, OpenMode.ForRead) as AttributeReference;
                            if (attRef == null) continue;

                            // Create a new DBText object with the attribute's value
                            DBText dBText = new DBText
                            {
                                TextString = attRef.TextString,
                                Position = attRef.Position,
                                Height = attRef.Height,
                                Rotation = blkRef.Rotation, // Use block rotation for text
                                Layer = attRef.Layer,
                                Color = attRef.Color,
                                Oblique = attRef.Oblique,
                                WidthFactor = attRef.WidthFactor,
                                TextStyleId = attRef.TextStyleId
                            };
                            DBText newText = dBText;

                            // Set the alignment point and alignment settings
                            newText.HorizontalMode = attRef.HorizontalMode;
                            newText.VerticalMode = attRef.VerticalMode;
                            newText.AlignmentPoint = attRef.AlignmentPoint;

                            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                            btr.AppendEntity(newText);
                            tr.AddNewlyCreatedDBObject(newText, true);
                        }
                    }
                }

                tr.Commit();
            }
        }
    }
}
