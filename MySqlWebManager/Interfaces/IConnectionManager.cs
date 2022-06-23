using MySqlWebManager.Dtos;

namespace MySqlWebManager.Interfaces
{
    public interface IConnectionManager
    {
        List<ConnectionDto> GetList();
        void AddConnection(ConnectionDto info);
        void RemoveConnection(string ConnectionId);
        void UpdateConnection(ConnectionDto info);
        ConnectionDto GetConnectionDtoById(string connectionId, bool increasePriority);
    }
}
