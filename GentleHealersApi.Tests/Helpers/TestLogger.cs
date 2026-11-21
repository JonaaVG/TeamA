using Serilog;
using System.Reflection;

namespace GentleHealersApi.Tests.Helpers
{
    public class TestLogger
    {
        private readonly string _logFilePath;
        private readonly Serilog.Core.Logger _logger;

        private static readonly string _runFolder = CreateRunFolder();

        public TestLogger(
            TestContext testContext,
            Type testClassType)
        {
            string testName = testContext.TestName;

            var categories = GetTestCategories(
                testName,
                testClassType
            );

            var testCaseId = categories
                .FirstOrDefault(category =>
                    category.StartsWith("TC-"));

            string fileName;

            if (!string.IsNullOrWhiteSpace(testCaseId))
            {
                fileName = $"{testCaseId}_{testName}.log";
            }
            else
            {
                fileName = $"{testName}.log";
            }

            _logFilePath = Path.Combine(
                _runFolder,
                fileName
            );

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(_logFilePath)
                .CreateLogger();

            Info("Test gestart");
            Info($"Datum/tijd: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Info("Softwareversie: 1.0.0");

            if (categories.Count > 0)
            {
                Info(
                    $"Testcategorieën: {string.Join(", ", categories)}"
                );
            }
            else
            {
                Info("Geen testcategorieën gevonden.");
            }
        }

        private static List<string> GetTestCategories(
            string testName,
            Type testClassType)
        {
            var testMethod = testClassType
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                )
                .FirstOrDefault(method =>
                    method.Name.Equals(
                        testName,
                        StringComparison.Ordinal
                    )
                );

            if (testMethod == null)
            {
                return new List<string>();
            }

            var attributes = testMethod.GetCustomAttributes(
                typeof(TestCategoryAttribute),
                inherit: true
            );

            var categories = new List<string>();

            foreach (var attribute in attributes)
            {
                var fields = attribute
                    .GetType()
                    .GetFields(
                        BindingFlags.Instance |
                        BindingFlags.NonPublic |
                        BindingFlags.Public
                    );

                foreach (var field in fields)
                {
                    var value = field.GetValue(attribute);

                    if (value is IEnumerable<string> values)
                    {
                        categories.AddRange(values);
                    }
                    else if (value is string valueString)
                    {
                        categories.Add(valueString);
                    }
                }
            }

            return categories;
        }

        private static string CreateRunFolder()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            // 1. Pak de map waar de app draait (bin/Debug/net10.0)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // 2. Ga 3 mappen omhoog om in de root van je testproject te komen
            // (net10.0 -> Debug -> bin -> LU2-software-testen-week7.Tests)
            var projectDirInfo = new DirectoryInfo(baseDir);
            for (int i = 0; i < 3 && projectDirInfo.Parent != null; i++)
            {
                projectDirInfo = projectDirInfo.Parent;
            }

            // 3. Selecteer de juiste map binnen het project
            string testLogsRoot = Path.Combine(projectDirInfo.FullName, "TestLogs");

            // 4. Maak een unieke submap op basis van de timestamp (bijv. 2026-10-10_18-00-00)
            string folder = Path.Combine(testLogsRoot, timestamp);

            Directory.CreateDirectory(folder);

            return folder;
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Info(string message)
        {
            _logger.Information(message);
        }

        public void Warn(string message)
        {
            _logger.Warning(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(Exception ex, string message)
        {
            _logger.Error(ex, message);
        }

        public void LogResult(bool passed)
        {
            if (passed)
            {
                Info("RESULTAAT: PASS");
            }
            else
            {
                Error("RESULTAAT: FAIL");
            }
        }

        public void Close()
        {
            _logger.Dispose();

            if (File.Exists(_logFilePath))
            {
                File.SetAttributes(
                    _logFilePath,
                    File.GetAttributes(_logFilePath) | FileAttributes.ReadOnly
                );
            }
        }
    }
}