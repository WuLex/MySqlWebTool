using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using MySqlWebManager.Common;
using MySqlWebManager.Dtos;
using MySqlWebManager.Interfaces;
using MySqlWebManager.Models;
using MySqlWebManager.util;
using SqlSugar;
using System.Text;

namespace MySqlWebManager.Controllers
{
    public class DDDCodeController : Controller
    {
        #region 变量

        private string connStr =
            "Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Pooling=False;charset=utf8;" +
            "MAX Pool Size=2000;Min Pool Size=1;Connection Lifetime=30;";

        private string conn = string.Empty;

        //查询表名的sql字符串
        private readonly string gettables = "select table_name from information_schema.tables where table_schema='{0}'";

        //查询表结构字段的sql字符串
        private readonly string getflieds =
            "select column_name ColumnName,DATA_TYPE,COLUMN_TYPE,IS_NULLABLE as IS_NULL,column_comment as Comment,extra as Auto,CHARACTER_MAXIMUM_LENGTH as MaxLen " +
            "from INFORMATION_SCHEMA.COLUMNS Where table_name ='{0}' and table_schema ='{1}' ";

        //查询表结构字段总数的sql
        private readonly string getTotalCountSql =
            "select count(1) as totalcount from INFORMATION_SCHEMA.COLUMNS Where table_name ='{0}' and table_schema ='{1}' ";

        #endregion 变量

        #region 依赖注入
        private readonly IConfiguration _configuration;
        private readonly ISqlSugarClient _db; // 核心对象：拥有完整的SqlSugar全部功能
        private readonly IConnectionManager _connectionManager;

        private readonly IFileProvider _fileProvider;

        public DDDCodeController(IConfiguration configuration, ISqlSugarClient db,
            IConnectionManager connectionManager, IFileProvider fileProvider)
        {
            _configuration = configuration;
            _db = db;
            conn = _configuration.GetConnectionString("DefaultConnection");
            _connectionManager = connectionManager;
            _fileProvider = fileProvider;
        }

        #endregion 依赖注入

        public async Task<IActionResult> Index()
        {
            IDirectoryContents contents = _fileProvider.GetDirectoryContents("");
            var fileInfoList = contents.Where(f => f.IsDirectory == false).ToList();
            for (int i = 0; i < fileInfoList.Count(); i++)
            {
                var templateContent = await System.IO.File.ReadAllTextAsync(fileInfoList[i].PhysicalPath, Encoding.UTF8);
            }

            return View(contents);
        }

        //public async Task<IActionResult> GenerateCode()
        //{
        //    //IDirectoryContents contents= _fileProvider.GetDirectoryContents("*.txt");

        //    _fileProvider.

        //}

        [HttpPost]
        public async Task<TableInfoDto> GetTablesListAsync([FromBody] ConnectionDto connectionDto)
        {
            var tableInfoDto = new TableInfoDto() { ConnectionId = "", TableNameList = new List<string>() };
            string getTableSql = string.Format(gettables, connectionDto.Db);
            conn = string.Format(connStr, connectionDto.Server, connectionDto.Db, connectionDto.Uid, connectionDto.Pwd);
            _db.Ado.Connection.ConnectionString = conn;
            try
            {
                _db.Ado.CheckConnection();
                connectionDto.ConnectionId = Guid.NewGuid().ToString();
                _connectionManager.AddConnection(connectionDto); //添加连接信息到xml

                tableInfoDto.ConnectionId = connectionDto.ConnectionId;
                tableInfoDto.TableNameList = await _db.Ado.SqlQueryAsync<string>(getTableSql);
            }
            catch (Exception ex)
            {
                //return Json(new { code = -1, msg = "无数据", count = 0, data = "" });
                return tableInfoDto;
            }

            return tableInfoDto;
        }

        [HttpPost]
        public async Task<PageDataResult<TableField>> GetTableFieldsListAsync([FromBody] TableInputDto tableInputDto)
        {
            //获取指定connectid的数据库信息
            ConnectionDto connectionDto = _connectionManager.GetConnectionDtoById(tableInputDto.ConnectionId, true);
            if (connectionDto != null)
            {
                conn = string.Format(connStr, connectionDto.Server, connectionDto.Db, connectionDto.Uid,
                    connectionDto.Pwd);
                _db.Ado.Connection.ConnectionString = conn;

                string getTableFieldSql = string.Format(getflieds, tableInputDto.TableName, connectionDto.Db);
                var totalCount = await _db.Ado.GetIntAsync(string.Format(getTotalCountSql, tableInputDto.TableName, connectionDto.Db));
                if (tableInputDto.Page >= 1 && tableInputDto.Limit > 0)
                {
                    int offsetNum = (tableInputDto.Page - 1) * tableInputDto.Limit;
                    string pagesql = $" limit {offsetNum},{tableInputDto.Limit}";
                    getTableFieldSql += pagesql;
                }

                List<TableField> datList = await _db.Ado.SqlQueryAsync<TableField>(getTableFieldSql);

                return new PageDataResult<TableField>()
                {
                    Msg = "success",
                    Code = 0,
                    Count = totalCount,
                    Data = datList
                };
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// 生成DDD代码
        /// </summary>
        /// <param name="generateCodeInputDto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GenerateCharpCodeAsync([FromBody] GenerateCodeInputDto generateCodeInputDto)
        {
            StringBuilder codeBuilder = new StringBuilder();

            if (string.IsNullOrEmpty(generateCodeInputDto.TableName))
            {
                return new JavaScriptResult("请选择表!");
            }

            if (generateCodeInputDto.MethodList == null || !generateCodeInputDto.MethodList.Any())
            {
                return new JavaScriptResult("未勾选任何一个方法名称!");
            }

            if (generateCodeInputDto.MethodList.Any())
            {
                codeBuilder.Append((await GenerateTemplateCodeAsync(generateCodeInputDto)) ?? "");
            }

            //if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_pagelist" && c.IsChecked == true))
            //{
            //    //GetPageList();
            //    codeBuilder.Append(((ContentResult)await GetPageListAsync(generateCodeInputDto)).Content ?? "");
            //}

            //if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_update" && c.IsChecked == true))
            //{
            //    //Update();
            //    codeBuilder.Append(((ContentResult)await UpdateAsync(generateCodeInputDto)).Content ?? "");
            //}

            //if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_delete" && c.IsChecked == true))
            //{
            //    //Delete();
            //    codeBuilder.Append(((ContentResult)await DeleteAsync(generateCodeInputDto)).Content ?? "");
            //}

            //if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_add" && c.IsChecked == true))
            //{
            //    // Insert();
            //    codeBuilder.Append((await InsertAsync(generateCodeInputDto)).Content ?? "");
            //}

            //if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_getmodel" && c.IsChecked == true))
            //{
            //    //GetModel();
            //    codeBuilder.Append(((ContentResult)await GetModel(generateCodeInputDto)).Content ?? "");
            //}

            //ReaderBind();
            return Content(codeBuilder.ToString());
        }

        public async Task<string> GenerateTemplateCodeAsync(GenerateCodeInputDto generateCodeInputDto)
        {
            #region 定义变量

            StringBuilder sb = new StringBuilder();
            List<TableField> dataList = new List<TableField>();
            var totalCount = 0;
            var tablename = generateCodeInputDto.TableName;

            #endregion

            //获取数据库连接信息
            ConnectionDto connectionDto =
                _connectionManager.GetConnectionDtoById(generateCodeInputDto.ConnectionId, true);

            if (connectionDto != null)
            {
                conn = string.Format(connStr, connectionDto.Server, connectionDto.Db, connectionDto.Uid,
                    connectionDto.Pwd);
                _db.Ado.Connection.ConnectionString = conn;

                string getTableFieldSql = string.Format(getflieds, tablename, connectionDto.Db);

                dataList = await _db.Ado.SqlQueryAsync<TableField>(getTableFieldSql);
                totalCount = dataList.Count();
            }

            #region 拼接代码字符串
            sb.Append("\n\n\n//=============DDD===============\n");

            #endregion

            //sb.Append("public IList<" + tablename + "> GetList(" + conndetion + "int top = 10)");
            //sb.Append("\n{\n");
            //sb.Append("IList<" + tablename + "> list= new List<" + tablename + ">();\r");

            // name="cb_controller"  i
            // name = "cb_apicontroller"
            // name="cb_repository"  i
            // name = "cb_service"  id="
            // name="cb_viewmodel"  id
            // name = "cb_model"  id="cb


            #region 模板文件读取

            IDictionary<string, string> TemplateDict = new Dictionary<string, string>();
            IDirectoryContents contents = _fileProvider.GetDirectoryContents("");
            var fileInfoList = contents.Where(f => f.IsDirectory == false).ToList();
            for (int i = 0; i < fileInfoList.Count(); i++)
            {
                var templateContent = await System.IO.File.ReadAllTextAsync(fileInfoList[i].PhysicalPath, Encoding.UTF8);
                switch (fileInfoList[i].Name)
                {

                    case "ApiControllerTemplate.txt":
                        TemplateDict.Add("cb_apicontroller", templateContent);
                        //sb.Append(templateContent);
                        break;

                    case "ControllerTemplate.txt":
                        TemplateDict.Add("cb_controller", templateContent);
                        break;

                    case "IRepositoryTemplate.txt":
                        TemplateDict.Add("cb_irepository", templateContent);
                        break;

                    case "IServiceTemplate.txt":
                        TemplateDict.Add("cb_iservice", templateContent);
                        break;
                    case "ModelTemplate.txt":
                        TemplateDict.Add("cb_model", templateContent);
                        break;
                    case "RepositoryTemplate.txt":
                        TemplateDict.Add("cb_repository", templateContent);
                        break;
                    case "ServiceTemplate.txt":
                        TemplateDict.Add("cb_service", templateContent);
                        break;
                    case "ViewModelTemplate.txt":
                        TemplateDict.Add("cb_viewmodel", templateContent);
                        break;
                    default:
                        break;
                }

            }
            #endregion

            var layerList = generateCodeInputDto.MethodList.Where(c => c.IsChecked == true).ToList();
            for (int i = 0; i < layerList.Count; i++)
            {
                sb.Append(TemplateDict[layerList[i].CheckName]);
                sb.Append("\r\n---------------------------------------------------------------------\r\n");
            }
            return sb.ToString();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="batchGenerationInputDto"></param>
        /// <returns></returns>
        protected async Task<IActionResult> BatchGenerationAsync(BatchGenerationInputDto batchGenerationInputDto)
        {
            //string txt_namespace, string txt_file, string txt_db
            string Modelnamespace = batchGenerationInputDto.Namespace;
            string ModelFilePath = batchGenerationInputDto.FilePath;

            //获取指定connectid的数据库信息
            ConnectionDto connectionDto =
                _connectionManager.GetConnectionDtoById(batchGenerationInputDto.ConnectionId, true);
            if (connectionDto != null)
            {
                conn = string.Format(connStr, connectionDto.Server, connectionDto.Db, connectionDto.Uid,
                    connectionDto.Pwd);
                _db.Ado.Connection.ConnectionString = conn;
            }
            else
            {
                return Content("数据库连接信息丢失,请重新连接!");
            }

            #region 获取表名称列表信息
            string getTableSql = string.Format(gettables, connectionDto.Db);
            var tableNameList = await _db.Ado.SqlQueryAsync<string>(getTableSql);
            var totalCount = tableNameList.Count();
            #endregion 获取表名称列表信息

            //--------------------------------------------------
            try
            {
                for (int i = 0; i < totalCount; i++)
                {
                    string tablename = tableNameList[i].ToString();
                    string wjj = ModelFilePath + connectionDto.Db;

                    #region 检查是否存在,不存在则创建文件
                    if (!Directory.Exists(wjj))
                    {
                        Directory.CreateDirectory(wjj);
                    }

                    string path = wjj + "/" + tablename + ".cs";
                    if (!System.IO.File.Exists(path))
                    {
                        //不存在,则创建
                        System.IO.File.Create(path).Close();
                    }
                    #endregion 检查是否存在,不存在则创建文件

                    #region 生成文件到指定路径
                    //写
                    using (StreamWriter w = System.IO.File.AppendText(path))
                    {
                        StringBuilder sb = new StringBuilder();
                        #region 获取表字段结构信息
                        List<TableField> dataList = await _db.Ado.SqlQueryAsync<TableField>(string.Format(getflieds, tablename, connectionDto.Db));
                        totalCount = dataList.Count();
                        if (totalCount <= 0)
                        {
                            return Content("NoData");
                        }
                        #endregion 获取表字段结构信息
                        #region

                        sb.Append("using System; ");
                        sb.Append("\rnamespace " + Modelnamespace);
                        sb.Append("\r{");
                        sb.Append("\r\tpublic class " + tablename);
                        sb.Append("\r\t{");
                        for (int j = 0; j < totalCount; j++)
                        {
                            var fliedname = dataList[j].ColumnName.ToString();
                            var fliedtype = dataList[j].DATA_TYPE.ToString();
                            var comment = dataList[j].Comment.ToString();

                            #region 参数

                            if (!string.IsNullOrWhiteSpace(comment))
                            {
                                sb.Append("\r\t\t/// <summary>");
                                sb.Append("\r\t\t/// " + comment);
                                sb.Append("\r\t\t/// </summary>");
                            }

                            sb.Append("\r\t\tpublic " + MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + "{ get; set; }\n");

                            #endregion 参数
                        }

                        sb.Append("\r\t}");
                        sb.Append("\r}");

                        #endregion 生成文件到指定路径

                        w.Write(sb.ToString());
                        w.Flush();
                        w.Close();
                    }
                    #endregion
                }
                return Content("success");
            }
            catch (Exception ex)
            {
                return Content("Fail");
            }
        }
    }
}