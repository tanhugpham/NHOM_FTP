using FileTransfer.Server.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Configuration;

namespace FileTransfer.Server.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<ClientSession> ClientSessions { get; set; }
        public DbSet<TransferHistory> TransferHistories { get; set; }
        public DbSet<FileTransferState> FileTransferStates { get; set; }
        public DbSet<SharedFile> SharedFiles { get; set; }

        public static string BuildConnectionString()
        {
            string server = ConfigurationManager.AppSettings["DbServer"];
            string database = ConfigurationManager.AppSettings["DbName"];
            string username = ConfigurationManager.AppSettings["DbUser"];

            string password = Environment.GetEnvironmentVariable("FT_DB_PASSWORD")
                ?? ConfigurationManager.AppSettings["DbPassword"];

            if (string.IsNullOrWhiteSpace(server))
                throw new InvalidOperationException(
                    "DbServer is not configured in App.config.");

            if (string.IsNullOrWhiteSpace(database))
                throw new InvalidOperationException(
                    "DbName is not configured in App.config.");

            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException(
                    "DbUser is not configured in App.config.");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "Database password is not configured. " +
                    "Set FT_DB_PASSWORD environment variable " +
                    "or add DbPassword to App.config.");

            return "Server=" + server
                + ";Port=3306"
                + ";Database=" + database
                + ";User=" + username
                + ";Password=" + password
                + ";SslMode=None"
                + ";AllowPublicKeyRetrieval=true;";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string connectionString = BuildConnectionString();

            options.UseMySql(connectionString,
                mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });
        }
    }
}
