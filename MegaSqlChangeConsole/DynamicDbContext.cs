using Microsoft.EntityFrameworkCore;

namespace EFCoreTableScanner
{
    // Динамический контекст базы данных
    public class DynamicDbContext : DbContext
    {
        private readonly string _connectionString;

        public DynamicDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Определяем тип СУБД по строке подключения
            if (_connectionString.Contains("Server=") || _connectionString.Contains("Data Source="))
            {
                optionsBuilder.UseSqlServer(_connectionString);
            }
            //else if (_connectionString.Contains("Host="))
            //{
            //    optionsBuilder.UseNpgsql(_connectionString);
            //}
            //else
            //{
            //    // По умолчанию используем SQLite
            //    optionsBuilder.UseSqlite(_connectionString);
            //}
        }
    }
}