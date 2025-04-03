# Plaud .NET Bulk Downloader
This project is meant to fill a gap and allow you to bulk download all your recordings, transcripts and summaries from Plaud.  This project is NOT supported by Plaud.

# Requirements
- .NET SDK 9.0 or later
  - macOS: `brew install dotnet`
  - Windows: Download from [.NET Download Page](https://dotnet.microsoft.com/download)
  - Linux: Follow instructions at [Install .NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)

# Inputs:
- Plaud User Name
- Plaud Password
- Download Location
- Start Date (optional) - Only download recordings after this date

# Running:

1) You can pass data into the console app in 3 ways:
   1) Command Line Input:
      - If you don't use either of the 2 options below this one then you will be prompted to provide the information from the Inputs section.
   2) Arguments:
      - -u = Plaud User Name
      - -p = Plaud Password
      - -d = Download Directory
      - -s = Start Date (format: yyyy-MM-dd)
   3) Environment Variables (User):
      - PlaudUserName = Plaud User Name
      - PlaudPassword = Plaud Password
      - PlaudDownloadDirectory = Download Directory
2) The Console app will create a subdirectory for each tag (folder) you have setup in Plaud in the directory you provided.
3) Next, it loops over each file and attempts to download the audio MP3, transcript (PDF, TXT, DOCX and SRT) and summary (TXT, DOCX, PDF).
4) If a start date is provided, only recordings made after that date will be downloaded.

# Examples:
```bash
# Download all recordings
./Plaud.ConsoleApp.BulkDownloader -u username -p password -d /path/to/download

# Download only recordings after January 1, 2024
./Plaud.ConsoleApp.BulkDownloader -u username -p password -d /path/to/download -s 2024-01-01
```

# Development

## Running Tests
After cloning the repository:

1. Navigate to the solution directory:
   ```bash
   cd Plaud.ConsoleApp.BulkDownloader
   ```

2. Run the tests:
   ```bash
   dotnet test
   ```

The test suite includes unit tests for the date filtering functionality and other core features.