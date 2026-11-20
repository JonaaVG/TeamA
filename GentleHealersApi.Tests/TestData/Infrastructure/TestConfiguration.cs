using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace GentleHealersApi.Tests.TestData
{
    [TestClass]
    public static class TestConfiguration
    {
        public static IConfiguration Configuration { get; private set; } = null!;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            string basePath = AppContext.BaseDirectory;

            Configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}