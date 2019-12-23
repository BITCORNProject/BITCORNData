using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BITCORNService.Models;
using Microsoft.EntityFrameworkCore;

namespace BITCORNService.Reflection
{
    public static class UserReflection
    {
        public static Type[] UserModel = new Type[] {
            typeof(User),
            typeof(UserIdentity),
            typeof(UserStat),
            typeof(UserWallet)
        };
        public static Dictionary<string, Type> ColumnToTable { get; private set; }
        public static Dictionary<Type, List<string>> TableColumns { get; private set; }
        static void CacheReflection()
        {
            if (ColumnToTable == null)
            {
                ColumnToTable = new Dictionary<string, Type>();
                TableColumns = new Dictionary<Type, List<string>>();
                HashSet<string> closedColumns = new HashSet<string>();
                foreach (var model in UserModel)
                {
                    var properties = model.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    var columns = new List<string>();
                    foreach (var property in properties)
                    {
                        if (property.PropertyType.IsPrimitive ||
                            property.PropertyType == typeof(string) ||
                            property.PropertyType == typeof(int?) ||
                            property.PropertyType == typeof(decimal?))
                        {
                            string name = property.Name.ToLower();
                            if (!closedColumns.Contains(name))
                            {
                                ColumnToTable.Add(name, model);
                                columns.Add(name);
                                closedColumns.Add(name);
                            }
                        }
                    }
                    TableColumns.Add(model, columns);
                }
            }
        }
        public static async Task<Dictionary<int,Dictionary<string, object>>> GetColumns(BitcornContext dbContext, string[] columns, int[] primaryKeys)
        {

            CacheReflection();
            string[] validColumns;
            if (columns.Length == 1 && columns[0] == "*")
            {
                validColumns = TableColumns.SelectMany(u => u.Value).ToArray();
            }
            else
            {
                var list = columns.Where(c => ColumnToTable.ContainsKey(c)).Distinct().ToList();
                string userIdKey = nameof(User.UserId).ToLower();
                if (!list.Contains(userIdKey))
                {
                    list.Add(userIdKey);
                }
                validColumns = list.ToArray();
            }
            if (validColumns.Length > 0)
            {
                string sql = GenerateSql(validColumns, primaryKeys);
                return await RawSqlQuery(dbContext, sql);
            }
            return null;
        }
        static string GenerateSql(string[] columns, int[] primaryKeyValues)
        {
            StringBuilder sql = new StringBuilder();
            List<string> uniqueTables = new List<string>();
            sql.Append("SELECT ");
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                {
                    sql.Append(',');
                }
                string column = columns[i];
                string table = ColumnToTable[column].Name;
                if (!uniqueTables.Contains(table))
                {
                    uniqueTables.Add(table);
                }
                sql.Append('[');
                sql.Append(table);
                sql.Append("].");
                sql.Append(column);
                sql.Append(' ');

            }
            var firstTable = uniqueTables[0];
            var selectFirstTable = "[" + firstTable + "]";
            
            sql.Append(" FROM ");
            sql.Append(selectFirstTable);
            sql.Append(' ');
            string primaryKeyName = nameof(User.UserId);
            if (uniqueTables.Count > 1)
            {
                for (int i = 1; i < uniqueTables.Count; i++)
                {
                    sql.Append(" JOIN ");
                    string selfSelect = "[" + uniqueTables[i] + "]";

                    sql.Append(selfSelect);
                    sql.Append(" on ");
                    sql.Append(selectFirstTable);
                    sql.Append('.');
                    sql.Append(primaryKeyName);
                    sql.Append('=');
                    sql.Append(selfSelect);
                    sql.Append('.');
                    sql.Append(primaryKeyName);
                    sql.Append(' ');
                }
            }
            sql.Append(" Where ");
            sql.Append(selectFirstTable);
            sql.Append('.');
            sql.Append(primaryKeyName);
            if (primaryKeyValues.Length == 1)
            {
                sql.Append("=");
                sql.Append(primaryKeyValues[0]);
            }
            else
            {
                sql.Append(" in (");
                sql.Append(string.Join(',', primaryKeyValues));
                sql.Append(")");
            }
            return sql.ToString();
        }
        public static async Task<Dictionary<int,Dictionary<string, object>>> RawSqlQuery(BitcornContext dbContext, string query)
        {
            using (var command = dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                dbContext.Database.OpenConnection();
               Dictionary<int,Dictionary<string, object>> output = new Dictionary<int,Dictionary<string, object>>();
                using (var result = await command.ExecuteReaderAsync())
                {

                    while (result.Read())
                    {
                        var table = new Dictionary<string, object>();
                        for (int i = 0; i < result.FieldCount; i++)
                        {
                            var name = result.GetName(i);
                            var value = result.GetValue(i);
                            table.Add(name, value);
                        }
                    
                        output.Add((int)table[nameof(User.UserId).ToLower()], table);
                    }
                }
                dbContext.Database.CloseConnection();
                return output;
            }

        }
    }
}
