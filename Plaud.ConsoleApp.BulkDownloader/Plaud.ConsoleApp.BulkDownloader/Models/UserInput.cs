using CommandLine;

namespace Plaud.ConsoleApp.BulkDownloader.Models
{
    public class UserInput
    {
        [Option('u', "username", Required = false, HelpText = "Username for the user.")]
        public string? Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "Password for the user.")]
        public string? Password { get; set; }

        [Option('d', "directory", Required = false, HelpText = "Directory to download files to.")]
        public string? Directory { get; set; }

        [Option('s', "startdate", Required = false, HelpText = "Only download recordings after this date (format: yyyy-MM-dd).")]
        public string? StartDate { get; set; }
    }
}