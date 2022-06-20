namespace MySqlWebManager.Common
{
    public class MysqlCommonHelper
    {
        public MysqlCommonHelper()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }
        public static string GetSqlType(string type)
        {
            switch (type)
            {
                case "int":
                    return "MySqlDbType.Int32";
                    break;
                case "bigint":
                    return "MySqlDbType.Int64";
                    break;
                case "varchar":
                case "char":
                    return "MySqlDbType.VarChar";
                    break;
                case "nvarchar":
                    return "MySqlDbType.VarChar";
                    break;
                case "datetime":
                case "timestamp":
                    return "MySqlDbType.DateTime";
                    break;
                case "date":
                    return "MySqlDbType.Date";
                    break;
                case "text":
                    return "MySqlDbType.Text";
                    break;
                case "bit":
                    return "MySqlDbType.Bit";
                    break;
                case "byte":
                    return "MySqlDbType.Byte";
                    break;
                case "double":
                    return "MySqlDbType.Double";
                    break;
                case "decimal":
                    return "MySqlDbType.Decimal";
                    break;
                case "float":
                    return "MySqlDbType.Float";
                    break;
                case "tinyint":
                case "smallint":
                    return "MySqlDbType.Int16";
                    break;
                default:
                    return "unknow";
                    break;
            }
        }

        public static string GetFiledType(string type)
        {
            switch (type)
            {
                case "tinyint":
                    return "Int16";
                    break;
                case "smallint":
                    return "short";
                    break;
                case "double":
                    return "double";
                    break;
                case "decimal":
                    return "decimal";
                    break;
                case "float":
                    return "float";
                    break;
                case "bit":
                    return "bool";
                    break;
                case "int":
                    return "int";
                    break;
                case "bigint":
                    return "long";
                    break;
                case "varchar":
                case "char":
                    return "string";
                    break;
                case "nvarchar":
                    return "string";
                    break;
                case "datetime":
                case "timestamp":
                case "date":
                    return "DateTime";
                    break;
                case "text":
                    return "string";
                    break;
                default:
                    return type;
                    break;
            }
        }
    }
}
