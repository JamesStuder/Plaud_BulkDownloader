using API.Plaud.NET.Interfaces;
using API.Plaud.NET.Services;
using System;
using API.Plaud.NET.Models;
using API.Plaud.NET.Models.Responses;
using CommandLine;
using Plaud.ConsoleApp.BulkDownloader.Models;

namespace Plaud.ConsoleApp.BulkDownloader
{
    internal class Program
    {
        private static IPlaudApiService? _apiService;
        private static UserInput? _userInput;
        private static ResponseListRecordings? _listOfAllRecordings;
        private static ResponseFileTags? _fileTags;
        
        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        /// <remarks>
        /// Command Line Arguments
        /// -u = Plaud Username.
        /// -p = Plaud Username.
        /// -d = Directory to download files too.
        ///
        /// Environment Variables:
        /// PlaudUserName = Plaud Username.
        /// PlaudPassword = Plaud Username.
        /// PlaudDownloadDirectory = Directory to download files too.
        ///
        /// If neither of these items are passed / set then user is prompt to provide the info.
        /// </remarks>
        public static void Main(string[] args)
        {
            _apiService = new PlaudApiService();
            GetUserInputs(args);
            if (_userInput == null || string.IsNullOrEmpty(_userInput.Username) || string.IsNullOrEmpty(_userInput.Password) || string.IsNullOrEmpty(_userInput.Directory))
            {
                Console.WriteLine("Invalid user input. Please try again.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            ResponseAuth authResponse = _apiService.AuthenticateAsync(_userInput.Username, _userInput.Password).Result;

            if (authResponse == null || string.IsNullOrEmpty(authResponse.AccessToken) || string.IsNullOrEmpty(_apiService.AccessToken))
            {
                Console.WriteLine("Unable to authenticate. Please try again.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }
            
            Console.WriteLine("Authenticated successfully.");
            GetNeededData();
        }

        private static void DownloadRecordings()
        {
            if (_listOfAllRecordings?.DataFileList.Count <= 0)
            {
                Console.WriteLine("No recordings to download.");
                return;
            }

            foreach (DataFileList? fileToDownload in _listOfAllRecordings?.DataFileList!)
            {
                
            }
        }
        
        /// <summary>
        /// Retrieves data required for the application's functionality.
        /// </summary>
        /// <remarks>
        /// This method performs the following actions:
        /// 1. Retrieves all recordings available for the authenticated user via the API service.
        /// 2. Retrieves all file tags available from the API service.
        /// Any issues or errors during the retrieval are logged to the console.
        /// Ensure that the API service has been successfully initialized and the user has been authenticated before invoking this method.
        /// </remarks>
        private static void GetNeededData()
        {
            _listOfAllRecordings = _apiService?.GetAllRecordingsAsync().Result;
            Console.WriteLine($"Retrieved {_listOfAllRecordings?.DataFileList.Count} recordings.");

            _fileTags = _apiService?.GetFileTagsAsync().Result;
            Console.WriteLine($"Retrieved {_fileTags?.DataFiletagList.Count} file tags.");
        }
        
        /// <summary>
        /// Parses and retrieves user inputs from command-line arguments or prompts the user for inputs if not provided.
        /// </summary>
        /// <param name="args">An array of command-line arguments containing optional username, password, and directory information.</param>
        /// <remarks>
        /// -u = Plaud Username.
        /// -p = Plaud Password.
        /// -d = Directory to download files to.
        /// If any argument is missing, the method uses environment variables or prompts the user interactively.
        /// </remarks>
        private static void GetUserInputs(string[] args)
        {
            _userInput = new UserInput();
            Parser.Default.ParseArguments<UserInput>(args)
                .WithParsed(ui =>
                {
                    ui.Username = !string.IsNullOrWhiteSpace(ui.Username) ? ui.Username : Environment.GetEnvironmentVariable("PlaudUserName", EnvironmentVariableTarget.User) ?? Prompt("Enter username: ");
                    ui.Password = !string.IsNullOrWhiteSpace(ui.Password) ? ui.Password : Environment.GetEnvironmentVariable("PlaudPassword", EnvironmentVariableTarget.User) ?? Prompt("Enter password: ");
                    ui.Directory = !string.IsNullOrWhiteSpace(ui.Directory) ? ui.Directory : Environment.GetEnvironmentVariable("PlaudDownloadDirectory", EnvironmentVariableTarget.User) ?? Prompt("Enter directory: ");
                    
                    Console.WriteLine($"Username: {ui.Username}");
                    Console.WriteLine($"Password: {ui.Password}");
                    Console.WriteLine($"Directory: {ui.Directory}");
                    _userInput = ui;
                })
                .WithNotParsed(errors =>
                {
                    _userInput.Username = Prompt("Enter username: ");
                    _userInput.Password = Prompt("Enter password: ");
                    _userInput.Directory = Prompt("Enter directory: ");
                });
        }

        /// <summary>
        /// Prompt user for input and return the input
        /// </summary>
        private static string? Prompt(string promptText)
        {
            Console.WriteLine(promptText);
            return Console.ReadLine();
        }
    }
}