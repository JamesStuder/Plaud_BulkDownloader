using API.Plaud.NET.Interfaces;
using API.Plaud.NET.Services;
using System;
using System.IO;
using System.Linq;
using API.Plaud.NET.Constants;
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
        /// If neither of these items is passed / set, then the user is prompted to provide the info.
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
            SetupFileTagDirectories();
            ProcessFiles();
            Console.WriteLine("Download complete.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Creates directory structures for file tags and ensures directories exist for saving files.
        /// </summary>
        /// <remarks>
        /// Checks if the root directory specified in the user input exists and creates it if necessary.
        /// For each file tag in the list, a subdirectory named after the tag (with invalid characters removed)
        /// is created within the root directory. If no file tags are available, a message is displayed,
        /// and files are organized directly in the root directory.
        /// </remarks>
        private static void SetupFileTagDirectories()
        {
            if (!Directory.Exists(_userInput?.Directory))
            {
                Directory.CreateDirectory(_userInput?.Directory!);
            }
            
            if (_fileTags?.DataFiletagList.Count <= 0)
            {
                Console.WriteLine("No file tags to process.  All recordings will be saved in directories per recording in the root directory.");
                return;
            }
            
            foreach (DataFiletagList? fileTag in _fileTags?.DataFiletagList!)
            {
                string subDirectory = RemoveInvalidCharactersFromDirectory($"{fileTag.Name}");
                string fullTagLocation = Path.Combine(_userInput?.Directory!, subDirectory);
                if (!Directory.Exists(fullTagLocation))
                {
                    Directory.CreateDirectory(fullTagLocation);
                }
            }
            
            Console.WriteLine("File tag locations setup successfully.");
        }

        /// <summary>
        /// Processes and organizes downloaded files based on metadata such as file tags.
        /// Ensures that the necessary directory structure exists and invokes the appropriate methods
        /// for downloading audio files, transcripts, and summaries for each recording.
        /// </summary>
        /// <remarks>
        /// Steps:
        /// 1. Verifies the existence of the main directory where files will be processed. Creates it if it does not exist.
        /// 2. Checks if any recordings exist to process. Logs a message and exits if none is found.
        /// 3. Iterates through each recording in the list of recordings:
        ///     a. Determines the tag associated with the recording, if any, and removes invalid characters from its name.
        ///     b. Creates a subdirectory structure for organizing files by tag and recording.
        ///     c. Calls corresponding methods to download the audio file, its transcript, and its summary.
        /// Handles invalid file or directory names by removing unsupported characters from the names to ensure proper file system compatibility.
        /// </remarks>
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
                // build your target folder & filename just like you already do
                string tagName = (fileToDownload.FiletagIdList.Count > 0 ? _fileTags?.DataFiletagList.First(t => t.Id == fileToDownload.FiletagIdList[0]).Name : string.Empty)!;
                tagName = RemoveInvalidCharactersFromDirectory(tagName);
                string? directoryWithTag = string.IsNullOrEmpty(tagName) ? _userInput?.Directory : Path.Combine(_userInput?.Directory!, tagName);
                string subDirectory = RemoveInvalidCharactersFromDirectory(fileToDownload.Filename);
                string fullRecordingLocation = Path.Combine(directoryWithTag!, subDirectory);
                
                if (_userInput!.Skip == true && Directory.Exists(fullRecordingLocation))
                {
                    Console.WriteLine($"Skipping “{fileToDownload.Filename}” (already downloaded).");
                    continue;
                }

                if (!Directory.Exists(fullRecordingLocation))
                {
                    Directory.CreateDirectory(fullRecordingLocation);
                }

                Console.WriteLine($"Working on: {subDirectory}");
                DownloadAudioFile(fileToDownload.Id, fileToDownload.Filename, fullRecordingLocation);
                DownloadTranscripts(fileToDownload.Id, $"Transcript_{fileToDownload.Filename}", fullRecordingLocation);
                DownloadSummaries(fileToDownload.Id, $"Summary_{fileToDownload.Filename}", fullRecordingLocation);
            }
        }

        /// <summary>
        /// Downloads summary files in multiple formats (TXT, PDF, DOCX, Markdown) for a given recording
        /// and writes them to the specified location on the disk.
        /// </summary>
        /// <param name="recordingId">The unique identifier of the recording whose summaries are being downloaded.</param>
        /// <param name="fileName">The name of the file to be used for saving the summaries.</param>
        /// <param name="fullRecordingLocation">The full directory path where the downloaded summaries will be stored.</param>
        /// <remarks>
        /// This method attempts to download summaries in all available formats. If a specific file format is unavailable,
        /// an appropriate message is logged to the console.
        /// </remarks>
        private static void DownloadSummaries(string recordingId, string fileName, string fullRecordingLocation)
        {
            try
            {
                string? transcriptBase64StringTxt = _apiService?.DownloadSummaryFileAsync(recordingId, FileTypes.TXT).Result;
                if (string.IsNullOrEmpty(transcriptBase64StringTxt))
                {
                    Console.WriteLine("Unable to download txt summary file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "txt", transcriptBase64StringTxt);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"TXT Summary Download Failed: {e.Message}");
            }
            
            try
            {
                string? transcriptBase64StringPdf = _apiService?.DownloadSummaryFileAsync(recordingId, FileTypes.PDF).Result;
                if (string.IsNullOrEmpty(transcriptBase64StringPdf))
                {
                    Console.WriteLine("Unable to download pdf summary file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "pdf", transcriptBase64StringPdf);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"PDF Summary Download Failed: {e.Message}");
            }
            
            try
            {
                string? transcriptBase64StringDocx = _apiService?.DownloadSummaryFileAsync(recordingId, FileTypes.DOCX).Result;
                if (string.IsNullOrEmpty(transcriptBase64StringDocx))
                {
                    Console.WriteLine("Unable to download docx summary file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "docx", transcriptBase64StringDocx);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"DOCX Summary Download Failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Downloads transcript files in various formats (TXT, PDF, DOCX, SRT) for a given recording ID
        /// and writes them to the disk at the specified location.
        /// </summary>
        /// <param name="recordingId">The unique identifier of the recording to download transcripts for.</param>
        /// <param name="fileName">The base file name to use for the downloaded transcript files.</param>
        /// <param name="fullRecordingLocation">The directory path where the files should be saved.</param>
        /// <remarks>
        /// This method attempts to download transcript files in multiple formats (TXT, PDF, DOCX, SRT) using
        /// an API service. If the download for a specific format fails, a message will be displayed on the console.
        /// Successfully downloaded files are written to the disk in the specified location with the appropriate file extension.
        /// </remarks>
        private static void DownloadTranscripts(string recordingId, string fileName, string fullRecordingLocation)
        {
            try
            {
                string? transcriptBase64StringTxt = _apiService?.DownloadTranscriptFileAsync(recordingId, FileTypes.TXT).Result;
                if (string.IsNullOrEmpty(transcriptBase64StringTxt))
                {
                    Console.WriteLine("Unable to download txt transcript file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "txt", transcriptBase64StringTxt);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"TXT Transcript Download Failed: {e.Message}");
            }

            try
            {
                string? transcriptBase64StringPdf = _apiService?.DownloadTranscriptFileAsync(recordingId, FileTypes.PDF).Result;
                if (string.IsNullOrEmpty(transcriptBase64StringPdf))
                {
                    Console.WriteLine("Unable to download pdf transcript file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "pdf", transcriptBase64StringPdf);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"PDF Transcript Download Failed: {e.Message}");
            }

            try
            {
                string? transcriptBase64StringDocx = _apiService?.DownloadTranscriptFileAsync(recordingId, FileTypes.DOCX).Result;
                if (string.IsNullOrEmpty(transcriptBase64StringDocx))
                {
                    Console.WriteLine("Unable to download docx transcript file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "docx", transcriptBase64StringDocx);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"DOCX Transcript Download Failed: {e.Message}");
            }

            try
            {
                string? transcriptBase64StringSrt = _apiService?.DownloadTranscriptFileAsync(recordingId, FileTypes.SRT).Result;
                if (string.IsNullOrEmpty(transcriptBase64StringSrt))
                {
                    Console.WriteLine("Unable to download srt transcript file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "srt", transcriptBase64StringSrt);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"SRT Transcript Download Failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Downloads an audio file in Base64 string format and saves it as an MP3 file at the specified location.
        /// </summary>
        /// <param name="recordingId">The unique identifier of the recording to be downloaded.</param>
        /// <param name="fileName">The name of the audio file to be saved.</param>
        /// <param name="fullRecordingLocation">The directory path where the downloaded audio file will be stored.</param>
        /// <remarks>
        /// This method interacts with the Plaud API to retrieve the audio file data in Base64 format.
        /// The resulting audio file is processed and stored in the designated directory as an MP3 file.
        /// </remarks>
        private static void DownloadAudioFile(string recordingId, string fileName, string fullRecordingLocation)
        {
            try
            {
                string? audioBase64String = _apiService?.DownloadAudioFileAsync(recordingId).Result;
                if (string.IsNullOrEmpty(audioBase64String))
                {
                    Console.WriteLine("Unable to download MP3 file.");
                }
                else
                {
                    WriteFileToDisk(fileName, fullRecordingLocation, "mp3", audioBase64String);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"MP3 Download Failed: {e.Message}");
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
        /// -s = Skip previous downloaded files.  This is set to true by default.
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
                    if (!ui.Skip.HasValue)
                    {
                        string? env = Environment.GetEnvironmentVariable("PlaudSkip", EnvironmentVariableTarget.User);
                        ui.Skip = bool.TryParse(env, out bool envSkip) ? envSkip : PromptBool("Skip previously downloaded files? (true/false): ");
                    }
                })
                .WithNotParsed(errors =>
                {
                    _userInput.Username = Prompt("Enter username: ");
                    _userInput.Password = Prompt("Enter password: ");
                    _userInput.Directory = Prompt("Enter directory: ");
                    _userInput.Skip = PromptBool("Skip previously downloaded files? (true/false): ");
                });
        }

        /// <summary>
        /// Prompt the user for input and return the input
        /// </summary>
        private static string? Prompt(string promptText)
        {
            Console.WriteLine(promptText);
            return Console.ReadLine();
        }

        /// <summary>
        /// Prompts the user to enter a boolean value using a provided message.
        /// Acceptable inputs include variations of "true"/"false" such as "yes"/"no", "y"/"n", "t"/"f".
        /// </summary>
        /// <param name="message">The message to display to the user when prompting for input.</param>
        /// <returns>A boolean value derived from the user's input. Returns <c>true</c> for affirmative inputs such as "true", "yes", "y", or "t"; otherwise returns <c>false</c>.</returns>
        private static bool PromptBool(string message)
        {
            while (true)
            {
                Console.Write(message);
                string? input = Console.ReadLine()?.Trim().ToLower();
                switch (input)
                {
                    case "true":
                        return true;
                    case "false":
                        return false;
                    default:
                        Console.WriteLine("Please enter ‘true’ or ‘false’.");
                        break;
                }
            }
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
        /// <param name="fileType">The extension or type of the file (e.g., txt, pdf).</param>
        /// <param name="fileContent">The content of the file in Base64-encoded string format.</param>
        /// <remarks>
        /// This method ensures that the file name is sanitized by removing invalid characters.
        /// The file content is decoded from Base64 before being written to disk.
        /// </remarks>
        private static void WriteFileToDisk(string fileName, string filePath, string fileType, string fileContent)
        {
            string cleanedFileName = RemoveInvalidCharactersFromDirectory(fileName);
            string fileWithExtension = Path.Combine(filePath, $"{cleanedFileName}.{fileType}");
            byte[] fileBytes = Convert.FromBase64String(fileContent);
            File.WriteAllBytes(fileWithExtension, fileBytes);
        }
    }
}