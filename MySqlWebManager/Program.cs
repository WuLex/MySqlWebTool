using MySqlWebManager.Common;
using MySqlWebManager.Extentions;
using SqlSugar;

//IConfiguration configuration = new ConfigurationBuilder()
//                            .AddJsonFile("appsettings.json")
//                            .Build();

var builder = WebApplication.CreateBuilder(args);


MyServiceProvider.ServiceProvider = builder.Services.BuildServiceProvider();
// Add services to the container.
builder.Services.AddControllersWithViews();

#region ��������ע��
builder.Services.BatchRegisterService("MySqlWebManager");
#endregion

#region SqlSugarע��
builder.Services.AddScoped<ISqlSugarClient>(o =>
{
    return new SqlSugar.SqlSugarClient(new SqlSugar.ConnectionConfig()
    {
        ConnectionString = "Data Source=127.0.0.1;Initial Catalog=bbsdb;Persist Security Info=True;User ID=root;Password=wu12345;Pooling=False;charset=utf8;MAX Pool Size=2000;Min Pool Size=1;Connection Lifetime=30;",//����, ���ݿ������ַ���
        DbType = DbType.MySql,//����, ���ݿ�����
        IsAutoCloseConnection = true,//Ĭ��false, ʱ��֪���ر����ݿ�����, ����Ϊtrue����ʹ��using����Close����
        InitKeyType = SqlSugar.InitKeyType.SystemTable//Ĭ��SystemTable, �ֶ���Ϣ��ȡ, �磺�������ǲ�����������ʶ�еȵ���Ϣ
    });
});
//builder.Services.AddSqlSugar(new IocConfig
//{
//    ConnectionString = "Data Source = 192.168.1.64; Initial Catalog = classzone; Persist Security Info=True; User ID = root; Password = root; Pooling = False; charset = utf8; MAX Pool Size=2000; Min Pool Size=1; Connection Lifetime = 30;",//���ӷ��ִ�
//    DbType = IocDbType.MySql,
//    IsAutoCloseConnection = true,
//}); 
#endregion

#region �����ַ���
IConfigurationRoot configuration = builder.Configuration;
var conn = configuration.GetConnectionString("DefaultConnection");
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
