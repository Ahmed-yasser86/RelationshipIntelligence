using Microsoft.Extensions.Logging;
using ServiceContracts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Servicess
{
    public class FileEmailSender : IEmailSender
    {
        private readonly string _outputDirectory;
        private readonly ILogger<FileEmailSender> _logger;

        public FileEmailSender(string outputDirectory, ILogger<FileEmailSender> logger)
        {
            _outputDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.GetTempPath()
                : outputDirectory;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, string textBody)
        {
            Directory.CreateDirectory(_outputDirectory);
            string path = Path.Combine(_outputDirectory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(path, $"<!-- To: {to} | Subject: {subject} -->\n{htmlBody}");
            _logger.LogInformation("Digest email for {To} written to {Path}", to, path);
        }
    }
}
