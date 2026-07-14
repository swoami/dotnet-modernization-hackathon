using System;
using Microsoft.Extensions.Configuration;

namespace ContosoInsurance.Common.Config
{
    /// <summary>
    /// Access to appsettings.json and environment variables via IConfiguration.
    /// </summary>
    public static class ConfigHelper
    {
        private static readonly Lazy<IConfiguration> _configuration = new Lazy<IConfiguration>(CreateConfiguration);

        public static string GetSetting(string key, string defaultValue = null)
        {
            var value = Configuration["AppSettings:" + key];
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static string GetConnectionString(string name)
        {
            var connectionString = Configuration.GetConnectionString(name);
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException(
                    "Missing connection string '" + name + "'. Check appsettings.json.");
            return connectionString;
        }

        public static int GetInt(string key, int defaultValue)
        {
            var raw = GetSetting(key);
            return int.TryParse(raw, out var v) ? v : defaultValue;
        }

        private static IConfiguration Configuration => _configuration.Value;

        private static IConfiguration CreateConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
