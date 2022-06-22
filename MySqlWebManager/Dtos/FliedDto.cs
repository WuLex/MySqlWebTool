namespace MySqlWebManager.Dtos
{
    public class FliedDto
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