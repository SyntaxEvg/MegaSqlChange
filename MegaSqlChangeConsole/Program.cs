using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EFCoreTableScanner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Загружаем конфигурацию из appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Получаем строку подключения из конфигурации
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Ошибка: строка подключения не найдена в appsettings.json");
                return;
            }

            // Создаем динамический контекст
            using var dbContext = new DynamicDbContext(connectionString);

            // Получаем все таблицы из базы данных
            var tables = await GetAllTablesAsync(dbContext);

            Console.WriteLine($"Найдено {tables.Count} таблиц в базе данных");

            foreach (var table in tables)
            {
                string tableName = table;
                Console.WriteLine($"Обрабатываем таблицу: {tableName}");

                bool tableModified = false;

                // Получаем все строковые столбцы для данной таблицы
                var stringColumns = await GetStringColumnsAsync(dbContext, tableName);

                if (stringColumns.Count == 0)
                {
                    Console.WriteLine($"  В таблице {tableName} не найдено строковых столбцов");
                    continue;
                }

                // Получаем все записи для данной таблицы
                var rows = await GetTableDataAsync(dbContext, tableName);

                // Получаем имя первичного ключа (если есть)
                var primaryKeyColumn = await GetPrimaryKeyColumnAsync(dbContext, tableName);
                foreach (var row in rows)
                {
                    bool rowModified = false;
                    var updates = new Dictionary<string, string>();

                    string rowId = primaryKeyColumn != null && row.ContainsKey(primaryKeyColumn)
                        ? row[primaryKeyColumn].ToString()
                        : "неизвестный ID";

                    foreach (var column in stringColumns)
                    {
                        if (row.ContainsKey(column) && row[column] != null)
                        {
                            string value = row[column].ToString();
                            
                            if (!string.IsNullOrEmpty(value) && ContainsNonEnglishNonRussianChars(value))
                            {
                                Console.WriteLine($"  Найдены недопустимые символы в столбце '{column}', ID: {rowId}");

                                // Исправляем найденные проблемы
                                string fixedValue = FixInvalidChars(value);
                                updates[column] = fixedValue;

                                Console.WriteLine($"  Исправлено: '{value}' -> '{fixedValue}'");
                                rowModified = true;
                            }
                        }
                    }

                    // Обновляем запись, если были найдены проблемы
                    if (rowModified)
                    {
                        if (primaryKeyColumn != null && row.ContainsKey(primaryKeyColumn))
                        {
                            // Обновление по первичному ключу
                            await UpdateRowAsync(dbContext, tableName, primaryKeyColumn, row[primaryKeyColumn], updates);
                        }
                        else
                        {
                            // Обновление по всем оригинальным значениям (как SSMS)
                            var originalRow = row.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                            await UpdateRowLikeSSMSAsync(dbContext, tableName, originalRow, updates);
                        }

                        tableModified = true;
                    }
                }

                if (tableModified)
                {
                    Console.WriteLine($"Изменения для таблицы {tableName} сохранены");
                }
                else
                {
                    Console.WriteLine($"Нет изменений для таблицы {tableName}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("Обработка всех таблиц завершена");
        }

        // Получение всех таблиц из базы данных
        static async Task<List<string>> GetAllTablesAsync(DynamicDbContext dbContext)
        {
            var tables = new List<string>();

            // SQL-запрос зависит от типа СУБД
            string sql = dbContext.Database.IsSqlServer()
                ? "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'" : null;
                //: dbContext.Database.IsNpgsql()
                //    ? "SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname != 'pg_catalog' AND schemaname != 'information_schema'"
                //    : dbContext.Database.IsSqlite()
                //        ? "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
                //        : "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            await dbContext.Database.OpenConnectionAsync();

            using var result = await command.ExecuteReaderAsync();
            while (await result.ReadAsync())
            {
                tables.Add(result.GetString(0));
            }

            return tables;
        }

        // Получение строковых столбцов для таблицы
        static async Task<List<string>> GetStringColumnsAsync(DynamicDbContext dbContext, string tableName)
        {
            var columns = new List<string>();

            string sql = dbContext.Database.IsSqlServer()
                ? $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND DATA_TYPE IN ('varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext')" :null
                //: dbContext.Database.IsNpgsql()
                //    ? $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}' AND data_type IN ('character varying', 'character', 'text')"
                //    : dbContext.Database.IsSqlite()
                //        ? $"PRAGMA table_info('{tableName}')"
                //        : $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' AND DATA_TYPE LIKE '%char%' OR DATA_TYPE LIKE '%text%'"
                        ;

            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            await dbContext.Database.OpenConnectionAsync();

            using var result = await command.ExecuteReaderAsync();

            //if (dbContext.Database.IsSqlite())
            //{
            //    // Для SQLite обработка PRAGMA table_info
            //    while (await result.ReadAsync())
            //    {
            //        string typeName = result.GetString(2).ToLower();
            //        if (typeName.Contains("char") || typeName.Contains("text") || typeName.Contains("varchar"))
            //        {
            //            columns.Add(result.GetString(1)); // Имя столбца
            //        }
            //    }
            //}
            //else
            //{
                while (await result.ReadAsync())
                {
                    columns.Add(result.GetString(0));
                }
           // }

            return columns;
        }

        // Получение первичного ключа таблицы
        static async Task<string> GetPrimaryKeyColumnAsync(DynamicDbContext dbContext, string tableName)
        {
            string sql = dbContext.Database.IsSqlServer()
                ? $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1 AND TABLE_NAME = '{tableName}'" : null
                //: dbContext.Database.IsNpgsql()
                //    ? $"SELECT a.attname FROM pg_index i JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey) WHERE i.indrelid = '{tableName}'::regclass AND i.indisprimary"
                //    : dbContext.Database.IsSqlite()
                //        ? $"PRAGMA table_info('{tableName}')"
                //        : $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE TABLE_NAME = '{tableName}' AND CONSTRAINT_NAME LIKE '%PK%'"
                        ;

            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            await dbContext.Database.OpenConnectionAsync();

            using var result = await command.ExecuteReaderAsync();
            if (await result.ReadAsync())
            {
                return result.GetString(0);
            }

            return null;
        }

        // Получение данных из таблицы
        static async Task<List<Dictionary<string, object>>> GetTableDataAsync(DynamicDbContext dbContext, string tableName)
        {
            var rows = new List<Dictionary<string, object>>();

            string sql = $"SELECT * FROM {EscapeTableName(dbContext, tableName)}";

            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            await dbContext.Database.OpenConnectionAsync();

            using var result = await command.ExecuteReaderAsync();

            while (await result.ReadAsync())
            {
                var row = new Dictionary<string, object>();

                for (int i = 0; i < result.FieldCount; i++)
                {
                    string columnName = result.GetName(i);
                    object value = result.IsDBNull(i) ? null : result.GetValue(i);
                    row[columnName] = value;
                }

                rows.Add(row);
            }

            return rows;
        }
        public static bool ContainsFrenchOrTurkishChars(string text)
        {
            return Regex.IsMatch(text, @"[éèêëàâùûçîïôœÉÈÊËÀÂÙÛÇÎÏÔŒçğıöşüİĞÖŞÜ]");
        }

        // Обновление записи в таблице через ПМ
        static async Task UpdateRowAsync(DynamicDbContext dbContext, string tableName, string primaryKeyColumn, object primaryKeyValue, Dictionary<string, string> updates)
        {
            if (updates.Count == 0) return;

            var setClause = string.Join(", ", updates.Select(u => $"{EscapeColumnName(dbContext, u.Key)} = @{u.Key}"));
            string sql = $"UPDATE {EscapeTableName(dbContext, tableName)} SET {setClause} WHERE {EscapeColumnName(dbContext, primaryKeyColumn)} = @PrimaryKeyValue";

            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            // Добавляем параметры
            foreach (var update in updates)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@{update.Key}";
                parameter.Value = update.Value;
                command.Parameters.Add(parameter);
            }

            var pkParameter = command.CreateParameter();
            pkParameter.ParameterName = "@PrimaryKeyValue";
            pkParameter.Value = primaryKeyValue;
            command.Parameters.Add(pkParameter);

            await dbContext.Database.OpenConnectionAsync();
            await command.ExecuteNonQueryAsync();
        }
        static async Task UpdateRowLikeSSMSAsync(DbContext dbContext, string tableName, Dictionary<string, object> originalRow, Dictionary<string, string> updatedValues)
        {
            var setClauses = updatedValues.Select(kvp => $"{kvp.Key} = @set_{kvp.Key}").ToList();
            var whereClauses = originalRow.Select(kvp =>
                kvp.Value == null || kvp.Value == DBNull.Value
                    ? $"{kvp.Key} IS NULL"
                    : $"{kvp.Key} = @where_{kvp.Key}"
            ).ToList();

            string sql = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";

            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;

            foreach (var kvp in updatedValues)
            {
                var param = command.CreateParameter();
                param.ParameterName = $"@set_{kvp.Key}";
                param.Value = kvp.Value != null ? kvp.Value : DBNull.Value;
                command.Parameters.Add(param);
            }

            foreach (var kvp in originalRow)
            {
                if (kvp.Value != null && kvp.Value != DBNull.Value)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = $"@where_{kvp.Key}";
                    param.Value = kvp.Value;
                    command.Parameters.Add(param);
                }
            }

            if (command.Connection.State != ConnectionState.Open)
                await command.Connection.OpenAsync();

            int affected = await command.ExecuteNonQueryAsync();
            Console.WriteLine($"Обновлено строк: {affected}");
        }


        // Экранирование имени таблицы в зависимости от СУБД
        static string EscapeTableName(DynamicDbContext dbContext, string tableName)
        {
            return dbContext.Database.IsSqlServer()
                ? $"[{tableName}]" : null
                //: dbContext.Database.IsNpgsql()
                //    ? $"\"{tableName}\""
                //    : $"\"{tableName}\""
                    ;
        }

        // Экранирование имени столбца в зависимости от СУБД
        static string EscapeColumnName(DynamicDbContext dbContext, string columnName)
        {
            return dbContext.Database.IsSqlServer()
                ? $"[{columnName}]" : null;
                //: dbContext.Database.IsNpgsql()
                //    ? $"\"{columnName}\""
                //    : $"\"{columnName}\"";
        }

        // Метод для проверки наличия символов, не являющихся английскими или русскими
        static bool ContainsNonEnglishNonRussianChars(string text)
        {
            // Регулярное выражение для проверки: 
            // - Английские буквы (a-zA-Z)
            // - Русские буквы (а-яА-ЯёЁ)
            // - Цифры (0-9)
            // - Пробелы, знаки пунктуации и другие общие символы
            var regex = new Regex(@"[^a-zA-Zа-яА-ЯёЁ0-9\s\.,;:!?@#$%^&*()_+\-=\[\]{}|\\/<>~`'""]",RegexOptions.Compiled);
            return regex.IsMatch(text);
        }

        // Метод-заглушка для исправления недопустимых символов
        static string FixInvalidChars(string text)
        {
            // Заменяем все символы, не являющиеся английскими или русскими, на пустую строку
            var regex = new Regex(@"[^a-zA-Zа-яА-ЯёЁ0-9\s\.,;:!?@#$%^&*()_+\-=\[\]{}|\\/<>~`'""]");
            return regex.Replace(text, "");
        }
    }
}