namespace MySqlWebManager.Dtos
{
    public class GenerateCodeInputDto
    {
        public string TableName { get; set; }

        public string ConnectionId { get; set; }

        public Dictionary<string, bool> MethodDic { get; set; }  
    }
}
