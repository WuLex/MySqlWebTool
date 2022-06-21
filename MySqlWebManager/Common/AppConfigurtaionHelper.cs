using Microsoft.Extensions.Configuration.Json;

namespace MySqlWebManager.Common
{
    /// <summary>
    /// 读取配置文件
    /// </summary>
    public class AppConfigurtaionHelper
    {
        public static IConfiguration Configuration { get; set; }
        static AppConfigurtaionHelper()
        {
            Configuration = new ConfigurationBuilder()
                .Add(new JsonConfigurationSource { Path = "appsettings.json", ReloadOnChange = true })
                .Build();
        }
    }
}
