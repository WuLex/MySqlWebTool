namespace MySqlWebManager.Dtos
{
    public class TableInputDto : PageQueryParams
    {
        public string TableName { get; set; }

        public string ConnectionId { get; set; }
    }
}