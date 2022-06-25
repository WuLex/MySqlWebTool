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

#region 批量依赖注入
builder.Services.BatchRegisterService("MySqlWebManager");
#endregion

#region SqlSugar注入
builder.Services.AddScoped<ISqlSugarClient>(o =>
{
    return new SqlSugar.SqlSugarClient(new SqlSugar.ConnectionConfig()
    {
        ConnectionString = "Data Source=127.0.0.1;Initial Catalog=bbsdb;Persist Security Info=True;User ID=root;Password=wu12345;Pooling=False;charset=utf8;MAX Pool Size=2000;Min Pool Size=1;Connection Lifetime=30;",//必填, 数据库连接字符串
        DbType = DbType.MySql,//必填, 数据库类型
        IsAutoCloseConnection = true,//默认false, 时候知道关闭数据库连接, 设置为true无需使用using或者Close操作
        InitKeyType = SqlSugar.InitKeyType.SystemTable//默认SystemTable, 字段信息读取, 如：该属性是不是主键，标识列等等信息
    });
});
//builder.Services.AddSqlSugar(new IocConfig
//{
//    ConnectionString = "Data Source = 192.168.1.64; Initial Catalog = classzone; Persist Security Info=True; User ID = root; Password = root; Pooling = False; charset = utf8; MAX Pool Size=2000; Min Pool Size=1; Connection Lifetime = 30;",//连接符字串
//    DbType = IocDbType.MySql,
//    IsAutoCloseConnection = true,
//}); 
#endregion

#region 连接字符串
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
