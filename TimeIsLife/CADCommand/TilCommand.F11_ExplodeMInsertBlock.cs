using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
		[CommandMethod("F11_ExplodeMInsertBlock")]
		public void F11_ExplodeMInsertBlock()
		{
			Document document = Application.DocumentManager.MdiActiveDocument;
			Database database = document.Database;
			Editor editor = document.Editor;
			Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;

			using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
			try
			{

				PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions()
				{
					SingleOnly = true,
					RejectObjectsOnLockedLayers = true,
				};

				TypedValueList typedValues = new TypedValueList();
				typedValues.Add(typeof(BlockReference));
				SelectionFilter selectionFilter = new SelectionFilter(typedValues);
				PromptSelectionResult promptSelectionResult = editor.GetSelection(promptSelectionOptions, selectionFilter);

				if (promptSelectionResult.Status == PromptStatus.OK)
				{
					SelectionSet selectionSet = promptSelectionResult.Value;
					foreach (var id in selectionSet.GetObjectIds())
					{
						MInsertBlock mInsertBlock = transaction.GetObject(id, OpenMode.ForWrite) as MInsertBlock;
						if (mInsertBlock == null) continue;
						mInsertBlock.ExplodeToOwnerSpace();
						mInsertBlock.Erase();
					}
				}

				transaction.Commit();
			}
			catch
			{
			}

		}
	}
}
