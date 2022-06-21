using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using MySqlWebManager.Common;
using System.Data;
using System.Text;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using SqlSugar;

namespace MySqlWebManager.Controllers
{
    public class DatabaseAdminController : Controller
    {
        #region 变量
        private string connStr =
                                "Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};Pooling=False;charset=utf8;" +
                                "MAX Pool Size=2000;Min Pool Size=1;Connection Lifetime=30;";

        private string conn = string.Empty;

        //sql字符串
        private readonly string gettables = "select table_name from information_schema.tables where table_schema='{0}'";
        private readonly string getflieds =
            "select column_name name,data_type type,COLUMN_TYPE,column_comment as info,extra as auto,CHARACTER_MAXIMUM_LENGTH as len " +
            "from INFORMATION_SCHEMA.COLUMNS Where table_name ='{0}' and table_schema ='{1}'";

        public int z = 0;
        #endregion

        #region 依赖注入
        private readonly IConfiguration _configuration;
        private readonly ISqlSugarClient _db; // 核心对象：拥有完整的SqlSugar全部功能
        public DatabaseAdminController(IConfiguration configuration, ISqlSugarClient db)
        {
            _configuration = configuration;
            _db = db;
            conn = _configuration.GetConnectionString("DefaultConnection");
        }
        #endregion

        public IActionResult Index()
        {
            return View();
        }

        

        //protected void Page_Load(object sender, EventArgs e)
        //{
        //    if (!IsPostBack)
        //    {
        //        txt_namespace.Text = "CiWong." + txt_db.Text + ".Entities";
        //        // BindTables();
        //    }
        //}

        #region DB

        public DataTable GetTable(string sql)
        {
            conn = string.Format(connStr, txt_server.Text, txt_db.Text, txt_uid.Text, txt_pwd.Text);
            DataSet ds = MySqlHelper.ExecuteDataset(conn, sql);
            return ds.Tables[0];
        }

        public void ExecuteSql(string sql)
        {
            MySqlHelper.ExecuteNonQuery(conn, sql);
        }

        #endregion

        #region select

        private void SelectAll(StringBuilder Sb, DataTable dt, int count, string tablename, string proname)
        {
            var cb3list = Request.Query["cb3"].ToString();
            if (string.IsNullOrEmpty(cb3list))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择要查询的列！')</script>");
                return;
            }

            string[] arraycb3 = new string[] { };
            arraycb3 = cb3list.Split(',');

            Sb.Append("CREATE OR REPLACE Procedure pro_" + proname + "_" + tablename);
            Sb.Append("\n(\n");
            //
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["column_name"].ToString();
                var fliedtype = dt.Rows[i]["data_type"].ToString();
                var fliedlength = 0; // dt.Rows[i]["data_length"].ToString();
                                     //显示选中

                #region

                if (arraycb3.Any())
                {
                    for (int j = 0; j < arraycb3.Count(); j++)
                    {
                        if (fliedname == arraycb3[j].ToString())
                        {
                            Sb.Append("      _" + fliedname + " out " + fliedtype + "(" + fliedlength + ")");
                            if (j != arraycb3.Count() - 1)
                            {
                                Sb.Append(",\n");
                            }
                        }
                    }
                }

                #endregion
            }

            Sb.Append("\n)\n");
            Sb.Append("AS\n");
            Sb.Append("BEGIN\n");
            Sb.Append("   SELECT ");
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlength = 0; // dt.Rows[i]["data_length"].ToString();
                                     //显示选中

                #region

                if (arraycb3.Any())
                {
                    for (int j = 0; j < arraycb3.Count(); j++)
                    {
                        if (fliedname == arraycb3[j].ToString())
                        {
                            Sb.Append("[" + fliedname + "]");
                            if (j != arraycb3.Count() - 1)
                            {
                                Sb.Append(",");
                            }
                        }
                    }
                }

                #endregion
            }

            Sb.Append(" INTO ");
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlength = 0; // dt.Rows[i]["data_length"].ToString();
                                     //显示选中

                #region

                if (arraycb3.Any())
                {
                    for (int j = 0; j < arraycb3.Count(); j++)
                    {
                        if (fliedname == arraycb3[j].ToString())
                        {
                            Sb.Append("_" + fliedname);
                            if (j != arraycb3.Count() - 1)
                            {
                                Sb.Append(",");
                            }
                        }
                    }
                }

                #endregion
            }

            Sb.Append(" FROM [" + tablename + "]");
        }

        #endregion

        #region bind

        [HttpPost]
        public void BindTables(string txt_db)
        {
            string sql = string.Format(gettables, txt_db);
            lb_tables.DataSource = GetTable(sql);
            lb_tables.DataTextField = "table_name";
            lb_tables.DataValueField = "table_name";
            lb_tables.DataBind();
        }

        public void BindFlieds(string tablename, string txt_db)
        {
            gv_fileds.DataSource = GetTable(string.Format(getflieds, tablename, txt_db));
            gv_fileds.DataBind();
        }

        protected void btnConnection(string txt_db)
        {
            BindTables(txt_db);
        }

        protected void lb_tables_SelectedIndexChanged()
        {
            BindFlieds(lb_tables.SelectedItem.Text);
        }

        #endregion

        #region 添加

        void Insert()
        {
            #region

            StringBuilder Sb = new StringBuilder();
            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;

            #endregion

            Sb.Append("\r\rpublic bool Insert(" + tablename + " model)");
            Sb.Append("\n{\n");

            #region 修改的字段

            string sql = "insert into " + tablename + "(";
            string paras = "";
            for (int i = 0; i < count; i++)
            {
                if (dt.Rows[i]["auto"].ToString() == "auto_increment")
                {
                    continue;
                }

                var fliedname = dt.Rows[i]["name"].ToString();
                sql += fliedname + ",";
                paras += "@" + fliedname + ",";
            }

            sql = sql.TrimEnd(',') + ")";
            paras = paras.TrimEnd(',');
            sql += " values (" + paras + ")";

            #endregion

            Sb.Append("string sql=\"" + sql + "\";");
            Sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            int c = 0;
            string conndetion = "";
            for (int i = 0; i < count; i++)
            {
                if (dt.Rows[i]["auto"].ToString() == "auto_increment")
                {
                    continue;
                }

                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlen = (dt.Rows[i]["len"] ?? "").ToString();

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                Sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                Sb.Append(",");
                conndetion += "\rparameters[" + c++ + "].Value = model." + fliedname + ";";

                #endregion
            }

            string strSb = Sb.ToString().TrimEnd(',');
            Sb = new StringBuilder(strSb);

            #endregion

            Sb.Append("\r\t\t\t\t};");
            Sb.Append(conndetion);
            Sb.Append("\rreturn MysqlCommonHelper.ExecuteNonQuery(connectionString,sql,parameters)>0;");
            Sb.Append("\n}\n");
            txt_content.Text += Sb.ToString();
        }

        #endregion

        #region GetModel

        void GetModel()
        {
            #region

            StringBuilder Sb = new StringBuilder();
            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;

            //得到条件
            var IndexID =Convert.ToString(Request.Query["cb2"]);
            var SelectFlied = Convert.ToString(Request.Query["cb3"]);
            if (string.IsNullOrEmpty(IndexID))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表条件!')</script>");
                return;
            }

            if (string.IsNullOrEmpty(SelectFlied))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表查询字段!')</script>");
                return;
            }

            #endregion

            string[] arrayIndexID = new string[] { };
            string[] arrayFlied = new string[] { };
            arrayIndexID = IndexID.Split(','); //tiaojian
            arrayFlied = SelectFlied.Split(',');

            //Sb.Append("\n\n\n=============Select===============\n");

            #region 条件

            string conndetion = "";
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                #endregion
            }

            conndetion = conndetion.TrimEnd(',');

            #endregion

            Sb.Append("public " + tablename + " GetModel(" + conndetion + ")");
            Sb.Append("\n{\n");
            Sb.Append("var model = new " + tablename + "();\r");

            #region 字段

            string sql = "select ";
            StringBuilder sbBinder = new StringBuilder();
            //sbBinder.Append("\r\t model=new " + tablename + "{");//begin

            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 字段

                for (int j = 0; j < arrayFlied.Count(); j++)
                {
                    if (fliedname == arrayFlied[j].ToString())
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
                            sbBinder.Append("\r\tmodel." + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) + ".Parse(dr[\"" +
                                            fliedname + "\"].ToString());");
                        }

                        if (j != arrayFlied.Count() - 1)
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
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < arrayIndexID.Count() - 1)
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
            Sb.Append("string sql=\"" + sql + "\";");
            Sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlen = (dt.Rows[i]["len"] ?? "").ToString();

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j])
                    {
                        Sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        if (j < arrayIndexID.Count() - 1)
                            Sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + arrayIndexID[j].ToString() + ";";
                    }
                }

                #endregion
            }

            //Sb.Append("\r\t new MySqlParameter(\"@top\", MySqlDbType.Int32)");
            //conndetion += "\rparameters[" + c++ + "].Value = top;";

            #endregion

            Sb.Append("\r\t\t\t\t};");
            Sb.Append(conndetion);
            Sb.Append("\rusing (var dr = MysqlCommonHelper.ExecuteReader(connectionString, sql, parameters))");
            Sb.Append("\r{");
            Sb.Append("\t\rif (dr.Read())");
            Sb.Append("\t\r{");
            //Sb.Append("\t\rlist.Add(ReaderBind(dr));");

            #region read bind

            Sb.Append(sbBinder.ToString());

            #endregion

            Sb.Append("\t\r}");
            Sb.Append("\r}");
            Sb.Append("\rreturn model;");
            Sb.Append("\n}\n");
            txt_content.Text += Sb.ToString();
        }

        #endregion

        #region 列表

        void GetList()
        {
            #region

            StringBuilder Sb = new StringBuilder();
            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;

            //得到条件
            var IndexID =Convert.ToString(Request.Query["cb2"]);
            var SelectFlied = Convert.ToString(Request.Query["cb3"]);
            if (string.IsNullOrEmpty(IndexID))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表条件!')</script>");
                return;
            }

            if (string.IsNullOrEmpty(SelectFlied))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表查询字段!')</script>");
                return;
            }

            #endregion

            string[] arrayIndexID = new string[] { };
            string[] arrayFlied = new string[] { };
            arrayIndexID = IndexID.Split(','); //tiaojian
            arrayFlied = SelectFlied.Split(',');

            //Sb.Append("\n\n\n=============Select===============\n");

            #region 条件

            string conndetion = "";
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                #endregion
            }

            #endregion

            Sb.Append("public IList<" + tablename + "> GetList(" + conndetion + "int top = 10)");
            Sb.Append("\n{\n");
            Sb.Append("IList<" + tablename + "> list= new List<" + tablename + ">();\r");

            #region 字段

            string sql = "select ";
            StringBuilder sbBinder = new StringBuilder();
            sbBinder.Append("\r\tlist.Add(new " + tablename + "(){"); //begin

            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 字段

                for (int j = 0; j < arrayFlied.Count(); j++)
                {
                    if (fliedname == arrayFlied[j].ToString())
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
                            sbBinder.Append("\r\t" + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) + ".Parse(dr[\"" +
                                            fliedname + "\"].ToString())");
                        }

                        if (j != arrayFlied.Count() - 1)
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
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j])
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < arrayIndexID.Count() - 1)
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
            Sb.Append("string sql=\"" + sql + "\";");
            Sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlen = (dt.Rows[i]["len"] ?? "").ToString();

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        Sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        Sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + arrayIndexID[j].ToString() + ";";
                    }
                }

                #endregion
            }

            Sb.Append("\r\t new MySqlParameter(\"@top\", MySqlDbType.Int32)");
            conndetion += "\rparameters[" + c++ + "].Value = top;";

            #endregion

            Sb.Append("\r\t\t\t\t};");
            Sb.Append(conndetion);
            Sb.Append("\rusing (var dr = MysqlCommonHelper.ExecuteReader(connectionString, sql, parameters))");
            Sb.Append("\r{");
            Sb.Append("\t\rwhile (dr.Read())");
            Sb.Append("\t\r{");
            //Sb.Append("\t\rlist.Add(ReaderBind(dr));");

            #region read bind

            Sb.Append(sbBinder.ToString());

            #endregion

            Sb.Append("\t\r}");
            Sb.Append("\r}");
            Sb.Append("\rreturn list;");
            Sb.Append("\n}\n");
            txt_content.Text = Sb.ToString();
        }

        #endregion

        #region 分页

        void GetPageList()
        {
            #region

            StringBuilder Sb = new StringBuilder();
            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;

            //得到条件
            var IndexID =Convert.ToString(Request.Query["cb2"]);
            var SelectFlied = Convert.ToString(Request.Query["cb3"]);
            if (string.IsNullOrEmpty(IndexID))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表条件!')</script>");
                return;
            }

            if (string.IsNullOrEmpty(SelectFlied))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表查询字段!')</script>");
                return;
            }

            #endregion

            string[] arrayIndexID = new string[] { };
            string[] arrayFlied = new string[] { };
            arrayIndexID = IndexID.Split(','); //tiaojian
            arrayFlied = SelectFlied.Split(',');

            Sb.Append("\n\n\n//=============分页===============\n");

            #region 条件

            string conndetion = "";
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                #endregion
            }

            #endregion

            Sb.Append("public IList<" + tablename + "> GetList(out int count," + conndetion +
                      "int pageindex = 1, int pagesize = 10)");
            Sb.Append("\n{\n");
            Sb.Append("IList<" + tablename + "> list= new List<" + tablename + ">();\r");
            Sb.Append("int pagestart = (pageindex - 1)*pagesize;\r");
            //Sb.Append("int pageend = pagestart + pagesize;\r");

            string where = "";


            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    where = " where ";
                }

                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        where += fliedname + "=@" + fliedname;
                        if (j < arrayIndexID.Count() - 1)
                        {
                            where += " and ";
                        }
                    }
                }

                //where = where.Trim(',');

                #endregion
            }

            Sb.Append("string where = \"" + where + "\";\r");
            StringBuilder sbBinder = new StringBuilder();
            sbBinder.Append("\r\tlist.Add(new " + tablename + "(){"); //begin

            #region 字段

            string sql = "select ";
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 字段

                for (int j = 0; j < arrayFlied.Count(); j++)
                {
                    if (fliedname == arrayFlied[j].ToString())
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
                            sbBinder.Append("\r\t" + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) + ".Parse(dr[\"" +
                                            fliedname + "\"].ToString())");
                        }

                        if (j != arrayFlied.Count() - 1)
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

            //sql += " limit @top";
            sql += " limit \"+pagestart+\", \"+pagesize";
            Sb.Append("string sql=\"" + sql + ";");
            Sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlen = (dt.Rows[i]["len"] ?? "").ToString();

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        Sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        if (j < arrayIndexID.Count() - 1)
                            Sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + arrayIndexID[j].ToString() + ";";
                    }
                }

                #endregion
            }

            //Sb.Append("\r\t new MySqlParameter(\"@top\", MySqlDbType.Int32)");
            //conndetion += "\rparameters[" + c++ + "].Value = top;";

            #endregion

            Sb.Append("\r\t\t\t\t};");
            Sb.Append(conndetion);
            Sb.Append("\rusing (var dr = MysqlCommonHelper.ExecuteReader(connectionString, sql, parameters))");
            Sb.Append("\r{");
            Sb.Append("\r\twhile (dr.Read())");
            Sb.Append("\r\t{");
            //Sb.Append("\r\tlist.Add(ReaderBind(dr));");
            Sb.Append(sbBinder.ToString());
            Sb.Append("\r\t}");
            Sb.Append("\r}");
            Sb.Append("\rcount=GetCount(where, parameters);");
            Sb.Append("\rreturn list;");
            Sb.Append("\n}\n");
            txt_content.Text += Sb.ToString();
            GetCount();
        }

        #endregion

        #region 修改

        void Update()
        {
            #region

            StringBuilder Sb = new StringBuilder();
            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;

            //得到条件
            var IndexID = Convert.ToString(Request.Query["cb2"]);
            var SelectFlied = Convert.ToString(Request.Query["cb3"]);
            if (string.IsNullOrEmpty(IndexID))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表条件!')</script>");
                return;
            }

            if (string.IsNullOrEmpty(SelectFlied))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表修改的字段!')</script>");
                return;
            }

            #endregion

            string[] arrayIndexID = new string[] { };
            string[] arrayFlied = new string[] { };
            arrayIndexID = IndexID.Split(','); //tiaojian
            arrayFlied = SelectFlied.Split(',');

            //Sb.Append("\n\n\n=============Select===============\n");

            #region 方法参数

            string conndetion = "";

            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }
                //conndetion = conndetion.TrimEnd(',');

                for (int j = 0; j < arrayFlied.Count(); j++)
                {
                    if (!arrayIndexID.Contains(arrayFlied[j].ToString()))
                    {
                        if (fliedname == arrayFlied[j].ToString())
                        {
                            conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                        }
                    }
                }

                #endregion
            }

            conndetion = conndetion.TrimEnd(',');

            #endregion

            Sb.Append("\r\rpublic bool Update(" + conndetion + ")");
            Sb.Append("\n{\n");

            #region 修改的字段

            string sql = "update  " + tablename + " set ";
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                //var fliedtype = dt.Rows[i]["type"].ToString();

                #region 字段

                for (int j = 0; j < arrayFlied.Count(); j++)
                {
                    if (fliedname == arrayFlied[j].ToString())
                    {
                        sql += fliedname += "=@" + fliedname;
                        if (j != arrayFlied.Count() - 1)
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
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < arrayIndexID.Count() - 1)
                            conndetion += " and ";
                    }
                }

                //conndetion = conndetion.Trim(',');

                #endregion
            }

            sql += conndetion;

            #endregion

            Sb.Append("string sql=\"" + sql + "\";");
            Sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlen = (dt.Rows[i]["len"] ?? "").ToString();

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        Sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        Sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + arrayIndexID[j].ToString() + ";";
                    }
                }

                //conndetion = conndetion.TrimEnd(',');

                for (int j = 0; j < arrayFlied.Count(); j++)
                {
                    if (!arrayIndexID.Contains(arrayFlied[j].ToString()))
                    {
                        if (fliedname == arrayFlied[j].ToString())
                        {
                            Sb.Append(
                                "\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                            Sb.Append(",");
                            conndetion += "\rparameters[" + c++ + "].Value = " + arrayFlied[j].ToString() + ";";
                        }
                    }
                }

                #endregion
            }

            string strSb = Sb.ToString().TrimEnd(',');
            Sb = new StringBuilder(strSb);

            #endregion

            Sb.Append("\r\t\t\t\t};");
            Sb.Append(conndetion);
            Sb.Append("\rreturn MysqlCommonHelper.ExecuteNonQuery(connectionString,sql,parameters)>0;");
            Sb.Append("\n}\n");
            txt_content.Text += Sb.ToString();
        }

        #endregion

        #region 删除

        void Delete()
        {
            #region

            StringBuilder Sb = new StringBuilder();
            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;

            //得到条件
            var IndexID = Convert.ToString(Request.Query["cb2"]);
            var SelectFlied = Convert.ToString(Request.Query["cb3"]);
            if (string.IsNullOrEmpty(IndexID))
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择删除条件!')</script>");
                return;
            }

            #endregion

            string[] arrayIndexID = new string[] { };
            string[] arrayFlied = new string[] { };
            arrayIndexID = IndexID.Split(','); //tiaojian
            arrayFlied = SelectFlied.Split(',');

            //Sb.Append("\n\n\n=============Select===============\n");

            #region 方法参数

            string conndetion = "";

            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + ",";
                    }
                }

                //conndetion = conndetion.TrimEnd(',');

                #endregion
            }

            conndetion = conndetion.TrimEnd(',');

            #endregion

            Sb.Append("\r\rpublic bool Delete(" + conndetion + ")");
            Sb.Append("\n{\n");
            string sql = "delete from  " + tablename + " ";

            #region 条件

            conndetion = "";
            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    conndetion = " where ";
                }

                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();

                #region 参数

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j].ToString())
                    {
                        conndetion += fliedname + "=@" + fliedname;
                        if (j < arrayIndexID.Count() - 1)
                            conndetion += " and ";
                    }
                }

                //conndetion = conndetion.Trim(',');

                #endregion
            }

            sql += conndetion;

            #endregion

            Sb.Append("string sql=\"" + sql + "\";");
            Sb.Append("\rMySqlParameter[] parameters = {");

            #region 条件

            conndetion = "";
            int c = 0;
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var fliedlen = (dt.Rows[i]["len"] ?? "").ToString();

                #region 参数

                string len = "";
                if (!string.IsNullOrWhiteSpace(fliedlen))
                {
                    len += "," + fliedlen;
                }

                for (int j = 0; j < arrayIndexID.Count(); j++)
                {
                    if (fliedname == arrayIndexID[j])
                    {
                        Sb.Append("\r\t new MySqlParameter(\"@" + fliedname + "\", " + MysqlCommonHelper.GetSqlType(fliedtype) + len + ")");
                        Sb.Append(",");
                        conndetion += "\rparameters[" + c++ + "].Value = " + arrayIndexID[j].ToString() + ";";
                    }
                }

                conndetion = conndetion.TrimEnd(',');

                #endregion
            }

            string strSb = Sb.ToString().TrimEnd(',');
            Sb = new StringBuilder(strSb);

            #endregion

            Sb.Append("\r\t\t\t\t};");
            Sb.Append(conndetion);
            Sb.Append("\nreturn MysqlCommonHelper.ExecuteNonQuery(connectionString,sql,parameters)>0;");
            Sb.Append("\n}\n");
            txt_content.Text += Sb.ToString();
        }

        #endregion

        #region count

        void GetCount()
        {
            var tablename = lb_tables.SelectedItem.Text;
            string str = "\rpublic int GetCount(string where ,MySqlParameter[] parameters=null)" +
                         "\r{" +
                         "\r\tstring sql = \"select count(*) from " + tablename + " \" + where;" +
                         "\r\tint rows = Convert.ToInt32(MysqlCommonHelper.ExecuteScalar(connectionString, sql, parameters));" +
                         "\r\treturn rows;" +
                         "\r}";
            txt_content.Text += str;
        }

        #endregion

        #region Bind

        void ReaderBind()
        {
            StringBuilder Sb = new StringBuilder();
            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;
            Sb.Append("\rpublic " + tablename + " ReaderBind(IDataReader dr)");
            Sb.Append("\r{");
            Sb.Append("\r\t//需保证字段不能为null");
            Sb.Append("\r\tvar model = new " + tablename + "();");
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                if (fliedtype.ToLower() == "varchar" || fliedtype.ToLower() == "text" || fliedtype.ToLower() == "char")
                {
                    Sb.Append("\r\tmodel." + fliedname + "=dr[\"" + fliedname + "\"].ToString();");
                }
                else
                {
                    Sb.Append("\r\tmodel." + fliedname + "=" + MysqlCommonHelper.GetFiledType(fliedtype) + ".Parse(dr[\"" + fliedname +
                              "\"].ToString());");
                }
            }

            Sb.Append("\r\treturn model;");
            Sb.Append("\r}");
            txt_content.Text += Sb.ToString();
        }

        #endregion

        #region Model

        void CreateModel()
        {
            if (lb_tables.SelectedItem == null)
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表!')</script>");
                return;
            }

            StringBuilder sb = new StringBuilder();

            #region

            var tablename = lb_tables.SelectedItem.Text;
            var dt = GetTable(string.Format(getflieds, tablename, txt_db.Text));
            var count = dt.Rows.Count;

            #endregion

            sb.Append("public class " + tablename);
            sb.Append("\r{");
            for (int i = 0; i < count; i++)
            {
                var fliedname = dt.Rows[i]["name"].ToString();
                var fliedtype = dt.Rows[i]["type"].ToString();
                var info = dt.Rows[i]["info"].ToString();

                #region 参数

                if (!string.IsNullOrWhiteSpace(info))
                {
                    sb.Append("\r\t/// <summary>");
                    sb.Append("\r\t/// " + info);
                    sb.Append("\r\t/// </summary>");
                }

                sb.Append("\r\tpublic " + MysqlCommonHelper.GetFiledType(fliedtype) + " " + fliedname + "{ get; set; }\n");

                #endregion
            }

            sb.Append("\r}");
            txt_content.Text = sb.ToString();
        }

        #endregion

        protected void Button3_Click(object sender, EventArgs e)
        {
            if (lb_tables.SelectedItem == null)
            {
                Page.RegisterStartupScript("alert", "<script>alert('请选择表!')</script>");
                return;
            }

            txt_content.Text = "";
            if (cb_list.Checked)
                GetList();
            if (cb_pagelist.Checked)
                GetPageList();
            if (cb_update.Checked)
                Update();
            if (cb_delete.Checked)
                Delete();
            if (cb_add.Checked)
                Insert();
            if (cb_getmodel.Checked)
                GetModel();
            ReaderBind();
        }

        protected void Button4_Click(object sender, EventArgs e)
        {
            CreateModel();
        }

        protected void Button2_Click(string txt_namespace, string txt_file, string txt_db)
        {
            string Modelnamespace = txt_namespace;
            string ModelFile = txt_file;

            string sql = string.Format(gettables, txt_db);
            var dt = GetTable(sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string tablename = dt.Rows[i]["table_name"].ToString();
                string wjj = ModelFile + txt_db;
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

                //写
                using (StreamWriter w = System.IO.File.AppendText(path))
                {
                    StringBuilder sb = new StringBuilder();
                    var dt1 = GetTable(string.Format(getflieds, tablename, txt_db));
                    var count = dt1.Rows.Count;

                    #region

                    sb.Append("using System; ");
                    sb.Append("\rnamespace " + Modelnamespace);
                    sb.Append("\r{");
                    sb.Append("\r\tpublic class " + tablename);
                    sb.Append("\r\t{");
                    for (int j = 0; j < count; j++)
                    {
                        var fliedname = dt1.Rows[j]["name"].ToString();
                        var fliedtype = dt1.Rows[j]["type"].ToString();
                        var info = dt1.Rows[j]["info"].ToString();

                        #region 参数

                        if (!string.IsNullOrWhiteSpace(info))
                        {
                            sb.Append("\r\t\t/// <summary>");
                            sb.Append("\r\t\t/// " + info);
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
            }

            Response.Write("ok!");
        }

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