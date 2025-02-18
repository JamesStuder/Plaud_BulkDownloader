using CommandLine;

namespace Plaud.ConsoleApp.BulkDownloader.Models
{
    public class UserInput
    {
        [Option('u', "username", Required = false, HelpText = "Username to connect with.")]
        public string? Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "Password for the user.")]
        public string? Password { get; set; }

        [Option('d', "directory", Required = false, HelpText = "Directory to download files to.")]
        public string? Directory { get; set; }
    }
}