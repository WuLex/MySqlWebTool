using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MySqlWebManager.Common;
using MySqlWebManager.Dtos;
using MySqlWebManager.Interfaces;
using MySqlWebManager.Models;
using MySqlWebManager.util;
using SqlSugar;
using System.Data;
using System.Text;

namespace MySqlWebManager.Controllers
{
    public class DatabaseAdminController : Controller
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

        public DatabaseAdminController(IConfiguration configuration, ISqlSugarClient db,
            IConnectionManager connectionManager)
        {
            _configuration = configuration;
            _db = db;
            conn = _configuration.GetConnectionString("DefaultConnection");
            _connectionManager = connectionManager;
        }

        #endregion 依赖注入

        public IActionResult Index()
        {
            return View();
        }

        #region MVC方法

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

        #endregion MVC方法

        #region DB

        public DataTable GetTable(ConnectionDto connectionDto, string sql)
        {
            var connInfo = new ConnectionDto()
            {
                Server = connectionDto.Server,
                Db = connectionDto.Db,
                Uid = connectionDto.Uid,
                Pwd = connectionDto.Pwd,
                ConnectionId = connectionDto.ConnectionId,
            };
            conn = string.Format(connStr, connInfo.Server, connInfo.Db, connInfo.Uid, connInfo.Pwd);
            DataSet ds = MySqlHelper.ExecuteDataset(conn, sql);
            return ds.Tables[0];
        }

        public void ExecuteSql(string sql)
        {
            MySqlHelper.ExecuteNonQuery(conn, sql);
        }

        #endregion DB

        #region select

        //private IActionResult SelectAll(StringBuilder sb, DataTable dt, int count, string tablename, string proname)
        //{
        //    var cb3list = Request.Query["cb3"].ToString();
        //    if (string.IsNullOrEmpty(cb3list))
        //    {
        //        //Page.RegisterStartupScript("alert", "<script>alert('请选择要查询的列！')</script>");
        //        return new JavaScriptResult("alert('请选择要查询的列！')");
        //    }

        //    string[] arraycb3 = new string[] { };
        //    arraycb3 = cb3list.Split(',');

        //    sb.Append("CREATE OR REPLACE Procedure pro_" + proname + "_" + tablename);
        //    sb.Append("\n(\n");
        //    //
        //    for (int i = 0; i < totalCount; i++)
        //    {
        //        var fliedname = dt.Rows[i]["column_name"].ToString();
        //        var fliedtype = dt.Rows[i]["data_type"].ToString();
        //        var fliedlength = 0; // dt.Rows[i]["data_length"].ToString();
        //                             //显示选中

        //        #region

        //        if (arraycb3.Any())
        //        {
        //            for (int j = 0; j < arraycb3.Count(); j++)
        //            {
        //                if (fliedname == arraycb3[j].ToString())
        //                {
        //                    sb.Append("      _" + fliedname + " out " + fliedtype + "(" + fliedlength + ")");
        //                    if (j != arraycb3.Count() - 1)
        //                    {
        //                        sb.Append(",\n");
        //                    }
        //                }
        //            }
        //        }

        //        #endregion select
        //    }

        //    sb.Append("\n)\n");
        //    sb.Append("AS\n");
        //    sb.Append("BEGIN\n");
        //    sb.Append("   SELECT ");
        //    for (int i = 0; i < totalCount; i++)
        //    {
        //        var fliedname = dataList[i].ColumnName;
        //        var fliedtype = dataList[i].DATA_TYPE;
        //        var fliedlength = 0; // dt.Rows[i]["data_length"].ToString();
        //                             //显示选中

        //        #region

        //        if (arraycb3.Any())
        //        {
        //            for (int j = 0; j < arraycb3.Count(); j++)
        //            {
        //                if (fliedname == arraycb3[j].ToString())
        //                {
        //                    sb.Append("[" + fliedname + "]");
        //                    if (j != arraycb3.Count() - 1)
        //                    {
        //                        sb.Append(",");
        //                    }
        //                }
        //            }
        //        }

        //        #endregion
        //    }

        //    sb.Append(" INTO ");
        //    for (int i = 0; i < totalCount; i++)
        //    {
        //        var fliedname = dataList[i].ColumnName;
        //        var fliedtype = dataList[i].DATA_TYPE;
        //        var fliedlength = 0; // dt.Rows[i]["data_length"].ToString();
        //                             //显示选中

        //        #region

        //        if (arraycb3.Any())
        //        {
        //            for (int j = 0; j < arraycb3.Count(); j++)
        //            {
        //                if (fliedname == arraycb3[j].ToString())
        //                {
        //                    sb.Append("_" + fliedname);
        //                    if (j != arraycb3.Count() - 1)
        //                    {
        //                        sb.Append(",");
        //                    }
        //                }
        //            }
        //        }

        //        #endregion
        //    }

        //    sb.Append(" FROM [" + tablename + "]");

        //    return Content(sb.ToString());
        //}

        #endregion select

        #region bind

        [HttpPost]
        public void BindTables(ConnectionDto connectionDto)
        {
            string getTableSql = string.Format(gettables, connectionDto.Db);
            //lb_tables.DataSource = GetTable(connectionDto,getTableSql);
            //lb_tables.DataTextField = "table_name";
            //lb_tables.DataValueField = "table_name";
            //lb_tables.DataBind();
        }

        public void BindFlieds(string tablename, string txt_db)
        {
            //gv_fileds.DataSource = GetTable(string.Format(getflieds, tablename, txt_db));
            //gv_fileds.DataBind();
        }

        [HttpPost]
        protected void Connection(ConnectionDto connectionDto)
        {
            BindTables(connectionDto);
        }

        protected void lb_tables_SelectedIndexChanged()
        {
            //BindFlieds(lb_tables.SelectedItem.Text);
        }

        #endregion bind

        #region 添加

        public async Task<IActionResult> InsertAsync(GenerateCodeInputDto generateCodeInputDto)
        {
            #region

            #region 定义变量

            StringBuilder sb = new StringBuilder();
            List<TableField> dataList = new List<TableField>();
            var totalCount = 0;
            var tablename = generateCodeInputDto.TableName;

            #endregion 定义变量

            #region 获取数据库连接信息

            ConnectionDto connectionDto =
                _connectionManager.GetConnectionDtoById(generateCodeInputDto.ConnectionId, true);

            if (connectionDto != null)
            {
                conn = string.Format(connStr, connectionDto.Server, connectionDto.Db, connectionDto.Uid,
                    connectionDto.Pwd);
                _db.Ado.Connection.ConnectionString = conn;

                string getTableFieldSql = string.Format(getflieds, tablename, connectionDto.Db);
                dataList = await _db.Ado.SqlQueryAsync<TableField>(getTableFieldSql);
                totalCount = dataList.Count;
            }
            else
            {
                return new JavaScriptResult("alert('请连接数据库!')");
            }

            #endregion 获取数据库连接信息

            //得到条件
            var queryConditionList = generateCodeInputDto.QueryConditionList;
            var selectFieldList = generateCodeInputDto.QueryFieldList;
            if (!queryConditionList.Any())
            {
                return new JavaScriptResult("alert('请选择表条件!')");
            }

            if (!selectFieldList.Any())
            {
                return new JavaScriptResult("alert('请选择表查询字段!')");
            }

            #endregion 添加

            sb.Append("\r\rpublic bool Insert(" + tablename + " model)");
            sb.Append("\n{\n");

            #region 修改的字段

            string sql = "insert into " + tablename + "(";
            string paras = "";
            for (int i = 0; i < totalCount; i++)
            {
                if (dataList[i].Auto == "auto_increment")
                {
                    continue;
                }

                var fliedname = dataList[i].ColumnName;
                sql += fliedname + ",";
                paras += "@" + fliedname + ",";
            }

            sql = sql.TrimEnd(',') + ")";
            paras = paras.TrimEnd(',');
            sql += " values (" + paras + ")";

            #endregion 修改的字段

            sb.Append("string sql=\"" + sql + "\";");
            sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            int c = 0;
            string conndetion = "";
            for (int i = 0; i < totalCount; i++)
            {
                if (dataList[i].Auto == "auto_increment")
                {
                    continue;
                }

                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                var fliedlen = dataList[i].MaxLen ?? "";

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) +
                          len + ")");
                sb.Append(",");
                conndetion += "\rparameters[" + c++ + "].Value = model." + fliedname + ";";

                #endregion 参数
            }

            string strSb = sb.ToString().TrimEnd(',');
            sb = new StringBuilder(strSb);

            #endregion 条件

            sb.Append("\r\t\t\t\t};");
            sb.Append(conndetion);
            sb.Append("\rreturn MysqlCommonHelper.ExecuteNonQuery(connectionString,sql,parameters)>0;");
            sb.Append("\n}\n");
            //txt_content.Text += sb.ToString();

            return Content(sb.ToString());
        }

        #endregion

        #region GetModel

        [HttpPost]
        public async Task<IActionResult> GetModel(GenerateCodeInputDto generateCodeInputDto)
        {
            #region

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

            //得到条件
            var queryConditionList = generateCodeInputDto.QueryConditionList;
            var selectFieldList = generateCodeInputDto.QueryFieldList;
            if (!queryConditionList.Any())
            {
                return new JavaScriptResult("alert('请选择表条件!')");
            }

            if (!selectFieldList.Any())
            {
                return new JavaScriptResult("alert('请选择表查询字段!')");
            }

            #endregion

            //string[] arrayIndexID = new string[] { };
            //string[] arrayFlied = new string[] { };
            //arrayIndexID = IndexID.Split(','); //tiaojian
            //arrayFlied = SelectFlied.Split(',');

            sb.Append("\n\n\n//=============Select===============\n");

            #region 条件

            string conndetion = "";
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                #endregion
            }

            conndetion = conndetion.TrimEnd(',');

            #endregion

            sb.Append("public " + tablename + " GetModel(" + conndetion + ")");
            sb.Append("\n{\n");
            sb.Append("var model = new " + tablename + "();\r");

            #region 字段

            string sql = "select ";
            StringBuilder sbBinder = new StringBuilder();
            //sbBinder.Append("\r\t model=new " + tablename + "{");//begin

            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 字段

                for (int j = 0; j < selectFieldList.Count(); j++)
                {
                    if (fliedname == selectFieldList[j])
                    {
                        sql += fliedname;
                        if (fliedtype.ToLower() == "varchar" || fliedtype.ToLower() == "text")
                        {
                            sbBinder.Append("\r\tmodel." + fliedname + "=dr[\"" + fliedname + "\"].ToString();");
                        }
                        else if (fliedtype.ToLower() == "bit")
                        {
                            sbBinder.Append("\r\tmodel." + fliedname + "=dr[\"" + fliedname + "\"].ToString()==\"1\";");
                        }
                        else
                        {
                            sbBinder.Append("\r\tmodel." + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) +
                                            ".Parse(dr[\"" +
                                            fliedname + "\"].ToString());");
                        }

                        if (j != selectFieldList.Count() - 1)
                        {
                            sql += ",";
                        }
                    }
                }

                #endregion
            }

            //sbBinder.Append("};");//end

            #endregion

            sql += " from " + tablename;

            #region 条件

            conndetion = "";
            for (int i = 0; i < totalCount; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j].ToString())
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < queryConditionList.Count() - 1)
                            conndetion += " and ";
                    }
                }

                //conndetion = conndetion.Trim(',');

                #endregion
            }

            sql += conndetion;

            #endregion

            #region 排序

            var cb_order = Convert.ToString(Request.Query["cb_order"]);
            if (!string.IsNullOrWhiteSpace(cb_order))
            {
                var _orders = cb_order.Split(',');
                string strOrder = " order by ";
                foreach (var o in _orders)
                {
                    var asc = Request.Query["ddl_" + o];
                    strOrder += o + " " + asc + ",";
                }

                strOrder = strOrder.TrimEnd(',');
                sql += strOrder;
            }

            #endregion

            sql += " limit 1";
            sb.Append("string sql=\"" + sql + "\";");
            sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                var fliedlen = dataList[i].MaxLen ?? "";

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " +
                                  MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        if (j < queryConditionList.Count() - 1)
                            sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + queryConditionList[j].ToString() + ";";
                    }
                }

                #endregion
            }

            //sb.Append("\r\t new MySqlParameter(\"@top\", MySqlDbType.Int32)");
            //conndetion += "\rparameters[" + c++ + "].Value = top;";

            #endregion

            sb.Append("\r\t\t\t\t};");
            sb.Append(conndetion);
            sb.Append("\rusing (var dr = MysqlCommonHelper.ExecuteReader(connectionString, sql, parameters))");
            sb.Append("\r{");
            sb.Append("\t\rif (dr.Read())");
            sb.Append("\t\r{");
            //sb.Append("\t\rlist.Add(ReaderBind(dr));");

            #region read bind

            sb.Append(sbBinder.ToString());

            #endregion

            sb.Append("\t\r}");
            sb.Append("\r}");
            sb.Append("\rreturn model;");
            sb.Append("\n}\n");
            //txt_content.Text += sb.ToString();

            return Content(sb.ToString());
        }

        #endregion

        #region 列表

        public async Task<IActionResult> GetListAsync(GenerateCodeInputDto generateCodeInputDto)
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
            //var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));

            #region 拼接代码字符串

            //得到条件
            var queryConditionList = generateCodeInputDto.QueryConditionList;
            var selectFieldList = generateCodeInputDto.QueryFieldList;

            if (!queryConditionList.Any())
            {
                return new JavaScriptResult("alert('请选择表条件!')");
            }

            if (!selectFieldList.Any())
            {
                return new JavaScriptResult("alert('请选择表查询字段!')");
            }

            //string[] arrayIndexID = new string[] { };
            //string[] arrayFlied = new string[] { };
            //arrayIndexID = IndexID.Split(','); //tiaojian
            //arrayFlied = SelectFlied.Split(',');

            sb.Append("\n\n\n//=============Select===============\n");

            #region 条件

            string conndetion = "";

            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                #endregion
            }

            #endregion

            sb.Append("public IList<" + tablename + "> GetList(" + conndetion + "int top = 10)");
            sb.Append("\n{\n");
            sb.Append("IList<" + tablename + "> list= new List<" + tablename + ">();\r");

            #region 字段

            string sql = "select ";
            StringBuilder sbBinder = new StringBuilder();
            sbBinder.Append("\r\tlist.Add(new " + tablename + "(){"); //begin

            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 字段

                for (int j = 0; j < selectFieldList.Count(); j++)
                {
                    if (fliedname == selectFieldList[j])
                    {
                        sql += fliedname;
                        if (fliedtype.ToLower() == "varchar" || fliedtype.ToLower() == "text")
                        {
                            sbBinder.Append("\r\t" + fliedname + "=dr[\"" + fliedname + "\"].ToString()");
                        }
                        else if (fliedtype.ToLower() == "bit")
                        {
                            sbBinder.Append("\r\tmodel." + fliedname + "=dr[\"" + fliedname + "\"].ToString()==\"1\"");
                        }
                        else
                        {
                            sbBinder.Append("\r\t" + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) +
                                            ".Parse(dr[\"" +
                                            fliedname + "\"].ToString())");
                        }

                        if (j != selectFieldList.Count() - 1)
                        {
                            sql += ",";
                            sbBinder.Append(",");
                        }
                    }
                }

                #endregion
            }

            sbBinder.Append("});"); //end

            #endregion

            sql += " from " + tablename;

            #region 条件

            conndetion = "";
            for (int i = 0; i < totalCount; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < queryConditionList.Count() - 1)
                            conndetion += " and ";
                    }
                }

                //conndetion = conndetion.Trim(',');

                #endregion
            }

            sql += conndetion;

            #endregion

            #region 排序

            var cb_order = Convert.ToString(Request.Query["cb_order"]);
            if (!string.IsNullOrWhiteSpace(cb_order))
            {
                var _orders = cb_order.Split(',');
                string strOrder = " order by ";
                foreach (var o in _orders)
                {
                    var asc = Convert.ToString(Request.Query["ddl_" + o]);
                    strOrder += o + " " + asc + ",";
                }

                strOrder = strOrder.TrimEnd(',');
                sql += strOrder;
            }

            #endregion

            sql += " limit @top";
            sb.Append("string sql=\"" + sql + "\";");
            sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                var fliedlen = dataList[i].MaxLen ?? "";

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j].ToString())
                    {
                        sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " +
                                  MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + queryConditionList[j].ToString() + ";";
                    }
                }

                #endregion
            }

            sb.Append("\r\t new MySqlParameter(\"@top\", MySqlDbType.Int32)");
            conndetion += "\rparameters[" + c++ + "].Value = top;";

            #endregion

            sb.Append("\r\t\t\t\t};");
            sb.Append(conndetion);
            sb.Append("\rusing (var dr = MysqlCommonHelper.ExecuteReader(connectionString, sql, parameters))");
            sb.Append("\r{");
            sb.Append("\t\rwhile (dr.Read())");
            sb.Append("\t\r{");
            //sb.Append("\t\rlist.Add(ReaderBind(dr));");

            #region read bind

            sb.Append(sbBinder.ToString());

            #endregion

            sb.Append("\t\r}");
            sb.Append("\r}");
            sb.Append("\rreturn list;");
            sb.Append("\n}\n");
            //txt_content.Text = sb.ToString();

            #endregion

            return Content(sb.ToString());
        }

        #endregion

        #region 分页

        public async Task<IActionResult> GetPageListAsync(GenerateCodeInputDto generateCodeInputDto)
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

            //得到条件
            var queryConditionList = generateCodeInputDto.QueryConditionList;
            var selectFieldList = generateCodeInputDto.QueryFieldList;
            var queryOrderList = generateCodeInputDto.QueryOrderList;

            if (!queryConditionList.Any())
            {
                return new JavaScriptResult("alert('请选择表条件!')");
            }

            if (!selectFieldList.Any())
            {
                return new JavaScriptResult("alert('请选择表查询字段!')");
            }

            //string[] arrayIndexID = new string[] { };
            //string[] arrayFlied = new string[] { };
            //arrayIndexID = IndexID.Split(','); //tiaojian
            //arrayFlied = SelectFlied.Split(',');

            sb.Append("\n\n\n//=============分页===============\n");

            #region 条件

            string conndetion = "";
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j].ToString())
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                #endregion
            }

            #endregion

            sb.Append("public IList<" + tablename + "> GetList(out int count," + conndetion +
                      "int pageindex = 1, int pagesize = 10)");
            sb.Append("\n{\n");
            sb.Append("IList<" + tablename + "> list= new List<" + tablename + ">();\r");
            sb.Append("int pagestart = (pageindex - 1)*pagesize;\r");
            //sb.Append("int pageend = pagestart + pagesize;\r");

            string where = "";

            for (int i = 0; i < totalCount; i++)
            {
                if (i == 0)
                {
                    where = " where ";
                }

                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        where += fliedname + "=@" + fliedname;
                        if (j < queryConditionList.Count() - 1)
                        {
                            where += " and ";
                        }
                    }
                }

                //where = where.Trim(',');

                #endregion
            }

            sb.Append("string where = \"" + where + "\";\r");
            StringBuilder sbBinder = new StringBuilder();
            sbBinder.Append("\r\tlist.Add(new " + tablename + "(){"); //begin

            #region 字段

            string sql = "select ";
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 字段

                for (int j = 0; j < selectFieldList.Count(); j++)
                {
                    if (fliedname == selectFieldList[j])
                    {
                        sql += fliedname;
                        if (fliedtype.ToLower() == "varchar" || fliedtype.ToLower() == "text")
                        {
                            sbBinder.Append("\r\t" + fliedname + "=dr[\"" + fliedname + "\"].ToString()");
                        }
                        else if (fliedtype.ToLower() == "bit")
                        {
                            sbBinder.Append("\r\tmodel." + fliedname + "=dr[\"" + fliedname + "\"].ToString()==\"1\"");
                        }
                        else
                        {
                            sbBinder.Append("\r\t" + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) +
                                            ".Parse(dr[\"" +
                                            fliedname + "\"].ToString())");
                        }

                        if (j != selectFieldList.Count() - 1)
                        {
                            sbBinder.Append(",");
                            sql += ",";
                        }
                    }
                }

                #endregion
            }

            sbBinder.Append("});"); //end

            #endregion

            sql += " from " + tablename;

            #region 条件

            sql += "\"+where+\"";

            #endregion

            #region 排序

            //var cb_order = Convert.ToString(Request.Query["cb_order"]);
            if (queryOrderList != null && !queryOrderList.Any())
            {
                //var _orders = cb_order.Split(',');
                string strOrder = " order by ";
                foreach (var o in queryOrderList)
                {
                    //var asc = Request.Query["ddl_" + o];
                    var asc = Request.Query["ddl_" + o];
                    strOrder += o + " " + asc + ",";
                }

                strOrder = strOrder.TrimEnd(',');
                sql += strOrder;
            }

            #endregion

            //sql += " limit @top";
            sql += " limit \"+pagestart+\", \"+pagesize";
            sb.Append("string sql=\"" + sql + ";");
            sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                var fliedlen = dataList[i].MaxLen ?? "";

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j].ToString())
                    {
                        sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " +
                                  MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        if (j < queryConditionList.Count() - 1)
                            sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + queryConditionList[j].ToString() + ";";
                    }
                }

                #endregion
            }

            //sb.Append("\r\t new MySqlParameter(\"@top\", MySqlDbType.Int32)");
            //conndetion += "\rparameters[" + c++ + "].Value = top;";

            #endregion

            sb.Append("\r\t\t\t\t};");
            sb.Append(conndetion);
            sb.Append("\rusing (var dr = MysqlCommonHelper.ExecuteReader(connectionString, sql, parameters))");
            sb.Append("\r{");
            sb.Append("\r\twhile (dr.Read())");
            sb.Append("\r\t{");
            //sb.Append("\r\tlist.Add(ReaderBind(dr));");
            sb.Append(sbBinder.ToString());
            sb.Append("\r\t}");
            sb.Append("\r}");
            sb.Append("\rcount=GetCount(where, parameters);");
            sb.Append("\rreturn list;");
            sb.Append("\n}\n");
            //txt_content.Text += sb.ToString();
            //GetCount();
            return Content(sb.ToString());
        }

        #endregion

        #region 修改

        public async Task<IActionResult> UpdateAsync(GenerateCodeInputDto generateCodeInputDto)
        {
            #region

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

            //得到条件
            var queryConditionList = generateCodeInputDto.QueryConditionList;
            var selectFieldList = generateCodeInputDto.QueryFieldList;
            if (!queryConditionList.Any())
            {
                return new JavaScriptResult("alert('请选择表条件!')");
            }

            if (!selectFieldList.Any())
            {
                return new JavaScriptResult("alert('请选择表修改字段!')");
            }

            #endregion

            //string[] arrayIndexID = new string[] { };
            //string[] arrayFlied = new string[] { };
            //arrayIndexID = IndexID.Split(','); //tiaojian
            //arrayFlied = SelectFlied.Split(',');

            sb.Append("\n\n\n//=============Select===============\n");

            #region 方法参数

            string conndetion = "";

            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count; j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                #endregion
            }

            conndetion = conndetion.TrimEnd(',');

            #endregion

            sb.Append("\r\rpublic bool Update(" + conndetion + ")");
            sb.Append("\n{\n");

            #region 修改的字段

            string sql = "update  " + tablename + " set ";
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                //var fliedtype = dataList[i].DATA_TYPE;

                #region 字段

                for (int j = 0; j < selectFieldList.Count(); j++)
                {
                    if (fliedname == selectFieldList[j])
                    {
                        sql += fliedname += "=@" + fliedname;
                        if (j != selectFieldList.Count() - 1)
                        {
                            sql += ",";
                        }
                    }
                }

                #endregion
            }

            #endregion

            #region 条件

            conndetion = "";
            for (int i = 0; i < totalCount; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < queryConditionList.Count() - 1)
                            conndetion += " and ";
                    }
                }

                //conndetion = conndetion.Trim(',');

                #endregion
            }

            sql += conndetion;

            #endregion

            sb.Append("string sql=\"" + sql + "\";");
            sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                var fliedlen = dataList[i].MaxLen ?? "";

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " +
                                  MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + queryConditionList[j] + ";";
                    }
                }

                conndetion = conndetion.TrimEnd(',');

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (!queryConditionList.Contains(queryConditionList[j]))
                    {
                        if (fliedname == queryConditionList[j])
                        {
                            sb.Append(
                                "\r\t new MySqlParameter(\"@" + fliedname + "\", " +
                                MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                            sb.Append(",");
                            conndetion += "\rparameters[" + c++ + "].Value = " + queryConditionList[j] + ";";
                        }
                    }
                }

                #endregion
            }

            string strSb = sb.ToString().TrimEnd(',');
            sb = new StringBuilder(strSb);

            #endregion

            sb.Append("\r\t\t\t\t};");
            sb.Append(conndetion);
            sb.Append("\rreturn MysqlCommonHelper.ExecuteNonQuery(connectionString,sql,parameters)>0;");
            sb.Append("\n}\n");
            //txt_content.Text += sb.ToString();
            return Content(sb.ToString());
        }

        #endregion

        #region 删除

        public async Task<IActionResult> DeleteAsync(GenerateCodeInputDto generateCodeInputDto)
        {
            #region

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

            //得到条件
            var queryConditionList = generateCodeInputDto.QueryConditionList;
            var selectFieldList = generateCodeInputDto.QueryFieldList;

            if (!queryConditionList.Any())
            {
                return new JavaScriptResult("alert('请选择删除条件!')");
            }

            #endregion

            //string[] arrayIndexID = new string[] { };
            //string[] arrayFlied = new string[] { };
            //arrayIndexID = IndexID.Split(','); //tiaojian
            //arrayFlied = SelectFlied.Split(',');

            sb.Append("\n\n\n//=============Delete===============\n");

            #region 方法参数

            string conndetion = "";

            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数遍历

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j].ToString())
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                //conndetion = conndetion.TrimEnd(',');

                #endregion
            }

            conndetion = conndetion.TrimEnd(',');

            #endregion

            sb.Append("\r\rpublic bool Delete(" + conndetion + ")");
            sb.Append("\n{\n");
            string sql = "delete from  " + tablename + " ";

            #region 条件

            conndetion = "";
            for (int i = 0; i < totalCount; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;

                #region 参数

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j].ToString())
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < queryConditionList.Count() - 1)
                            conndetion += " and ";
                    }
                }

                //conndetion = conndetion.Trim(',');

                #endregion
            }

            sql += conndetion;

            #endregion

            sb.Append("string sql=\"" + sql + "\";");
            sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                var fliedlen = dataList[i].MaxLen ?? "";

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < queryConditionList.Count(); j++)
                {
                    if (fliedname == queryConditionList[j])
                    {
                        sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " +
                                  MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + queryConditionList[j].ToString() + ";";
                    }
                }

                conndetion = conndetion.TrimEnd(',');

                #endregion
            }

            string strSb = sb.ToString().TrimEnd(',');
            sb = new StringBuilder(strSb);

            #endregion

            sb.Append("\r\t\t\t\t};");
            sb.Append(conndetion);
            sb.Append("\nreturn MysqlCommonHelper.ExecuteNonQuery(connectionString,sql,parameters)>0;");
            sb.Append("\n}\n");
            //txt_content.Text += sb.ToString();

            return Content(sb.ToString());
        }

        #endregion

        #region count

        public string GetCount(GenerateCodeInputDto generateCodeInputDto)
        {
            var tablename = ""; //lb_tables.SelectedItem.Text;
            string str = "\rpublic int GetCount(string where ,MySqlParameter[] parameters=null)" +
                         "\r{" +
                         "\r\tstring sql = \"select count(*) from " + tablename + " \" + where;" +
                         "\r\tint rows = Convert.ToInt32(MysqlCommonHelper.ExecuteScalar(connectionString, sql, parameters));" +
                         "\r\treturn rows;" +
                         "\r}";
            //txt_content.Text += str;
            return str;
        }

        #endregion

        #region Bind

        /// <summary>
        /// IDataReader
        /// </summary>
        /// <param name="generateCodeInputDto"></param>
        /// <returns></returns>
        private async Task ReaderBindAsync(GenerateCodeInputDto generateCodeInputDto)
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

            sb.Append("\rpublic " + tablename + " ReaderBind(IDataReader dr)");
            sb.Append("\r{");
            sb.Append("\r\t//需保证字段不能为null");
            sb.Append("\r\tvar model = new " + tablename + "();");
            for (int i = 0; i < totalCount; i++)
            {
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                if (fliedtype.ToLower() == "varchar" || fliedtype.ToLower() == "text" || fliedtype.ToLower() == "char")
                {
                    sb.Append("\r\tmodel." + fliedname + "=dr[\"" + fliedname + "\"].ToString();");
                }
                else
                {
                    sb.Append("\r\tmodel." + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) +
                              ".Parse(dr[\"" + fliedname +
                              "\"].ToString());");
                }
            }

            sb.Append("\r\treturn model;");
            sb.Append("\r}");
            //txt_content.Text += sb.ToString();
        }

        #endregion

        #region Model

        public async Task<IActionResult> GenerateEntityAsync([FromBody] TableInputDto tableInputDto)
        {
            #region 检查数据

            //获取指定connectid的数据库信息
            ConnectionDto connectionDto = _connectionManager.GetConnectionDtoById(tableInputDto.ConnectionId, true);
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

            var tablename = tableInputDto.TableName;
            if (string.IsNullOrEmpty(tablename))
            {
                return new JavaScriptResult("请选择表!");
            }

            #endregion

            #region 获取表字段结构信息

            string getTableFieldSql = string.Format(getflieds, tablename, connectionDto.Db);
            List<TableField> dataList = await _db.Ado.SqlQueryAsync<TableField>(getTableFieldSql);
            var totalCount = dataList.Count();

            #endregion

            #region 拼接class 类字符串

            StringBuilder sb = new StringBuilder();
            sb.Append("public class " + tablename);
            sb.Append("\r{");
            for (int i = 0; i < totalCount; i++)
            {
                //var fliedname = dataList[i].ColumnName;
                //var fliedtype = dataList[i].DATA_TYPE;
                //var info = dt.Rows[i]["info"].ToString();
                var fliedname = dataList[i].ColumnName;
                var fliedtype = dataList[i].DATA_TYPE;
                var comment = dataList[i].Comment;

                #region 参数 字段属性

                if (!string.IsNullOrWhiteSpace(comment))
                {
                    sb.Append("\r\t/// <summary>");
                    sb.Append("\r\t/// " + comment);
                    sb.Append("\r\t/// </summary>");
                }

                sb.Append("\r\tpublic " + MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname +
                          "{ get; set; }\n");

                #endregion
            }

            sb.Append("\r}");

            #endregion

            //txt_content.Text = sb.ToString();

            return Content(sb.ToString());
        }

        #endregion

        /// <summary>
        /// 生成c#代码
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

            if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_list" && c.IsChecked == true))
            {
                //return await GetListAsync(generateCodeInputDto);
                codeBuilder.Append(((ContentResult)await GetListAsync(generateCodeInputDto)).Content ?? "");
            }

            if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_pagelist" && c.IsChecked == true))
            {
                //GetPageList();
                codeBuilder.Append(((ContentResult)await GetPageListAsync(generateCodeInputDto)).Content ?? "");
            }

            if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_update" && c.IsChecked == true))
            {
                //Update();
                codeBuilder.Append(((ContentResult)await UpdateAsync(generateCodeInputDto)).Content ?? "");
            }

            if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_delete" && c.IsChecked == true))
            {
                //Delete();
                codeBuilder.Append(((ContentResult)await DeleteAsync(generateCodeInputDto)).Content ?? "");
            }

            if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_add" && c.IsChecked == true))
            {
                // Insert();
                codeBuilder.Append(((ContentResult)await InsertAsync(generateCodeInputDto)).Content ?? "");
            }

            if (generateCodeInputDto.MethodList.Any(c => c.CheckName == "cb_getmodel" && c.IsChecked == true))
            {
                //GetModel();
                codeBuilder.Append(((ContentResult)await GetModel(generateCodeInputDto)).Content ?? "");
            }

            //ReaderBind();
            return Content(codeBuilder.ToString());
        }

       

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
            #endregion

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
                    #endregion

                    #region 生成文件到指定路径
                    //写
                    using (StreamWriter w = System.IO.File.AppendText(path))
                    {
                        StringBuilder sb = new StringBuilder();
                        #region 获取表字段结构信息
                        List<TableField> dataList = await _db.Ado.SqlQueryAsync<TableField>(string.Format(getflieds, tablename, connectionDto.Db));
                        totalCount = dataList.Count();
                        if (totalCount<=0)
                        {
                            return Content("NoData");
                        }
                        #endregion
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

                            #endregion
                        }

                        sb.Append("\r\t}");
                        sb.Append("\r}");

                        #endregion

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

        //-----------------------------------------------------------------------------
        //gv_fileds

        //public static string ConvertGVToHTML(GridView gv)
        //{
        //    string html = "<table>";
        //    //add header row
        //    html += "<tr>";
        //    for (int i = 0; i < gv.Columns.Count; i++)
        //        html += "<td>" + gv.Columns[i].HeaderText + "</td>";
        //    html += "</tr>";
        //    //add rows
        //    for (int i = 0; i < gv.Rows.Count; i++)
        //    {
        //        html += "<tr>";
        //        for (int j = 0; j < gv.Columns.Count; j++)
        //            html += "<td>" + gv.Rows[i].Cells[j].Text.ToString() + "</td>";
        //        html += "</tr>";
        //    }
        //    html += "</table>";
        //    return html;

        //    for (int i = 0; i < gv.Rows.Count; i++)
        //    {
        //        html += "<tr>";

        //        for (int j = 0; j < gv.Columns.Count; j++)
        //        {
        //            TableCell cell = gv.Rows[i].Cells[j];

        //            html += "<td>";

        //            if (cell.Controls.Count > 1 && cell.Controls[1] is Label)
        //            {
        //                Label lblValue = cell.Controls[1] as Label;
        //                html += lblValue.Text;
        //            }
        //            else
        //            {
        //                html += cell.Text;
        //            }

        //            html += "</td>";
        //        }

        //        html += "</tr>";
        //    }
        //}
    }
}