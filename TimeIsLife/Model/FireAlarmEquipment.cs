using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.Model
{
    public class FireAlarmEquipment
    {
        public FireAlarmEquipmentType EquipmentType { get; set; }
        // 设备块名称
        public string BlockName { get; set; }

        // 块路径（例如，在CAD图纸中的路径）
        public string BlockPath { get; set; }

        // 系统图块路径
        public string SchematicBlockPath { get; set; }

        // 是否在系统图上表示
        public bool IsRepresentedOnSchematic { get; set; }

        // 在系统图上位置是否固定
        public bool IsPositionFixedOnSchematic { get; set; }

        // 系统图上占位宽度
        public double SchematicWidth { get; set; }

        // 构造函数
        public FireAlarmEquipment(FireAlarmEquipmentType fireAlarmEquipmentType ,string blockName,string blockPath,string schematicBlockPath,bool isRepresentedOnSchematic,bool isPositionFixedOnSchematic,double schematicWidth)
        {
            EquipmentType = fireAlarmEquipmentType;
            BlockName = blockName;
            BlockPath = blockPath;
            SchematicBlockPath = schematicBlockPath;
            IsRepresentedOnSchematic = isRepresentedOnSchematic;
            IsPositionFixedOnSchematic = isPositionFixedOnSchematic;
            SchematicWidth = schematicWidth;
        }
    }

    public enum FireAlarmEquipmentType
    {
        Fa01,
        Fa02,
        Fa03,
        Fa04,
        Fa05,
        Fa06,
        Fa07,
        Fa08,
        Fa09,
        Fa10,
        Fa11,
        Fa12,
        Fa13,
        Fa14,
        Fa15,
        Fa16,
        Fa17,
        Fa18,
        Fa19,
        Fa20,
        Fa21,
        Fa22,
        Fa23,
        Fa24,
        Fa25,
        Fa26,
        Fa27,
        Fa28,
        Fa29,
        Fa30,
        Fa31,
        Fa32,
        Fa33,
        Fa34,
        Fa35,
        Fa36,
        Fa37,
        Fa38,
        Fa39,
        Fa40,
        Fa41,
        Fa42,
        Fa43,
        Fa44,
        Fa45,
        Fa46,
        Fa47,
        Fa48,
        Fa49,
        Fa50,
        Fa51,
        Fa52,
        Fa53,
        Fa54,
        Fa55,
        Fa56,
        Fa57,
        Fa58,
        Fa59,
        Fa60,
        Fa61,
        Fa62,
        Fa63,
        Fa64,
        Fa65,
        Fa66,
        Fa67,
        Fa68,
        Fa69,
        Fa70,
        Fa71,
        Fa72,
        Fa73,
        Fa74,
        Fa75,
        Fa76
    }

}
