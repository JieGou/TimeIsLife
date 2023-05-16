// (C) Copyright 2022 by  
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

using NetTopologySuite.Geometries;
using NetTopologySuite.Precision;
using NetTopologySuite;

using System;

// 该行不是必需的，但是可以提高加载性能
[assembly: CommandClass(typeof(TimeIsLife.MyCommands))]

namespace TimeIsLife
{
    //当用户在给定文档的上下文中首次调用命令时，AutoCAD将为每个文档实例化此类。 换句话说，此类中的非静态数据是每个文档隐式的！
    public class MyCommands
    {
        // CommandMethod属性可以应用于任何公共类的任何公共成员函数。 该函数应不带参数，不返回任何值。
        // 如果该方法是事例成员，则为每个文档实例化封闭类。 如果成员是静态成员，则封闭类不会被实例化。

        // 注意：CommandMethod具有重载，您可以在其中提供帮助ID和上下文菜单。

        // 具有本地化名称的模态命令
        [CommandMethod("MyGroup", "MyCommand", "MyCommandLocal", CommandFlags.Modal)]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here

            #region 样板
            Initialize();
            GeometryFactory geometryFactory = CreateGeometryFactory();
            #endregion

            if (document != null)
            {
                editor = document.Editor;
                editor.WriteMessage("Hello, this is your first command.");
            }
        }

        // 选择优先的模态命令
        [CommandMethod("MyGroup", "MyPickFirst", "MyPickFirstLocal", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void MyPickFirst() // This method can have any name
        {
            PromptSelectionResult result = Application.DocumentManager.MdiActiveDocument.Editor.GetSelection();
            if (result.Status == PromptStatus.OK)
            {
                // There are selected entities
                // Put your command using pickfirst set code here
            }
            else
            {
                // There are no selected entities
                // Put your command code here
            }
        }

        // 具有本地化名称的应用程序会话命令
        // 该命令将在应用程序上下文中执行而不是当前文档上下文中运行，具有不同的功能和限制。 应该谨慎使用。
        [CommandMethod("MyGroup", "MySessionCmd", "MySessionCmdLocal", CommandFlags.Modal | CommandFlags.Session)]
        public void MySessionCmd() // This method can have any name
        {
            // Put your command code here
        }

        // LispFunction与CommandMethod相似，但是它创建了一个Lisp可调用函数。 支持许多返回类型，而不仅仅是字符串或整数。
        [LispFunction("MyLispFunction", "MyLispFunctionLocal")]
        public int MyLispFunction(ResultBuffer args) // This method can have any name
        {
            // Put your command code here

            // Return a value to the AutoCAD Lisp Interpreter
            return 1;
        }


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

        /// <summary>
        /// 获取NTS指定精度和标准坐标系的GeometryFactory实例
        /// </summary>
        /// <returns>GeometryFactory实例</returns>
        private GeometryFactory CreateGeometryFactory()
        {
            //NTS
            var precisionModel = new PrecisionModel(1000d);
            GeometryPrecisionReducer precisionReducer = new GeometryPrecisionReducer(precisionModel);
            NetTopologySuite.NtsGeometryServices.Instance = new NetTopologySuite.NtsGeometryServices
                (
                NetTopologySuite.Geometries.Implementation.CoordinateArraySequenceFactory.Instance,
                precisionModel,
                4326
                );
            return NtsGeometryServices.Instance.CreateGeometryFactory(precisionModel);
        }
    }

}
