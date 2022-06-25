namespace MySqlWebManager.Dtos
{
    public class TableInfoDto
    {
        public string ConnectionId { get; set; }

        public List<string> TableNameList { get; set; } = new();
    }
}
