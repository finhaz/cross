using Microsoft.Data.Sqlite;
using cross.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SQLite;
using System.Windows;
using ThingLing.Controls;

namespace SomeNameSpace
{
    public class DB_SQLlite : IDatabaseOperation
    {
        // 单例模式（保持不变）
        //private DB_SQLlite() { }
        //private static readonly Lazy<DB_SQLlite> _instance = new Lazy<DB_SQLlite>(() => new DB_SQLlite());
        //public static DB_SQLlite Instance => _instance.Value;
        /// <summary>
        /// 操作sqlite数据库(.NET Core)
        /// </summary>
        /// 
        // 定义连接字符串
        // 请根据项目需求手动更改连接字符串, 本例中将example.db文件在生成时复制到了输出目录
        // 因此DBQ后面写的是

        //public static string ConnString = "Driver={Microsoft Access Driver (*.mdb, *.accdb)};DBQ=./MOON.db;";
        public static string ConnString ;

        //public readonly string ConnString;

        //public Data_r[] data = new Data_r[200];
        public int u = 0;
        public int j = 0;

        //OleDbConnection conn;
        //OleDbCommand cmd;
        //OleDbDataReader dr;

        //OdbcConnection conn;
        //OdbcCommand cmd;
        //OdbcDataReader dr;

        string[] error1 = new string[16];
        //public int runnum;
        int knum = 0;//记录存在多少条PRUN记录
        int num_pso = 0;//记录存在多少条PSO记录
        int UG_Num = 0;

        public DB_SQLlite(string connectionString)
        {
            ConnString = connectionString;
        }

        /// <summary>
        /// 获取数据库中所有表名
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllTableNames()
        {
            List<string> result = new List<string>();
            using (SQLiteConnection conn = new SQLiteConnection(ConnString))
            {
                try
                {
                    conn.Open();
                    DataTable tbs = conn.GetSchema("tables");
                    foreach (DataRow dr in tbs.AsEnumerable().Where(x => x["TABLE_TYPE"].ToString() == "TABLE"))
                    {
                        result.Add(dr["TABLE_NAME"].ToString());
                    }
                }
                catch (Exception e)
                {
                    MessageBox.ShowAsync(e.Message);
                }
            }
            return result;
        }

        /// <summary>
        /// 更新数据库中表，实际上可以说是替换,若表不存在将自动创建
        /// </summary>
        /// <param name="dt">表</param>
        /// <param name="tableName">表名</param>
        public void UpdateDBTable(System.Data.DataTable dt, string tableName)
        {
            if (!IsTableExist(tableName))
            {
                CreatDBTable(dt, tableName);
            }
            using SQLiteConnection conn = new SQLiteConnection(ConnString);
            try
            {
                conn.Open();
                string sql = $"delete from {tableName}";
                SQLiteCommand odc = new SQLiteCommand(sql, conn);
                odc.ExecuteNonQuery();
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string add_sql = "";
                    if (dt.Columns.Contains("ID"))
                    {
                        add_sql = $"Insert Into {tableName} Values('{i + 1}'";
                    }
                    else
                    {
                        add_sql = $"Insert Into {tableName} Values('{dt.Rows[i].ItemArray[0]}'";
                    }
                    for (int j = 1; j < dt.Columns.Count; j++)
                    {
                        add_sql += $",'{dt.Rows[i].ItemArray[j]}'";
                    }
                    add_sql += ")";
                    odc.CommandText = add_sql;
                    odc.Connection = conn;
                    odc.ExecuteNonQuery();
                }
                odc.Dispose();
            }
            catch (Exception e)
            {
                if (e.Message.Contains("查询值的数目与目标字段中的数目不同"))
                {
                    DeleteDBTable(tableName);
                    UpdateDBTable(dt, tableName);
                }
                else
                {
                    MessageBox.ShowAsync(e.Message);
                }
            }
        }

        /// <summary>
        /// 新建数据表
        /// </summary>
        /// <param name="dt">表</param>
        /// <param name="tableName">表名</param>
        public bool CreatDBTable(System.Data.DataTable dt, string tableName)
        {
            if (IsTableExist(tableName))
            {
                Console.WriteLine("表已存在，请检查名称！");
                return false;
            }
            else
            {
                try
                {
                    using SQLiteConnection conn = new SQLiteConnection(ConnString);
                    conn.Open();
                    //构建字段组合
                    string StableColumn = "";
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        Type t = dt.Columns[i].DataType;
                        if (t.Name == "String")
                        {
                            StableColumn += string.Format("{0} varchar", dt.Columns[i].ColumnName);
                        }
                        else if (t.Name == "Int32" || t.Name == "Double")
                        {
                            StableColumn += string.Format("{0} int", dt.Columns[i].ColumnName);
                        }
                        if (i != dt.Columns.Count - 1)
                        {
                            StableColumn += ",";
                        }
                    }
                    string sql = "";
                    if (StableColumn.Contains("ID int"))
                    {
                        StableColumn = StableColumn.Replace("ID int,", "");
                        sql = $"create table {tableName}(ID autoincrement primary key,{StableColumn}";
                    }
                    else
                    {
                        sql = $"create table {tableName}({StableColumn})";
                    }
                    SQLiteCommand odc = new SQLiteCommand(sql, conn);
                    odc.ExecuteNonQuery();
                    odc.Dispose();
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.ShowAsync(e.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// 删除数据库中表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public bool DeleteDBTable(string tableName)
        {
            if (IsTableExist(tableName))
            {
                using SQLiteConnection conn = new SQLiteConnection(ConnString);
                try
                {
                    conn.Open();
                    string sql = $"drop table {tableName}";
                    SQLiteCommand odc = new SQLiteCommand(sql, conn);
                    odc.ExecuteNonQuery();
                    odc.Dispose();
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.ShowAsync(e.Message);
                    return false;
                }
            }
            else { return false; }
        }

        /// <summary>
        /// 查询表是否存在
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public  bool IsTableExist(string tableName)
        {
            using SQLiteConnection conn = new SQLiteConnection(ConnString);
            try
            {
                conn.Open();
                string sql = $"select * from {tableName}";
                SQLiteCommand odc = new SQLiteCommand(sql, conn);
                odc.ExecuteNonQuery();
                odc.Dispose();
                return true;
            }
            catch (Exception)
            {
                MessageBox.ShowAsync($"数据库中不存在表: {tableName}");
                return false;
            }
        }

        /// <summary>
        /// 获取数据库中表
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public DataTable GetDBTable(string tableName)
        {
            if (!IsTableExist(tableName))
            {
                return null;
            }
            using SQLiteConnection conn = new SQLiteConnection(ConnString);
            try
            {
                conn.Open();
                string sql = $"select * from {tableName}";
                DataTable dt = new DataTable();
                //OdbcDataAdapter oda = new OdbcDataAdapter(sql, conn);
                SQLiteDataAdapter oda = new SQLiteDataAdapter(sql, conn);
                oda.Fill(dt);
                oda.Dispose();
                dt.TableName = tableName;
                return dt;
            }
            catch (Exception e)
            {
                MessageBox.ShowAsync(e.Message);
                return null;
            }
        }


        public void DataBase_SET_Save(string table, float set_num, byte tempsn)
        {
            using SQLiteConnection conn = new SQLiteConnection(ConnString);

            conn.Open();

            string sql = "update " + table + " set [VALUE]=" + set_num + " where SN=" + tempsn;


            SQLiteCommand odc = new SQLiteCommand(sql, conn);
            odc.ExecuteNonQuery();
            odc.Dispose();
        }


        static void create_table(string dbfile)
        {
            //string dbfile = @"URI=file:sql.db";
            SqliteConnection cnn = new SqliteConnection(dbfile);
            cnn.Open();

            string sql = "Create table Person (Id integer primary key, Name text);";
            SqliteCommand cmd = new SqliteCommand(sql, cnn);
            cmd.ExecuteNonQuery();
            cnn.Close();
        }

        void addtableline(string dbfile)
        {
            //string dbfile = @"URI=file:sql.db";
            SqliteConnection cnn = new SqliteConnection(dbfile);
            cnn.Open();

            string sql = "insert into  Person (Id , Name) values(1,'Mike');";
            SqliteCommand cmd = new SqliteCommand(sql, cnn);
            cmd.ExecuteNonQuery();
            cnn.Close();

            Console.WriteLine("Insert row OK");

        }

        void find_data(string dbfile)
        {
            //string dbfile = @"URI=file:sql.db";

            SqliteConnection cnn = new SqliteConnection(dbfile);

            cnn.Open();

            string sql = "Select * From  Person";

            SqliteCommand cmd = new SqliteCommand(sql, cnn);

            SqliteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())

            {

                Console.WriteLine($"{reader.GetInt32(0)}  {reader.GetString(1)} ");

            }

            reader.Close();

            cnn.Close();


        }


        void update_data(string dbfile)
        {
            //string dbfile = @"URI=file:sql.db";
            SqliteConnection cnn = new SqliteConnection(dbfile);
            cnn.Open();

            string sql = "update  Person set Name='Jim jones' where id=1;";
            SqliteCommand cmd = new SqliteCommand(sql, cnn);
            cmd.ExecuteNonQuery();
            cnn.Close();

            Console.WriteLine("Update row OK");

        }


        void delete_data(string dbfile)
        {
            //string dbfile = @"URI=file:sql.db";
            SqliteConnection cnn = new SqliteConnection(dbfile);
            cnn.Open();

            string sql = "delete from  Person where id=3;";
            SqliteCommand cmd = new SqliteCommand(sql, cnn);
            cmd.ExecuteNonQuery();
            cnn.Close();
            Console.WriteLine("Delete row OK");

        }


    }
}