using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using API.Plaud.NET.Interfaces;
using API.Plaud.NET.Models;
using API.Plaud.NET.Models.Responses;
using API.Plaud.NET.Services;
using CommandLine;
using Plaud.ConsoleApp.ManualDownloader.Models;

namespace Plaud.ConsoleApp.ManualDownloader
{
    internal class Program
    {
        private static IPlaudApiService? _apiService;
        private static UserInput? _userInput;
        private static ResponseListRecordings? _listOfAllRecordings;
        private static ResponseFileTags? _fileTags;
        private static readonly List<string> _selectedTags = new ();
        private static readonly List<FailedDownload> _failedDownloads = new ();

        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        /// <remarks>
        /// Command Line Arguments
        /// -u = Plaud Username.
        /// -p = Plaud Password.
        /// -d = Directory to download files to.
        /// -t = Comma-separated tags to download.
        ///
        /// Environment Variables:
        /// PlaudUserName = Plaud Username.
        /// PlaudPassword = Plaud Password.
        /// PlaudDownloadDirectory = Directory to download files to.
        ///
        /// If neither of these items is passed / set, then the user is prompted to provide the info.
        /// </remarks>
        public static void Main(string[] args)
        {
            _apiService = new PlaudApiService();
            GetUserInputs(args);
            if (_userInput == null || string.IsNullOrEmpty(_userInput.Username) || string.IsNullOrEmpty(_userInput.Password))
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
            SelectTags();

            if (_selectedTags.Count == 0)
            {
                Console.WriteLine("No tags selected. Exiting...");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            GetDownloadDirectory();

            if (string.IsNullOrEmpty(_userInput.Directory))
            {
                Console.WriteLine("No download directory specified. Exiting...");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            SetupFileTagDirectories();
            ProcessFiles();

            if (_failedDownloads.Count > 0)
            {
                WriteFailedDownloadsCsv();
            }

            Console.WriteLine("Download complete.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Creates directory structures for selected file tags.
        /// </summary>
        private static void SetupFileTagDirectories()
        {
            if (!Directory.Exists(_userInput?.Directory))
            {
                Directory.CreateDirectory(_userInput?.Directory!);
            }

            foreach (string tagName in _selectedTags)
            {
                string subDirectory = RemoveInvalidCharactersFromDirectory(tagName);
                string fullTagLocation = Path.Combine(_userInput?.Directory!, subDirectory);
                if (!Directory.Exists(fullTagLocation))
                {
                    Directory.CreateDirectory(fullTagLocation);
                }
            }

            Console.WriteLine("Tag directories setup successfully.");
        }

        /// <summary>
        /// Processes and downloads audio files for recordings matching the selected tags.
        /// </summary>
        private static void ProcessFiles()
        {
            if (!Directory.Exists(_userInput?.Directory))
            {
                Directory.CreateDirectory(_userInput?.Directory!);
            }

            if (_listOfAllRecordings?.DataFileList.Count <= 0)
            {
                Console.WriteLine("No recordings to download.");
                return;
            }

            foreach (DataFileList fileToDownload in _listOfAllRecordings?.DataFileList!)
            {
                // Check if this recording has any of the selected tags
                if (fileToDownload.FiletagIdList.Count == 0)
                    continue;

                var recordingTags = fileToDownload.FiletagIdList
                    .Select(tagId => _fileTags?.DataFiletagList.FirstOrDefault(t => t.Id == tagId)?.Name)
                    .Where(tagName => !string.IsNullOrEmpty(tagName))
                    .ToList();

                var matchingTags = recordingTags.Where(t => _selectedTags.Contains(t!, StringComparer.OrdinalIgnoreCase)).ToList();

                if (matchingTags.Count == 0)
                    continue;

                // Use the first matching tag for directory placement
                string tagName = matchingTags.First()!;
                string tagDirectory = RemoveInvalidCharactersFromDirectory(tagName);
                string fullTagLocation = Path.Combine(_userInput?.Directory!, tagDirectory);

                Console.WriteLine($"Downloading: {fileToDownload.Filename} (Tag: {tagName})");
                DownloadAudioFile(fileToDownload.Id, fileToDownload.Filename, fullTagLocation, tagName);
            }
        }

        /// <summary>
        /// Downloads an audio file in Base64 string format and saves it as an MP3 file.
        /// </summary>
        /// <param name="recordingId">The unique identifier of the recording to be downloaded.</param>
        /// <param name="fileName">The name of the audio file to be saved.</param>
        /// <param name="fullRecordingLocation">The directory path where the downloaded audio file will be stored.</param>
        /// <param name="tagName">The tag name associated with this recording.</param>
        private static void DownloadAudioFile(string recordingId, string fileName, string fullRecordingLocation, string tagName)
        {
            try
            {
                string? audioBase64String = _apiService?.DownloadAudioFileAsync(recordingId).Result;
                if (string.IsNullOrEmpty(audioBase64String))
                {
                    Console.WriteLine($"Unable to download MP3 file: {fileName}");
                    _failedDownloads.Add(new FailedDownload { Tag = tagName, FileName = fileName });
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "mp3", audioBase64String);
                    Console.WriteLine($"Successfully downloaded: {fileName}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"MP3 Download Failed: {fileName} - {e.Message}");
                _failedDownloads.Add(new FailedDownload { Tag = tagName, FileName = fileName });
            }
        }

        /// <summary>
        /// Retrieves all recordings and file tags from the API.
        /// </summary>
        private static void GetNeededData()
        {
            _listOfAllRecordings = _apiService?.GetAllRecordingsAsync().Result;
            Console.WriteLine($"Retrieved {_listOfAllRecordings?.DataFileList.Count} recordings.");

            _fileTags = _apiService?.GetFileTagsAsync().Result;
            Console.WriteLine($"Retrieved {_fileTags?.DataFiletagList.Count} file tags.");
        }

        /// <summary>
        /// Prompts the user to select tags to download.
        /// </summary>
        private static void SelectTags()
        {
            if (_fileTags?.DataFiletagList.Count <= 0)
            {
                Console.WriteLine("No tags available.");
                return;
            }

            Console.WriteLine("\nAvailable Tags:");
            for (int i = 0; i < _fileTags?.DataFiletagList.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {_fileTags.DataFiletagList[i].Name}");
            }

            Console.WriteLine("\nEnter the tags you want to download (comma-separated, e.g., 1,3,5 or tag names):");
            string? input = _userInput?.Tags ?? Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("No tags selected.");
                return;
            }

            string[] selections = input.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (string selection in selections)
            {
                string trimmed = selection.Trim();

                // Try to parse as number (1-based index)
                if (int.TryParse(trimmed, out int index) && index > 0 && index <= _fileTags?.DataFiletagList.Count)
                {
                    string tagName = _fileTags.DataFiletagList[index - 1].Name;
                    if (!_selectedTags.Contains(tagName, StringComparer.OrdinalIgnoreCase))
                    {
                        _selectedTags.Add(tagName);
                    }
                }
                else
                {
                    // Try to match by tag name
                    DataFiletagList? matchingTag = _fileTags?.DataFiletagList.FirstOrDefault(t =>
                        t.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

                    if (matchingTag != null && !_selectedTags.Contains(matchingTag.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        _selectedTags.Add(matchingTag.Name);
                    }
                }
            }

            Console.WriteLine($"\nSelected {_selectedTags.Count} tag(s):");
            foreach (string tag in _selectedTags)
            {
                Console.WriteLine($"  - {tag}");
            }
        }

        /// <summary>
        /// Prompts the user for the download directory.
        /// </summary>
        private static void GetDownloadDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_userInput?.Directory))
            {
                Console.WriteLine($"Download directory: {_userInput.Directory}");
                return;
            }

            _userInput!.Directory = Environment.GetEnvironmentVariable("PlaudDownloadDirectory", EnvironmentVariableTarget.User)
                ?? Prompt("Enter directory to download files to: ");
        }

        /// <summary>
        /// Parses and retrieves user inputs from command-line arguments or prompts the user for inputs if not provided.
        /// </summary>
        /// <param name="args">An array of command-line arguments.</param>
        private static void GetUserInputs(string[] args)
        {
            _userInput = new UserInput();
            Parser.Default.ParseArguments<UserInput>(args)
                .WithParsed(ui =>
                {
                    ui.Username = !string.IsNullOrWhiteSpace(ui.Username) ? ui.Username : Environment.GetEnvironmentVariable("PlaudUserName", EnvironmentVariableTarget.User) ?? Prompt("Enter username: ");
                    ui.Password = !string.IsNullOrWhiteSpace(ui.Password) ? ui.Password : Environment.GetEnvironmentVariable("PlaudPassword", EnvironmentVariableTarget.User) ?? Prompt("Enter password: ");
                    Console.WriteLine($"Username: {ui.Username}");
                    Console.WriteLine($"Password: {ui.Password}");
                    _userInput = ui;
                })
                .WithNotParsed(errors =>
                {
                    _userInput.Username = Prompt("Enter username: ");
                    _userInput.Password = Prompt("Enter password: ");
                });
        }

        /// <summary>
        /// Prompt the user for input and return the input.
        /// </summary>
        private static string? Prompt(string promptText)
        {
            Console.WriteLine(promptText);
            return Console.ReadLine();
        }

        /// <summary>
        /// Removes invalid characters from the provided string to create a valid file name.
        /// </summary>
        /// <param name="name">The input string that may contain invalid file name characters.</param>
        /// <returns>A new string with invalid file name characters replaced by underscores.</returns>
        private static string RemoveInvalidCharactersFromDirectory(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        }

        /// <summary>
        /// Writes a file to disk with the specified name, path, type, and content.
        /// </summary>
        /// <param name="fileName">The name of the file to be created.</param>
        /// <param name="filePath">The directory path where the file will be saved.</param>
        /// <param name="fileType">The extension or type of the file (e.g., mp3).</param>
        /// <param name="fileContent">The content of the file in Base64-encoded string format.</param>
        private static void WriteFileToDisk(string fileName, string filePath, string fileType, string fileContent)
        {
            string cleanedFileName = RemoveInvalidCharactersFromDirectory(fileName);
            string fileWithExtension = Path.Combine(filePath, $"{cleanedFileName}.{fileType}");
            byte[] fileBytes = Convert.FromBase64String(fileContent);
            File.WriteAllBytes(fileWithExtension, fileBytes);
        }

        /// <summary>
        /// Writes failed downloads to a CSV file.
        /// </summary>
        private static void WriteFailedDownloadsCsv()
        {
            string csvPath = Path.Combine(_userInput?.Directory!, "FailedDownloads.csv");

            using (StreamWriter writer = new (csvPath))
            {
                writer.WriteLine("Tag,FileName");
                foreach (FailedDownload failed in _failedDownloads)
                {
                    writer.WriteLine($"{failed.Tag},{failed.FileName}");
                }
            }

            Console.WriteLine($"\nFailed downloads written to: {csvPath}");
            Console.WriteLine($"Total failed downloads: {_failedDownloads.Count}");
        }

        /// <summary>
        /// Represents a failed download.
        /// </summary>
        private class FailedDownload
        {
            public string Tag { get; init; } = string.Empty;
            public string FileName { get; init; } = string.Empty;
        }
    }
}