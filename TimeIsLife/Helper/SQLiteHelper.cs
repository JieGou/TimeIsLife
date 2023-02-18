using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace TimeIsLife.Helper
{
    public class SQLiteHelper
    {
        private readonly string connectionString;

        public SQLiteHelper(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IDbConnection GetConnection()
        {
            return new SQLiteConnection(connectionString);
        }

        public IEnumerable<T> Query<T>(string sql, object param = null)
        {
            using (var connection = GetConnection())
            {
                return connection.Query<T>(sql, param);
            }
        }

        public int Execute(string sql, object param = null)
        {
            using (var connection = GetConnection())
            {
                return connection.Execute(sql, param);
            }
        }

        public T Insert<T>(string sql, object param = null)
        {
            using (var connection = GetConnection())
            {
                connection.Execute(sql, param);
                return connection.Query<T>("SELECT last_insert_rowid()", param).Single();
            }
        }

        public int Update(string sql, object param = null)
        {
            return Execute(sql, param);
        }

        public int Delete(string sql, object param = null)
        {
            return Execute(sql, param);
        }
    }
}
