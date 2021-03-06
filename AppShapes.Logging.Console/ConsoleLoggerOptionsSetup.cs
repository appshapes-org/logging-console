﻿using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace AppShapes.Logging.Console
{
    public class ConsoleLoggerOptionsSetup : ConfigureFromConfigurationOptions<ConsoleLoggerOptions>
    {
        public ConsoleLoggerOptionsSetup(ILoggerProviderConfiguration<ConsoleLoggerManager> providerConfiguration) : base(providerConfiguration?.Configuration)
        {
        }
    }
}