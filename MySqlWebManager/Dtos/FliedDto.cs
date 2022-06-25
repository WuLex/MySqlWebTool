namespace MySqlWebManager.Dtos
{
    public class FliedDto
    {
        /// <summary>
        /// COLUMN_NAME
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// DATA_TYPE 
        /// </summary>
        public string DATA_TYPE { get; set; }

        /// <summary>
        /// COLUMN_TYPE
        /// </summary> 
        public string COLUMN_TYPE { get; set; }

        /// <summary>
        /// IS_NULLABLE
        /// </summary>
        public string IS_NULL { get; set; }

        /// <summary>
        /// COLUMN_COMMENT
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// EXTRA
        /// </summary>
        public string Auto { get; set; }

        /// <summary>
        /// CHARACTER_MAXIMUM_LENGTH
        /// </summary>
        public string MaxLen { get; set; }
    }
}