using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Tools
{
    public static class BlockTools
    {
        public static void InsertBlockReference(this BlockReference baseBlockReference, string layer, Point3d point3d, Scale3d scale3D, double rotateAngle, Dictionary<string, string> attNameValues)
        {
            Database database = baseBlockReference.Database;
            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
            BlockTable blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
            BlockTableRecord modelSpace = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            BlockTableRecord blockTableRecord = transaction.GetObject(baseBlockReference.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
            BlockReference newBlockReference = new BlockReference(point3d, baseBlockReference.BlockTableRecord)
            {
                Layer = layer,
                ScaleFactors = scale3D,
                Rotation = rotateAngle
            };

            modelSpace.AppendEntity(newBlockReference);
            transaction.AddNewlyCreatedDBObject(newBlockReference, true);

            if (blockTableRecord.HasAttributeDefinitions)
            {
                foreach (var id in blockTableRecord)
                {
                    AttributeDefinition attributeDefinition = transaction.GetObject(id, OpenMode.ForRead) as AttributeDefinition;
                    if (attributeDefinition != null)
                    {
                        //创建一个新的属性对象
                        AttributeReference attrbuteReference = new AttributeReference();
                        //从属性定义获得属性对象的对象特性
                        attrbuteReference.SetAttributeFromBlock(attributeDefinition, newBlockReference.BlockTransform);
                        //设置属性对象的其它特性
                        attrbuteReference.Position = attributeDefinition.Position.TransformBy(newBlockReference.BlockTransform);
                        attrbuteReference.Rotation = rotateAngle;
                        attrbuteReference.AdjustAlignment(database);
                        //判断是否包含指定的属性名称
                        if (attNameValues.ContainsKey(attributeDefinition.Tag.ToUpper()))
                        {
                            //设置属性值
                            attrbuteReference.TextString = attNameValues[attributeDefinition.Tag.ToUpper()].ToString();
                        }
                        //向块参照添加属性对象
                        newBlockReference.AttributeCollection.AppendAttribute(attrbuteReference);
                        transaction.AddNewlyCreatedDBObject(attrbuteReference, true);
                    }
                }
            }

            transaction.Commit();
        }
    }
}
