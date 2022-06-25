using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySqlWebManager.Dtos
{
    /// <summary>
    /// MySQL的information_schema库中有个COLUMNS表，里面记录了mysql所有库中所有表的字段信息
    /// </summary>
    public class TableField
    {
        
        /// <summary>
        /// COLUMN_NAME 字段名
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// DATA_TYPE 数据类型  里面的值是字符串，比如varchar，float，int
        /// </summary>
        public string DATA_TYPE { get; set; }

        /// <summary>
        /// COLUMN_TYPE 字段类型。比如float(9,3)，varchar(50)
        /// </summary> 
        public string COLUMN_TYPE { get; set; }

        /// <summary>
        /// IS_NULLABLE 字段是否可以是NULL 该列记录的值是YES或者NO。
        /// </summary>
        public string IS_NULL { get; set; }

        /// <summary>
        /// COLUMN_COMMENT 字段注释
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// EXTRA  其他信息  比如主键的auto_increment
        /// </summary>
        public string Auto { get; set; }

        /// <summary>
        /// CHARACTER_MAXIMUM_LENGTH 字段的最大字符数
        /// 假如字段设置为varchar(50)，那么这一列记录的值就是50。
        /// 该列只适用于二进制数据，字符，文本，图像数据。其他类型数据比如int，float，datetime等，在该列显示为NULL
        /// </summary>
        public string MaxLen { get; set; }
    }
}
