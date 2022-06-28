namespace MySqlWebManager.Dtos
{
    public class GenerateCodeInputDto
    {
        public string TableName { get; set; }

        public string ConnectionId { get; set; }

        public List<CheckInfoDto> MethodList { get; set; }

        public List<string> QueryConditionList { get; set; }

        public List<string> QueryFieldList { get; set; }

    }
}
