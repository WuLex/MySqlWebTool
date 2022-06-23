using MySqlWebManager.Common;
using MySqlWebManager.Dtos;
using MySqlWebManager.Interfaces;
using System.Runtime.CompilerServices;
using System.Text;
namespace MySqlWebManager.Implements
{
    public class ConnectionManager : IConnectionManager
    {
        private List<ConnectionDto> s_list = null;
        private readonly Encoding DefaultEncoding = Encoding.Unicode;


        private readonly IWebHostEnvironment _hostingEnvironment;

        private static string _ContentRootPath = MyServiceProvider.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;

        //= Path.Combine(HttpRuntime.AppDomainAppPath, @"App_Data\Connection.xml");
        private readonly string s_savePath = Path.Combine(_ContentRootPath, @"Files\Connection.xml");



        /// <inheritdoc />
        public ConnectionManager(IWebHostEnvironment env)
        {
            try
            {
                _hostingEnvironment = env;

                string appDataPath = Path.Combine(_hostingEnvironment.WebRootPath, "App_Data");
                s_savePath = Path.Combine(_hostingEnvironment.ContentRootPath, @"Files\Connection.xml");
                if (Directory.Exists(appDataPath) == false)
                {
                    Directory.CreateDirectory(appDataPath);
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        public ConnectionManager()
        {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<ConnectionDto> GetList()
        {
            EnsureListLoaded();

            // 调用这个方法应该会比“修改”的次数会少很多，所以决定在这里排序。
            return (from c in s_list orderby c.Priority descending select c).ToList();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddConnection(ConnectionDto info)
        {
            EnsureListLoaded();

            s_list.Add(info);
            SaveListToFile();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void RemoveConnection(string ConnectionId)
        {
            EnsureListLoaded();

            int index = -1;
            for (int i = 0; i < s_list.Count; i++)
                if (s_list[i].ConnectionId == ConnectionId)
                {
                    index = i;
                    break;
                }

            if (index >= 0)
            {
                s_list.RemoveAt(index);
                SaveListToFile();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void UpdateConnection(ConnectionDto info)
        {
            EnsureListLoaded();

            ConnectionDto exist = s_list.FirstOrDefault(x => x.ConnectionId == info.ConnectionId);

            if (exist != null)
            {
                //exist.ServerIP = info.ServerIP;
                //exist.UserName = info.UserName;
                //exist.Password = info.Password;
                //exist.SSPI = info.SSPI;
                // 注意：其它没列出的成员，表示不需要在此更新。
                SaveListToFile();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public ConnectionDto GetConnectionDtoById(string connectionId, bool increasePriority)
        {
            if (string.IsNullOrEmpty(connectionId))
                throw new ArgumentNullException("connectionId");

            EnsureListLoaded();

            ConnectionDto exist = s_list.FirstOrDefault(x => x.ConnectionId == connectionId);
            if (exist == null)
            {
                return null;
                //throw new MyMessageException("connectionId is invalid.");
            }
            if (increasePriority)
            {
                exist.Priority++;
                SaveListToFile();
            }

            return exist;
        }


        private void EnsureListLoaded()
        {
            if (s_list == null)
            {
                try
                {
                    s_list = XmlHelper.XmlDeserializeFromFile<List<ConnectionDto>>(s_savePath, DefaultEncoding);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    s_list = new List<ConnectionDto>();
                }
            }
        }

        private void SaveListToFile()
        {
            if (s_list == null || s_list.Count == 0)
            {
                try
                {
                    File.Delete(s_savePath);
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                XmlHelper.XmlSerializeToFile(s_list, s_savePath, DefaultEncoding);
            }
        }
    }
}
