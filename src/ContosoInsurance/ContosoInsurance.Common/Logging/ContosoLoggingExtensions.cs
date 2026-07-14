using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContosoInsurance.Common.Logging
{
    public static class ContosoLoggingExtensions
    {
        public static ILoggingBuilder AddContosoLogging(
            this ILoggingBuilder builder,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(configuration);

            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "O ";
            });
            builder.AddApplicationInsights(
                telemetryConfiguration =>
                {
                    var connectionString = configuration["ApplicationInsights:ConnectionString"]
                        ?? configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        telemetryConfiguration.ConnectionString = connectionString;
                    }
                },
                _ => { });

            return builder;
        }
    }
}
