namespace MySqlWebManager.Dtos
{
    [Serializable]
    public class ConnectionDto
    {
        public string Server { get; set; }
        public string Db { get; set; }
        public string Uid { get; set; }
        public string Pwd { get; set; }

        public string ConnectionId { get; set; }

        public int Priority { get; set; }
    }
}
