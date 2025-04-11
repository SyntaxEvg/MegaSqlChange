using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MegaSqlChangeConsole;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main1(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // Загружаем конфигурацию
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        using var context = new AppDbContext(connectionString);
        var connection = context.Database.GetDbConnection();
        connection.Open();

        var tables = GetAllTables(connection);

        foreach (var table in tables)
        {
            Console.WriteLine($"Таблица: {table.Schema}.{table.Name}");

            var stringColumns = GetStringColumns(connection, table.Schema, table.Name);
            if (stringColumns.Count == 0)
            {
                Console.WriteLine("  Нет строковых колонок.");
                continue;
            }

            var rows = GetTableData(connection, table.Schema, table.Name, stringColumns);
            int updatedCount = 0;

            foreach (var row in rows)
            {
                var id = row["Id"];
                bool hasChanges = false;
                var updates = new Dictionary<string, string>();

                foreach (var column in stringColumns)
                {
                    var value = row[column] as string;
                    if (!string.IsNullOrEmpty(value) && ContainsInvalidCharacters(value))
                    {
                        Console.WriteLine($"  Найдено поле: {column} = {value}");
                        var fixedValue = FixInvalidCharacters(value);
                        updates[column] = fixedValue;
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    UpdateRow(connection, table.Schema, table.Name, updates, id);
                    updatedCount++;
                }
            }

            Console.WriteLine($"  Обновлено строк: {updatedCount}");
        }

        Console.WriteLine("Обработка завершена.");
    }

    static List<(string Schema, string Name)> GetAllTables(IDbConnection connection)
    {
        var result = new List<(string, string)>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT TABLE_SCHEMA, TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE'";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add((reader.GetString(0), reader.GetString(1)));
        }

        return result;
    }

    static List<string> GetStringColumns(IDbConnection connection, string schema, string table)
    {
        var result = new List<string>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT COLUMN_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @table
            AND DATA_TYPE IN ('nvarchar', 'varchar', 'text', 'ntext')";

        var param1 = cmd.CreateParameter();
        param1.ParameterName = "@schema";
        param1.Value = schema;
        cmd.Parameters.Add(param1);

        var param2 = cmd.CreateParameter();
        param2.ParameterName = "@table";
        param2.Value = table;
        cmd.Parameters.Add(param2);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    static List<Dictionary<string, object>> GetTableData(IDbConnection connection, string schema, string table, List<string> columns)
    {
        var result = new List<Dictionary<string, object>>();

        var columnList = string.Join(", ", columns.Select(c => $"[{c}]"));
        var sql = $"SELECT Id, {columnList} FROM [{schema}].[{table}]";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            result.Add(row);
        }

        return result;
    }

    static void UpdateRow(IDbConnection connection, string schema, string table, Dictionary<string, string> updates, object id)
    {
        var setClause = string.Join(", ", updates.Keys.Select(k => $"[{k}] = @{k}"));
        var sql = $"UPDATE [{schema}].[{table}] SET {setClause} WHERE Id = @Id";

        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;

        foreach (var kvp in updates)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = $"@{kvp.Key}";
            param.Value = kvp.Value;
            cmd.Parameters.Add(param);
        }

        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "@Id";
        idParam.Value = id;
        cmd.Parameters.Add(idParam);

        cmd.ExecuteNonQuery();
    }

    static bool ContainsInvalidCharacters(string input)
    {
        return Regex.IsMatch(input, @"[^a-zA-Zа-яА-Я0-9\s]");
    }

    static string FixInvalidCharacters(string input)
    {
        return Regex.Replace(input, @"[^a-zA-Zа-яА-Я0-9\s]", "");
    }
}