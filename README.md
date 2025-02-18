# Plaud .NET Bulk Downloader
This project is meant to fill a gap and allow you to bulk download all your recordings, transcripts and summaries from Plaud.  This project is NOT supported by Plaud.

# Inputs:
- Plaud User Name
- Plaud Password
- Download Location

# Running:

1) You can pass data into the console app in 3 ways:
   1) Command Line Input:
      - If you don't use either of the 2 options below this one then you will be prompted to provide the information from the Inputs section.
   2) Arguments:
      - -u = Plaud User Name
      - -p = Plaud Password
      - -d = Download Directory
   3) Environment Variables (User):
      - PlaudUserName = Plaud User Name
      - PlaudPassword = Plaud Password
      - PlaudDownloadDirectory = Download Directory
2) The Console app will create a subdirectory for each tag (folder) you have setup in Plaud in the directory you provided.
3) Next, it loops over each file and attempts to download the audio MP3, transcript (PDF, TXT, DOCX and SRT) and summary (TXT, DOCX, PDF).