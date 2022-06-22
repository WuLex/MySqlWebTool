using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySqlWebManager.Dtos
{
    public class TableField
    {

        /// <summary>
        /// column_name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// data_type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// COLUMN_TYPE
        /// </summary>
        public string COLUMN_TYPE { get; set; }

        /// <summary>
        /// column_comment
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// extra
        /// </summary>
        public string Auto { get; set; }

        /// <summary>
        /// CHARACTER_MAXIMUM_LENGTH
        /// </summary>
        public string Len { get; set; }
    }
}
