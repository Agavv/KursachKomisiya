using MailKit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MebelShop.Helpers;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace MebelShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Администратор,Директор")]
    public class DatabaseController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly string _backupFolder;

        public DatabaseController(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
            _backupFolder = Path.Combine(_env.ContentRootPath, "Backups");

            if (!Directory.Exists(_backupFolder))
                Directory.CreateDirectory(_backupFolder);
        }

        [HttpGet("list")]
        public IActionResult GetBackupList()
        {
            var files = Directory.GetFiles(_backupFolder, "*.bak")
                .Select(f => new
                {
                    FileName = Path.GetFileName(f),
                    CreatedAt = System.IO.File.GetCreationTime(f),
                    SizeMB = new FileInfo(f).Length / 1024.0 / 1024.0
                })
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            return Ok(files);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                string backupFolder = Path.Combine(_env.ContentRootPath, "Backups");

                if (!Directory.Exists(backupFolder))
                    Directory.CreateDirectory(backupFolder);

                string fileName = $"MebelShop_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.bak";
                string backupPath = Path.Combine(backupFolder, fileName);

                string connectionString = _config.GetConnectionString("con");

                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    connection.Open();

                    string sql = $@"
                BACKUP DATABASE [{databaseName}]
                TO DISK = N'{backupPath}'
                WITH FORMAT, INIT,
                     NAME = N'{databaseName}-FullBackup',
                     SKIP, NOREWIND, NOUNLOAD, STATS = 10;";

                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "✅ Резервная копия успешно создана", fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "❌ Ошибка при создании резервной копии",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteBackup(string fileName)
        {
            try
            {
                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                    return BadRequest(new { message = "Некорректное имя файла" });

                string filePath = Path.Combine(_backupFolder, fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "Файл не найден" });

                System.IO.File.Delete(filePath);

                return Ok(new { message = "Резервная копия удалена успешно" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при удалении резервной копии", error = ex.Message });
            }
        }

        [HttpPost("restore")]
        public async  Task<IActionResult> RestoreBackup([FromBody] RestoreBackupDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.FileName))
                    return BadRequest(new { message = "Имя файла не указано" });

                string backupFile = Path.Combine(_backupFolder, dto.FileName);
                if (!System.IO.File.Exists(backupFile))
                    return NotFound(new { message = "Файл резервной копии не найден" });

                string databaseName = "MebelShop";

                var connectionString = _config.GetConnectionString("con");
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new SqlCommand($@"
                                -- Устанавливаем базу в SINGLE_USER, чтобы закрыть все соединения
                                IF DB_ID(@dbName) IS NOT NULL
                                BEGIN
                                    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                    RESTORE DATABASE [{databaseName}] FROM DISK = @backupFile WITH REPLACE;
                                    ALTER DATABASE [{databaseName}] SET MULTI_USER;
                                END
                            ", connection))
                    {
                        cmd.Parameters.AddWithValue("@backupFile", backupFile);
                        cmd.Parameters.AddWithValue("@dbName", databaseName);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "База данных успешно восстановлена из резервной копии" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при восстановлении базы данных", error = ex.Message });
            }
        }
    }
    public class RestoreBackupDto
    {
        public string FileName { get; set; } = string.Empty;
    }
}
