using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TVDBSharp;

namespace TorrentHelper
{
    class Program
    {
        //private const string downloaddir = "";
        private const string scandir = "D:\\__InProgressTorrents\\";
        private const string movedir = "D:\\__CompletedTorrents\\";
        private const string OutputPath = "D:\\__MoveToServer\\";
        private const string OutputExtension = ".mp4";
        private static string[] charstoremove = new string[] { "h26x64", "internal", "noivtc", "lucidtv.", "tbs", "webrip", ".HDTV.", "[eztv]", "x264-", "KILLERS", "720p", "SVA.", "AVS.", "strife.", "web.", "us", "convoy.", "720x20p" };
        const string HandBrakeLocation = "C:\\Program Files\\HandBrake CLI\\HandBrakeCLI.exe";
        const string HandBrakeCommandLine = @"-i ""{in}"" -o ""{out}"" --preset=""Fast 1080p30""";

        public static void Main(string[] args)
        {
            //MoveFiles();
            //GetTorrentInfo();
            ConvertToMP4();

            //delete the mkv files once converted
        }

        private static void ConvertToMP4()
        {
            UseHandBrakeToConvert(); 
        }

        private static void GetTorrentInfo()
        {
            string filename = null;
            var videofiles = Directory.EnumerateFiles(movedir, "*.*", SearchOption.TopDirectoryOnly).Where(f => MeetsCriteria(f));

            foreach (string torrent in videofiles)
            {
                FileInfo torinfo = new FileInfo(torrent);
                filename = CleanFileName(torinfo.Name);

                //Parsing out only the season num and episode num
                var splits = filename.Split('.');
                var seasonepisode = splits.Length > 1 ? splits[splits.Length - 2] : "";

                //Parsing out only the episode
                string episode = seasonepisode.Substring(seasonepisode.Length - 3);

                //Parsing out only the season
                string season = seasonepisode.Substring(0, 3);

                //Get everything to the left of the season/episode to get show name
                string showname = filename.Substring(0, filename.IndexOf(seasonepisode, StringComparison.CurrentCulture));
                showname = showname.Replace(".", " ").Trim();

                //Name the file accordingly
                filename = RemoveAllPeriods(filename);

                string filenamefortvdbAPI = ShowNameUpdater(showname);

                //Query TVDB API
                string episodetitle = QueryTVDBAPI(filenamefortvdbAPI, episode, season);

                //Rename the file with the data just pulled from TVDB API
                RenameWithEpisodeName(filename, showname, episodetitle, season, episode);
            }
        }

        private static void RenameWithEpisodeName(string filename, string showname, string episodetitle, string seasonnumber, string episodenumber)
        {
            string name = Path.GetFileNameWithoutExtension(filename);
            seasonnumber = seasonnumber.Replace("S", "").Trim();
            episodenumber = episodenumber.Replace("E", "").Trim();
            string newFile = showname + " - " + seasonnumber.TrimStart('0') + "x" + episodenumber + " - " + episodetitle + Path.GetExtension(filename);
            Console.WriteLine("The new filename Is: " + newFile);
            File.Move(Path.Combine(movedir, filename), Path.Combine(movedir, newFile));
        }
        private static string QueryTVDBAPI(string showname, string episodenumber, string seasonnumber)
        {
            string episodetitle = null;
            int intEN = StripLettersAndConvertToInt(episodenumber);
            int intSN = StripLettersAndConvertToInt(seasonnumber);
            var tvdb = new TVDB("1B3B1BC6F1B65370");
            var results = tvdb.Search(showname, 1);
            foreach (var show in results)
            {
                foreach (var s in show.Episodes)
                    if (s.EpisodeNumber == intEN && s.SeasonNumber == intSN)
                        episodetitle = s.Title;
            }
            return episodetitle;
        }
        private static int StripLettersAndConvertToInt(string name)
        {
            int seriesidentifier = 0;
            name = Regex.Replace(name, "[^0-9.]", "");
            return seriesidentifier = int.Parse(name);
        }
        private static string ShowNameUpdater(string showname)
        {
            switch (showname)
            {
                case "The Goldbergs 2013": { return "The Goldbergs (2013)"; }
                case "shameless": { return "Shameless (US)"; }
                case "The Flash": { return "The Flash (2014)"; }
            }

            return showname;
        }
        private static string RemoveAllPeriods(string filename)
        {
            string name = Path.GetFileNameWithoutExtension(filename);
            string newName = name.Replace(".", " ");
            string newFile = newName + Path.GetExtension(filename);
            File.Move(Path.Combine(movedir, filename), Path.Combine(movedir, newFile));
            return newFile;
        }
        private static string CleanFileName(string filename)
        {
            string newfilename = filename;
            foreach (var c in charstoremove)
                newfilename = newfilename.Replace(c, string.Empty);

            File.Move(Path.Combine(movedir, filename), Path.Combine(movedir, newfilename));

            return newfilename;
        }
        private static void UseHandBrakeToConvert()
        {
            var files = Directory.EnumerateFiles(movedir, "*.*", SearchOption.TopDirectoryOnly).Where(f => MeetsCriteria(f));

            foreach (var inputFile in files)
            {
                string InputExtension = Path.GetExtension(inputFile.ToLowerInvariant());
                Console.WriteLine(InputExtension);
                var outputFile = Path.Combine(OutputPath, Path.GetFileName(inputFile.ToLowerInvariant()).Replace(InputExtension, ".mp4"));
                Console.WriteLine(outputFile);
                if (File.Exists(outputFile))
                    continue;

                Console.WriteLine("Converting file {0}", inputFile);

                var arguments = HandBrakeCommandLine.Replace("{in}", SanitazeFileForHB(inputFile)).Replace("{out}", SanitazeFileForHB(outputFile));
                var handbrake = Process.Start(HandBrakeLocation, arguments);
                handbrake.WaitForExit();

                File.SetCreationTimeUtc(outputFile, File.GetCreationTimeUtc(inputFile));
                File.SetLastWriteTimeUtc(outputFile, File.GetLastWriteTimeUtc(inputFile));
            }

            Console.WriteLine("Conversion completed!!");
            Console.ReadLine();
        }
        static string SanitazeFileForHB(string file)
        {
            return file.Replace("\\", "/");
        }
        private static void MoveFiles()
        {
            var MyFiles = Directory.EnumerateFiles(scandir, "*.*", SearchOption.AllDirectories).Where(f => MeetsCriteria(f));

            foreach (string file in MyFiles)
            {
                FileInfo mFile = new FileInfo(file);
                mFile.MoveTo(Path.Combine(movedir, mFile.Name));
            }
        }
        private static bool MeetsCriteria(string Filename)
        {
            var ext = Path.GetExtension(Filename).ToLower();

            return (ext == ".avi" || ext == ".mp4" || ext == ".m4v" || ext == ".mkv" || ext == ".mod");
        }
    }
}
