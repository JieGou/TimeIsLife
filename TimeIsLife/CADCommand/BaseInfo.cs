using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: CommandClass(typeof(TimeIsLife.CADCommand.TilCommand))]

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        private Document document;
        private Database database;
        private Editor editor;
        private Matrix3d wcsToUcsMatrix3d;
        void Initialize()
        {
            document = Application.DocumentManager.MdiActiveDocument;
            database = document.Database;
            editor = document.Editor;
            wcsToUcsMatrix3d = editor.CurrentUserCoordinateSystem;
        }
    }
}
