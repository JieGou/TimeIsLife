using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Media3D;

namespace TimeIsLife.Helper
{
    public static class EditorHelper
    {
        public static SelectionSet GetSelectionSet(this Editor editor,SelectString selectString, 
            PromptSelectionOptions promptSelectionOptions, SelectionFilter selectionFilter, Point3dCollection point3dCollection)
        {
            // 请求在图形区域选择对象
            PromptSelectionResult psr = null;
            // 提示用户从图形文件中选取对象
            if (selectString == SelectString.GetSelection)  
            {
                if (promptSelectionOptions == null && selectionFilter == null)
                {
                    psr = editor.GetSelection();
                }
                else if (promptSelectionOptions == null && selectionFilter != null)
                {
                    psr = editor.GetSelection(selectionFilter);
                }
                else if (promptSelectionOptions != null && selectionFilter == null)
                {
                    psr = editor.GetSelection(promptSelectionOptions);
                }
                else if (promptSelectionOptions != null && selectionFilter != null)
                {
                    psr = editor.GetSelection(promptSelectionOptions, selectionFilter);
                }
            }
            //选择当前空间内所有未锁定及未冻结的对象
            else if (selectString == SelectString.SelectAll) 
            {
                if (selectionFilter == null)
                {
                    psr = editor.SelectAll();
                }
                else
                {
                    psr = editor.SelectAll(selectionFilter);
                }
            }
            //选择由给定点定义的多边形内的所有对象以及与多边形相交的对象。多边形可以是任意形状，但不能与自己交叉或接触。
            else if (selectString == SelectString.SelectCrossingPolygon) 
            {
                if (selectionFilter == null)
                {
                    psr = editor.SelectCrossingPolygon(point3dCollection) ;
                }
                else
                {
                    psr = editor.SelectCrossingPolygon(point3dCollection, selectionFilter);
                }
            }
            // 选择与选择围栏相交的所有对象。围栏选择与多边形选择类似，所不同的是围栏不是封闭的， 围栏同样不能与自己相交
            else if (selectString == SelectString.SelectFence)
            {
                if (selectionFilter == null)
                {
                    psr = editor.SelectFence(point3dCollection);
                }
                else
                {
                    psr = editor.SelectFence(point3dCollection, selectionFilter);
                }
            }
            // 选择完全框入由点定义的多边形内的对象。多边形可以是任意形状，但不能与自己交叉或接触
            else if (selectString == SelectString.SelectWindowPolygon)
            {
                if (selectionFilter == null)
                {
                    psr = editor.SelectWindowPolygon(point3dCollection);
                }
                else
                {
                    psr = editor.SelectWindowPolygon(point3dCollection, selectionFilter);
                }
            }
            //选择由两个点定义的窗口内的对象以及与窗口相交的对象
            else if (selectString == SelectString.SelectCrossingWindow)  
            {
                if (selectionFilter == null)
                {
                    psr = editor.SelectCrossingWindow(point3dCollection[0], point3dCollection[1]);
                }
                else
                {
                    psr = editor.SelectCrossingWindow(point3dCollection[0], point3dCollection[1], selectionFilter);
                }
            }
            // 选择完全框入由两个点定义的矩形内的所有对象。
            else if (selectString == SelectString.SelectWindow) 
            {
                if (selectionFilter == null)
                {
                    psr = editor.SelectWindow(point3dCollection[0], point3dCollection[1]);
                }
                else
                {
                    psr = editor.SelectWindow(point3dCollection[0], point3dCollection[1], selectionFilter);
                }
            }
            else
            {
                return null;
            }

            // 如果提示状态OK，表示对象已选
            if (psr.Status == PromptStatus.OK)
            {
                SelectionSet sSet = psr.Value;
                // 打印选择对象数量
                //editor.WriteMessage("Number of objects selected: " + sSet.Count.ToString() + "\n");
                return sSet;
            }
            else
            {
                // 打印选择对象数量
                editor.WriteMessage("Number of objects selected 0 \n");
                return null;
            }
        }
    }
}
