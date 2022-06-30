namespace MySqlWebManager.Dtos
{
    public class BatchGenerationInputDto
    {
        //string txt_namespace, string txt_file, string txt_db
        public string Namespace { get; set; }

        public string FilePath { get; set; }

        public string ConnectionId { get; set; }

    }
}
