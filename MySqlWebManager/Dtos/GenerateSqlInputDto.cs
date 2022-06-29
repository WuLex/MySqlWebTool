namespace MySqlWebManager.Dtos
{
    public class GenerateCodeInputDto
    {

        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 数据库连接Id
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// 生成方法名
        /// </summary>
        public List<CheckInfoDto> MethodList { get; set; }

        /// <summary>
        /// 条件字段
        /// </summary>
        public List<string> QueryConditionList { get; set; }

        /// <summary>
        /// 查询字段
        /// </summary>
        public List<string> QueryFieldList { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public List<string> QueryOrderList { get; set; }

    }
}
