using System.Configuration;

namespace ContosoInsurance.Common.Config
{
    /// <summary>
    /// Legacy access to Web.config / App.config via System.Configuration.
    /// Reads AppSettings and ConnectionStrings at call-time (no caching, no reload token).
    /// </summary>
    public static class ConfigHelper
    {
        public static string GetSetting(string key, string defaultValue = null)
        {
            var value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        public static string GetConnectionString(string name)
        {
            var cs = ConfigurationManager.ConnectionStrings[name];
            if (cs == null)
                throw new ConfigurationErrorsException(
                    "Missing connection string '" + name + "'. Check Web.config / App.config.");
            return cs.ConnectionString;
        }

        public static int GetInt(string key, int defaultValue)
        {
            var raw = GetSetting(key);
            return int.TryParse(raw, out var v) ? v : defaultValue;
        }
    }
}
