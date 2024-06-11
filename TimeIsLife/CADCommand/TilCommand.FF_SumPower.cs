using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TimeIsLife.ViewModel;

namespace TimeIsLife.CADCommand
{
    internal partial class TilCommand
    {
        //求和功率，选择多个表示功率的单行文字，文字内容由数字+“kw”组成，去除“kw”后的数字求和

        [CommandMethod("FF_SumPower")]
        public void FF_SumPower()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            Database database = document.Database;
            Editor editor = document.Editor;
            Matrix3d ucsToWcsMatrix3d = editor.CurrentUserCoordinateSystem;

            using (Transaction transaction = document.TransactionManager.StartTransaction())
            {
                PromptSelectionOptions options = new PromptSelectionOptions { MessageForAdding = "\n请选择要统计的功率:" };
                TypedValue[] values = { new TypedValue((int)DxfCode.Start, "TEXT") };
                PromptSelectionResult result = editor.GetSelection(options, new SelectionFilter(values));

                if (result.Status == PromptStatus.OK)
                {
                    SelectionSet set = result.Value;
                    List<double[]> powerValues = new List<double[]>();

                    foreach (ObjectId objectId in set.GetObjectIds())
                    {
                        DBText dbText = (DBText)transaction.GetObject(objectId, OpenMode.ForRead);
                        string text = dbText.TextString;
                        powerValues.Add(GetPowerValue(text));
                    }

                    double totalNormalOrFirePower = powerValues.Sum(pv => pv[0]);
                    double totalNormalInFirePower = powerValues.Sum(pv => pv[1]);

                    CalculateCurrentViewModel.Instance.NormalOrFirePower = totalNormalOrFirePower;
                    CalculateCurrentViewModel.Instance.NormalInFirePower = totalNormalInFirePower;
                    CalculateCurrentViewModel.Instance.Pe = Math.Max(totalNormalOrFirePower, totalNormalInFirePower);

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// 获取字符串中的数字
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns>数字</returns>
        public double[] GetPowerValue(string str)
        {
            double[] arr = new double[] { 0, 0 };
            if (!string.IsNullOrEmpty(str))
            {
                MatchCollection matchCollection = Regex.Matches(str, @"\d+\.?\d*");
                for (int i = 0; i < matchCollection.Count && i < 2; i++)
                {
                    arr[i] = double.Parse(matchCollection[i].Value);
                }
            }
            return arr;
        }
    }
}
