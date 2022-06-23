namespace MySqlWebManager.Dtos
{
    public class ConnectionDto
    {
        public string txt_server { get; set; }
        public string txt_db { get; set; }
        public string txt_uid { get; set; }
        public string txt_pwd { get; set; }

        public string ConnectionId { get; set; }

        public int Priority { get; set; }
    }
}
