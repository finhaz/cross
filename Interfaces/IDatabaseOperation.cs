using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cross.Interfaces
{
    public interface IDatabaseOperation
    {
        /// <summary>
        /// 检查指定表是否存在
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>存在返回true，否则false</returns>
        bool IsTableExist(string tableName);

        public DataTable GetDBTable(string tableName);

        public bool DeleteDBTable(string tableName);

        public bool CreatDBTable(System.Data.DataTable dt, string tableName);

        public void UpdateDBTable(System.Data.DataTable dt, string tableName);
        public void DataBase_SET_Save(string table, float set_num, byte tempsn);

        // 扩展：添加其他通用数据库操作（按需补充）
        // bool ExecuteNonQuery(string sql);
        // DataTable Query(string sql);
        // void Connect();
        // void Disconnect();

    }
}
