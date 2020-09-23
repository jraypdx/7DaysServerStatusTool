using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace _7DaysServer_InfoMonitor
{
    class MainWindow_ViewModel : INotifyPropertyChanged
    {
        public static string SERVERDIRECTORY = @"C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\7DaysToDieServer_Data";
        public static string WEBSERVERDIRECTORY = @"C:\webserver";

        #region COMMAND DECLERATIONS

        //ICommand ButtonTest_Command { get; }

        #endregion COMMAND DECLERATIONS


        #region PROPERTIES

        public string GameTime 
        {
            get => _GameTime;
            set
            {
                _GameTime = value;
                OnPropertyChanged();
            }
        }
        private string _GameTime;

        public string LastUpdatedTime
        {
            get => _LastUpdatedTime;
            set
            {
                _LastUpdatedTime = value;
                OnPropertyChanged();
            }
        }
        private string _LastUpdatedTime;

        public string CurrentServerLogFile
        {
            get => _CurrentServerLogFile;
            set
            {
                _CurrentServerLogFile = value;
                OnPropertyChanged();
            }
        }
        private string _CurrentServerLogFile;

        public ObservableCollection<string> OnlinePlayers
        {
            get => _OnlinePlayers;
            set
            {
                _OnlinePlayers = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<string> _OnlinePlayers;

        public ObservableCollection<string> Logs
        {
            get => _Logs;
            set
            {
                _Logs = value;
                OnPropertyChanged();
            }
        }
        private ObservableCollection<string> _Logs;


        #endregion PROPERTIES

        public MainWindow_ViewModel()
        {
            //ButtonTest_Command = new DelegateCommand(o => ButtonTest_Function());
            GameTime = "No game time found";
            LastUpdatedTime = "Not updated or fetched yet";
            OnlinePlayers = new ObservableCollection<string>();
            Logs = new ObservableCollection<string>();
            //PrintLog("first log test");
            if (FindCurrent7dLogFile())
                RunProgram();

        }


        /*public void ButtonTest_Function()
        {

        }*/

        /// <summary>
        /// Print a log to the log output with date+time prepended
        /// </summary>
        /// <param name="messageToPrint"></param>
        public void PrintLog(string messageToPrint)
        {
            if (!String.IsNullOrEmpty(messageToPrint))
            {
                App.Current.Dispatcher.Invoke(() => 
                    Logs.Add($"[{DateTime.Now.ToString("yyyy/MM/dd h:mm:ss tt")}]  {messageToPrint}")
                );
            }
        }

        /// <summary>
        /// Looks for the server process and prints to log, then returns true or false if it's found
        /// </summary>
        /// <returns></returns>
        public bool CheckForServerProcess()
        {
            try
            {
                string processName = "7DaysToDieServer";
                Process[] serverProcess = Process.GetProcessesByName(processName);
                if (serverProcess.Count() < 1)
                {
                    PrintLog($"Unable to find the {processName} process!");
                    return false;
                }
                else
                {
                    PrintLog($"{processName} process found with PID: {serverProcess.FirstOrDefault().Id}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                PrintLog($"Error finding server process: {ex.Message}");
                return false;
            }
        }

        public async void RunProgram()
        {
            await Task.Delay(TimeSpan.FromMinutes(2));

            if (!CheckForServerProcess()) //make sure the server is running - if it isn't, end the program
            {
                PrintLog("Couldn't find the server process...");
                return; //ends the recursive loop which ends the program
            }

            var telnetHelper = new ServerCurrentInfo();
            string logIdentifier = telnetHelper.GenerateIdentifierString();
            bool telnetResult = await telnetHelper.SendServerCommand(logIdentifier);
            if (!telnetResult)
            {
                PrintLog("Server telnet command failed!");
            }
            else
            {
                //PrintLog($"Telnet succesful with identifier: {logIdentifier}");
                telnetHelper = null; //Not sure if needed but don't want to use any unneeded ram
                ParseLogsForInfo(logIdentifier);
                SetLastUpdatedTime();
                SaveInfoAsHTML();
                PrintLog($"Index.html and info successfully updated");
            }

            RunProgram(); //Call it again, forever looping until closed

        }

        public void SetLastUpdatedTime()
        {
            DateTime now = DateTime.Now;
            string pst = now.ToString("dddd, dd MMMM yyyy h:mm:ss tt");
            string mdt = now.AddHours(1).ToString("dddd, dd MMMM yyyy h:mm:ss tt");
            string est = now.AddHours(3).ToString("dddd, dd MMMM yyyy h:mm:ss tt");
            LastUpdatedTime = $"PST: [{pst}]\nMST: [{mdt}]\nEST: [{est}]";
        }

        public void ParseLogsForInfo(string logIdentifier)
        {
            try
            {
                //Read file in use by another process: https://stackoverflow.com/questions/9759697/reading-a-file-used-by-another-process
                using (var fs = new FileStream(CurrentServerLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs, true))
                    {
                        string s = String.Empty;
                        bool inGameStateSection = false;
                        while ((s = sr.ReadLine()) != null)
                        {
                            if (inGameStateSection) //Once we know we're in the game state section, pull our data
                            {
                                if (s.Contains("Game time"))
                                    GameTime = s;
                                if (s == "Players:") //Parse the players log block
                                {
                                    OnlinePlayers = new ObservableCollection<string>(); //Clear this real quick so that if all players have gotten of it now displays no players as default
                                    s = sr.ReadLine(); // skip over empty/blank line
                                    while ((s = sr.ReadLine()) != "") //each line will be a player info line, and after all the players a blank line (or if no players just a blank line)
                                    {
                                        //example player line: "  1. id=171, Cabbage Merchant, pos=(-1873.3, 46.1, 2087.1), remote=True, steamid=76561198067267888, ip=192.168.254.254, ping=0"
                                        var playerInfo = s.Split(',');
                                        string playerName = playerInfo[1].Trim(); //get player name, remove the leading space/whitespace from splitting
                                        string playerSteamID = playerInfo[6].Trim(); //add 2 due to commas in player pos
                                        string playerPing = playerInfo[8].Trim(); //add 2 due to commas in player pos
                                        OnlinePlayers.Add($"{playerName} | {playerSteamID} | {playerPing}");
                                    }
                                    if (OnlinePlayers.Count < 1) //If nobody is online, display NONE instead of leaving it blank
                                        OnlinePlayers.Add("NONE");
                                    break; //Once done with players we don't want to keep reading or get any more info (maybe in the future we'll want more info though, do that here)
                                }
                            }
                            else if (s == $"WRITING GAME STATE: {logIdentifier}") //Found the game state log section based on the identifier
                                inGameStateSection = true;
                        }
                    }
                }
                return; //not needed but makes it easier to see the flow
            }
            catch (Exception ex)
            {
                PrintLog($"Error parsing log file: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public void SaveInfoAsHTML()
        {
            try
            {
                string playersString = "";
                foreach (var player in OnlinePlayers)
                    playersString += $"<p>{player}</p>";
                string timezonesString = "";
                foreach (var time in LastUpdatedTime.Split('\n'))
                    timezonesString += $"<p>{time}</p>";
                string htmlString = $"<h2>Josh's 7D Server</h2><p>{GameTime}</p><br><h4>Online players:</h4>{playersString}<br><br><p><strong>Last updated:</strong></p>{timezonesString}";
                File.WriteAllText(Path.Combine(WEBSERVERDIRECTORY, "index.html"), htmlString);
            }
            catch (Exception ex)
            {
                PrintLog($"Error writing index.html file: {ex.Message}");
            }
        }

        public bool FindCurrent7dLogFile()
        {
            var newestLogFile = Directory.GetFiles(SERVERDIRECTORY).Where(x => x.ToLower().EndsWith(".txt")).OrderByDescending(x => File.GetLastWriteTime(x)).First();
            if (String.IsNullOrEmpty(newestLogFile))
            {
                PrintLog("Unable to find the latest server log file!");
                return false;
            }
            else
            {
                CurrentServerLogFile = newestLogFile;
                PrintLog($"Server log file: {CurrentServerLogFile}");
                return true;
            }

            /*string currentLogFile = null;
            var dirTxtFiles = Directory.GetFiles(serverDirectory).Where(x => x.ToLower().EndsWith(".txt"));
            foreach (string f in dirTxtFiles)
            {
                if (currentLogFile == null)
                    currentLogFile = f;
                if (File.GetLastWriteTime(f) > File.GetLastWriteTime(currentLogFile))
                    currentLogFile = f;
            }*/
        }




        #region MVVM BASE CODE
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion MVVM BASE CODE


    }
}
