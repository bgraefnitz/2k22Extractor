﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using _2k22Extractor.NLL;
using System.Collections;

namespace _2k22Extractor
{

    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
            private static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
            static extern bool ReadProcessMemory(IntPtr hProcess,IntPtr lpBaseAddress,[Out] byte[] lpBuffer,int dwSize,out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        private const int AllProcessAccessLevel = 0x001F0FFF;

        private Game _game;
        private Game prevGame;

        private Int64 _baseAddress;

        private readonly int _exportFrequency = 5;

        //Setup all of the static game data:
        //create list and add all of the stats to the stat offset array with all of the offsets to be looped through
        //to add new stats for players, add the offset here, add a property to the player class with the SAME name, add to string format, box score header, toString on player class, also need to add blanks or summaries in toString and percentagesToString in Team class
        readonly List<Offset> _playerStatOffsets = new List<Offset>
            {
                new Offset("SecondsPlayed",23328,StatDataType.Float),
                new Offset("Points",22256,StatDataType.TwoByteInt),
                new Offset("DefRebounds",23290,StatDataType.TwoByteInt),
                new Offset("Assists",23320,StatDataType.TwoByteInt),
                new Offset("Steals",23312,StatDataType.TwoByteInt),
                new Offset("Blocks",23314,StatDataType.TwoByteInt),
                new Offset("Turnovers",23324,StatDataType.TwoByteInt),
                new Offset("TwoPM",22264,StatDataType.TwoByteInt),
                new Offset("TwoPA",22266,StatDataType.TwoByteInt),
                new Offset("ThreePM",22268,StatDataType.TwoByteInt),
                new Offset("ThreePA",22270,StatDataType.TwoByteInt),
                new Offset("FTM",22260,StatDataType.TwoByteInt),
                new Offset("FTA",22262,StatDataType.TwoByteInt),
                new Offset("OffRebounds",23288,StatDataType.TwoByteInt),
                new Offset("Fouls",23316,StatDataType.TwoByteInt),
                new Offset("PlusMinus",23336,StatDataType.TwoByteInt),
                new Offset("PointsAssisted",23322,StatDataType.TwoByteInt),
                new Offset("PointsInPaint",22280,StatDataType.TwoByteInt),
                new Offset("SecondChancePoints",22300,StatDataType.TwoByteInt),
                new Offset("FastBreakPoints",22298,StatDataType.TwoByteInt),
                new Offset("PointsOffTurnovers",22282,StatDataType.TwoByteInt),
                new Offset("Dunks",22304,StatDataType.TwoByteInt)
            };

        readonly List<Offset> _strategyOffsets = new List<Offset>
            {
                new Offset("OnBallPressure", 0, StatDataType.BitRange,0,3),
                new Offset("OffBallPressure", 0, StatDataType.BitRange,3,3),
                new Offset("ForceDirection", 0, StatDataType.BitRange,6,2),
                new Offset("OnBallScreen", 1, StatDataType.BitRange,0,3),
                new Offset("Hedge", 1, StatDataType.BitRange,3,3),
                new Offset("OffBallScreen", 1, StatDataType.BitRange,6,2),
                new Offset("Post", 2, StatDataType.BitRange,0,3),
                new Offset("DoublePerimeter", 2, StatDataType.BitRange,3,2),
                new Offset("DoublePost", 2, StatDataType.BitRange,5,3),
                new Offset("OnBallScreenCenter", 3, StatDataType.BitRange,0,3),
                new Offset("HedgeCenter", 3, StatDataType.BitRange,3,3),
                new Offset("DriveHelp", 4, StatDataType.BitRange,0,2),
                new Offset("ScreenHelp", 4, StatDataType.BitRange,2,2),
                new Offset("ExtendPressure", 4, StatDataType.BitRange,4,2),
                new Offset("StayAttached", 5, StatDataType.BitRange,0,2),
                new Offset("PreRotate", 5, StatDataType.BitRange,2,1),
                new Offset("DefendingPlayer", 8, StatDataType.FourByteInt)
            };

        //format string for box score (player stats)
        private const string FormatString =
            "{0,-36}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}{11,8}{12,8}{13,8}{14,8}{15,8}{16,8}{17,8}{18,8}{19,8}{20,8}{21,8}{22,8}";

        //format string for lineups
        private const string LineupFormatString =
            "{0,-100}{1,8}{2,8}{3,8}{4,8}{5,8}{6,8}{7,8}{8,8}{9,8}{10,8}{11,8}{12,8}{13,8}{14,8}{15,8}{16,8}{17,8}{18,8}{19,8}{20,8}{21,8}";

        public Form1()
        {
            InitializeComponent();

            cboAwayTeam.DataSource = Teams.TeamList.ToList();
            cboAwayTeam.DisplayMember = "Name";
            cboAwayTeam.ValueMember = "TeamId";
            cboHomeTeam.DataSource = Teams.TeamList.ToList();
            cboHomeTeam.DisplayMember = "Name";
            cboHomeTeam.ValueMember = "TeamId";

            txtFolder.Text = Properties.Settings.Default.FilePath;
            chkAutoOpen.Checked = Properties.Settings.Default.AutoOpenHTML;
            chkAutoClose.Checked = Properties.Settings.Default.AutoClose;
            chkAutoReplay.Checked = Properties.Settings.Default.AutoReplay;
        }

        //TODO: Separate "get value from memory" stuff into a separate method

        private void btnExport_Click(object sender, EventArgs e)
        {
            //Get export path from text box and set base file name for files to be written
            //For export file path, look at storing in registry so user doesn't have to reselect every time?
            var exportFilePath = txtFolder.Text;

            //this logic could be better, but make sure a directory is selected
            if (exportFilePath != null)
            {
                //Setup the game, teams and players
                var setupResult = SetupGame();

                if (setupResult)
                {
                    if (!exportFilePath.EndsWith(@"\"))
                        exportFilePath += @"\";
                    //hold the time of of the last export so that we don't export every time we gather new stats (only on the export frequency)
                    DateTime lastExport = DateTime.Now;

                    //bool to see if it is the first time through, so that we can open the live feeds if the user wants
                    var firstExport = true;

                    //Loop until the game is over
                    //Should probably thread this and implement cancellation, exit on error, etc.
                    while (!_game.GameEnded)
                    {
                        try
                        {
                            //Store the previous game for comparison to see what has changed for play by play
                            prevGame = Clone(_game);
                            //Get stats from memory
                            GetStats(prevGame);

                            //Detect changes in game to output game events for play by play
                            _game.GameEvents.AddRange(_game.GameChanges(prevGame));

                            //if the number of seconds in the export frequency have passed then export
                            if (DateTime.Now > lastExport.AddSeconds(_exportFrequency))
                            {
                                //Export box score to file
                                var liveBoxPath = ExportIndex(exportFilePath, "html", _game.GameTime, null, null);

                                //Open the live feeds?
                                if (firstExport && chkAutoOpen.Checked)
                                {
                                    firstExport = false;
                                    Process.Start(liveBoxPath);
                                }
                                lastExport = DateTime.Now;
                            }

                            //check that if there is no time left and scores don't match, that it actually stays that way!
                            if (_game.GameTime == "End of game check")
                            {
                                //check every 1 second for 60 seconds to make sure the game is actually ended
                                //we're doing this instead of just waiting 60 seconds in case the game didn't end, and continued on, we don't want to hang here if we know the game is still going
                                //also, somebody could have been fouled at the buzzer and take this long to shoot free throws
                                var checkCount = 0;
                                while (_game.GameTime == "End of game check" && checkCount <= 60)
                                {
                                    Thread.Sleep(1000);
                                    GetStats(prevGame);
                                    checkCount++;
                                }
                                //if after 60 seconds we're still checking to see if the game is over, that means the score is not tied with no time on the clock, so it's over
                                if (_game.GameTime == "End of game check")
                                    _game.GameEnded = true;
                            }
                        }

                        catch(Exception ex)
                        { throw ex; }
                        
                        //Sleep for the thread for a bit so that we aren't eating up all CPU
                        Thread.Sleep(200);
                    }

                    //Now that the game is done, export box score and play by play to txt file
                    //if it doesn't work, we'll sleep for 60 seconds and try again because it is imporant to generate these files
                    try
                    {
                        //Get game stats one last time and add any events that occurred since the last time previous game was cloned
                        GetStats(prevGame);
                        _game.GameEvents.AddRange(_game.GameChanges(prevGame));

                        var finalBoxPath = ExportStats(exportFilePath, "txt", _game.GameTime, null, null);
                        var finalPlayByPlayPath = ExportPlayByPlay(exportFilePath, "txt");
                        //Export the live files one more time to post the file path
                        ExportIndex(exportFilePath, "html", _game.GameTime, finalBoxPath, finalPlayByPlayPath);
                    }
                    catch
                    {
                        Thread.Sleep(60000);
                        var finalBoxPath = ExportStats(exportFilePath, "txt", _game.GameTime, null, null);
                        var finalPlayByPlayPath = ExportPlayByPlay(exportFilePath, "txt");
                        //Export the live files one more time to post the file path
                        ExportIndex(exportFilePath, "html", _game.GameTime, finalBoxPath, finalPlayByPlayPath);
                    }


                    //If the auto-close option is selected, then close the game and then the extractor
                    if (chkAutoClose.Checked)
                    {
                        var firstOrDefault = Process.GetProcessesByName("nba2k22").FirstOrDefault();
                        if (firstOrDefault != null)
                            firstOrDefault.Kill();
                        Close();
                    }
                    var input = new Input();
                    if (chkAutoReplay.Checked)
                    {
                        //wait 30 more seconds to make sure we're at the end game screen (don't want to hit a button to get there because if we already are it will go into box scores
                        Thread.Sleep(20000);
                        input.RestartGame((int)cboHomeTeam.SelectedValue, (int)cboAwayTeam.SelectedValue);
                        WaitOnGameLoaded();
                        btnExport_Click(this,null);
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a valid directory");
            }
        }

        private void WaitOnGameLoaded()
        {
            //wait 1 min while game is setup and teams are chosen
            Thread.Sleep(60000);
            //hit space bar so we don't have to wait on 2ktv forever
            var input = new Input();
            input.Continue();
            //loop to check game clock is set to 1st quarter and default start time for 1 minute - if it hasn't been after a minute then something has probably gone wrong and we'll just continue forward
            var checkCount = 0;
            GetStats(prevGame);
            while ((_game.CurrentQuarter != 1 || _game.SecondsRemaining != Game.StartingQuarterTime) && checkCount <= 60)
            {
                Thread.Sleep(1000);
                GetStats(prevGame);
                checkCount++;
            }
            //we have either waited until the game has loaded or it's not going to happen - wait a bit more to make sure all data is loaded and not just game clock, and then end the wait
            Thread.Sleep(2000);
            return;
        }

        private void btnFolder_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                txtFolder.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private bool SetupGame()
        {
            //get the process for NBA 2k22
            Process process = Process.GetProcessesByName("nba2k22").FirstOrDefault();

            //check to make sure NBA 2k22 is actually open
            if (process != null)
            {
                _baseAddress = (Int64)process.MainModule.BaseAddress;

                //Open the process with read-only access
                IntPtr processHandle = OpenProcess(AllProcessAccessLevel, false, process.Id);

                //Declare the game object with the appropriate start time
                _game = new Game(DateTime.Now,_baseAddress);

                //Create teams by clearing the team list, designating Home/Away, and supplying the pointer for Number of Players, and Pointer for the starting Center
                //Adding Away team first so that we can loop through the teams without worrying about Home/Away because Away is always shown first
                _game.Teams.Clear();
                
                //                               Score                   OnFloor                 Team Name               Def Settings
                _game.Teams.Add(new Team("Away",   528, _baseAddress + 0x53C6DE8, _baseAddress + 0x630669C, _baseAddress + 0x5163A5C));
                _game.Teams.Add(new Team("Home", -1400, _baseAddress + 0x53C6E10, _baseAddress + 0x6305444, _baseAddress + 0x6288548));

                foreach (var team in _game.Teams)
                {
                    for (var p = 0; p < 13; p++)
                    {
                        team.Players.Add(new Player(p + 1, team.BasePlayerPointer + 0x8 * p));
                    }
                }

                //declare byte read stuff
                IntPtr bytesRead;

                //Seconds remaining in quarter:
                var secondsRemainingBuffer = new byte[2];
                var secondsRemainingAddress = new IntPtr(_game.SecondsRemainingPointer);
                ReadProcessMemory(processHandle, secondsRemainingAddress, secondsRemainingBuffer, secondsRemainingBuffer.Length, out bytesRead);
                _game.SecondsRemaining = BitConverter.ToInt16(secondsRemainingBuffer, 0);

                //Get game-level data like quarter and time remaining
                //Quarter:
                var quarterBuffer = new byte[2];
                //This address hold the final score each quarter is offset by AC from this so we will add AC for each Quarter
                var quarterAddress = new IntPtr(_game.QuarterPointer);
                ReadProcessMemory(processHandle, quarterAddress, quarterBuffer, quarterBuffer.Length, out bytesRead);
                _game.CurrentQuarter = BitConverter.ToInt16(quarterBuffer, 0);

                //get dynamic location of scores
                var scorePointerBuffer = new byte[8];
                var scorePointer = new IntPtr(_game.ScorePointer);
                //Get the value of the pointer, which will show you where score is being held in dynamic memory
                ReadProcessMemory(processHandle, scorePointer, scorePointerBuffer,
                    scorePointerBuffer.Length,
                    out bytesRead);
                _game.DynamicGamePointer = BitConverter.ToInt64(scorePointerBuffer, 0);

                //set location of player with ball based on dynamic game pointer
                _game.PlayerWithBallPointer = _game.DynamicGamePointer + Game.PlayerWithBallModifier;

                if (_game.CurrentQuarter >= 1 && _game.SecondsRemaining <= Game.StartingQuarterTime)
                {
                    //Loop through teams to get team level data like team name and team stats/scores
                    foreach (var team in _game.Teams)
                    {
                        //Get team names
                        //convert the byte array to an integer and then to an IntPtr
                        var nameAddressValue = (IntPtr)team.NamePointer;
                        var teamNameBuffer = new byte[40];
                        ReadProcessMemory(processHandle, nameAddressValue, teamNameBuffer, teamNameBuffer.Length,
                            out bytesRead);
                        var teamName = Encoding.Unicode.GetString(teamNameBuffer);
                        team.Name = teamName.Split(new string[] {"\0"}, StringSplitOptions.None)[0];

                        //get number of players on the team
                        var numPlayersBuffer = new byte[1];
                        var numPlayersAddress = new IntPtr(team.NumPlayersPointer);
                        ReadProcessMemory(processHandle, numPlayersAddress, numPlayersBuffer, numPlayersBuffer.Length, out bytesRead);
                        team.NumPlayers = numPlayersBuffer[0];

                        
                        //convert the byte array to an integer and then to an IntPtr
                        team.FinalScorePointer = _game.DynamicGamePointer + team.FinalScoreOffset;

                        //loop through each spot on the depth chart
                        for (var p = team.Players.Count-1; p >= 0; p--)
                        {
                            var player = team.Players[p];
                            if (player.DepthChartPos > team.NumPlayers)
                                team.Players.Remove(player);

                            //get name using name pointer
                            var namePointerBuffer = new byte[8];
                            var playerPointer = new IntPtr(player.LastNamePointer);
                            //Get the value of the pointer, which will show you where last name is being held in dynamic memory
                            ReadProcessMemory(processHandle, playerPointer, namePointerBuffer,
                                namePointerBuffer.Length,
                                out bytesRead);
                            //convert the byte array to an integer and then to an IntPtr
                            var lastNameAddress64 = BitConverter.ToInt64(namePointerBuffer, 0);
                            var lastNameAddress = (IntPtr)lastNameAddress64;
                            //dynamic address of first name is 8 bytes after last name
                            var firstNameAddress = lastNameAddress + 40;
                            
                            //get 18 character name (40 bytes because unicode = characters *2)
                            var firstNameBuffer = new byte[40];
                            ReadProcessMemory(processHandle, firstNameAddress, firstNameBuffer,
                                firstNameBuffer.Length,
                                out bytesRead);
                            var firstName = Encoding.Unicode.GetString(firstNameBuffer);
                            player.FirstName = firstName.Split(new string[] { "\0" }, StringSplitOptions.None)[0];

                            //get 18 character name (40 bytes because unicode = characters *2)
                            var lastNameBuffer = new byte[40];
                            ReadProcessMemory(processHandle, lastNameAddress, lastNameBuffer, lastNameBuffer.Length,
                                out bytesRead);
                            var lastName = Encoding.Unicode.GetString(lastNameBuffer);
                            player.LastName = lastName.Split(new string[] { "\0" }, StringSplitOptions.None)[0];

                            //get stats pointers
                            //assuming 8 byte pointer registers since NBA 2k22 is only 64 bit
                            var statBuffer = new byte[8];
                            //this gets the static address for the given team/position on depth chart (as specified in _playerList)
                            var pointerToPointer = (IntPtr)(lastNameAddress64 + 0x4F0);
                            //Get the value of the pointer, which will show where player stats are held in dynamic memory
                            ReadProcessMemory(processHandle, pointerToPointer, statBuffer, statBuffer.Length, out bytesRead);
                            //convert the byte array to an integer and then to an IntPtr
                            player.DynamicPlayerPointer = BitConverter.ToInt64(statBuffer, 0);
                        }
                    }
                    //Now we'll get settings from the NLL site and set them in the game
                    /*
                    var gameSettings = NLL.DataAccessLayer.GetGameSettings(_game.Teams[0].Name, _game.Teams[1].Name);
                    foreach(var opponentSettings in gameSettings.AwaySettings)
                        MapSettingsToInGamePlayer(processHandle,_game.Teams[0], _game.Teams[1], opponentSettings);
                    foreach (var opponentSettings in gameSettings.HomeSettings)
                        MapSettingsToInGamePlayer(processHandle, _game.Teams[1], _game.Teams[0], opponentSettings);
                    */
                    return true;
                }
                MessageBox.Show("Please make sure a game setup with 12 minute quarters, is loaded, and start the extractor prior to tipoff!");
                return false;
            }
            MessageBox.Show("NBA 2k22 Isn't Even Open!!");
            return false;
        }

        private void MapSettingsToInGamePlayer(IntPtr processHandle, Team team, Team oppTeam, DefensiveMatchup opponentSettings)
        {
            /*
            //get opposing player and if found set all strategy settings
            var oppPlayer = oppTeam.Players.FirstOrDefault(p => p.FullName.ToLower() == opponentSettings.OpposingPlayer);
            //TODO: change to a null check on oppPlayer if rosters are solid enough that we can use this as validation that the correct rosters are being used
            if (oppPlayer == null)
                return;

            foreach(var offset in _strategyOffsets)
            {
                var valueObject = opponentSettings.GetType().GetProperty(offset.Name).GetValue(opponentSettings);
                if(offset.Name == "DefendingPlayer")
                {
                    if(valueObject != null)
                    {
                        var defPlayer = team.Players.FirstOrDefault(p => p.FullName.ToLower() == opponentSettings.DefendingPlayer);
                        SetInt(processHandle, team.DefensiveSettingsPointer, defPlayer.DepthChartPos - 1, offset.OffsetInt + 12 * (oppPlayer.DepthChartPos - 1));
                    }
                }
                else
                {
                    var value = Convert.ToInt32(valueObject);
                    SetBitsInInt(processHandle, team.DefensiveSettingsPointer, 1, offset.StartingBit, offset.BitLength, value, offset.OffsetInt + 12 * (oppPlayer.DepthChartPos - 1));
                }
            }
            */
    }

        private void GetStats(Game prevGame)
        {
            //get the process for NBA 2k22
            Process process = Process.GetProcessesByName("nba2k22").FirstOrDefault();

            //check to make sure NBA 2k22 is actually open
            if (process != null)
            {
                //Open the process with read-only access
                IntPtr processHandle = OpenProcess(AllProcessAccessLevel, false, process.Id);

                //declare byte read stuff
                IntPtr bytesRead;

                //Seconds remaining in quarter:
                var secondsRemainingBuffer = new byte[2];
                //This address hold the final score each quarter is offset by 5C from this so we will add 5C for each Quarter
                var secondsRemainingAddress = new IntPtr(_game.SecondsRemainingPointer);
                ReadProcessMemory(processHandle, secondsRemainingAddress, secondsRemainingBuffer, secondsRemainingBuffer.Length, out bytesRead);
                _game.SecondsRemaining = BitConverter.ToInt16(secondsRemainingBuffer, 0);

                //Get game-level data like quarter and time remaining
                //Quarter:
                var quarterBuffer = new byte[2];
                //This address hold the final score each quarter is offset by 5C from this so we will add 5C for each Quarter
                var quarterAddress = new IntPtr(_game.QuarterPointer);
                ReadProcessMemory(processHandle, quarterAddress, quarterBuffer, quarterBuffer.Length, out bytesRead);
                _game.CurrentQuarter = BitConverter.ToInt16(quarterBuffer, 0);

                //Loop through teams to get team level data like team name and team stats/scores
                for (int teamInt = 0; teamInt < _game.Teams.Count; teamInt++)
                {
                    var team = _game.Teams[teamInt];
                    
                    //Get Scores
                    var scoreBuffer = new byte[1];
                    //This address hold the final score each quarter is offset by AC from this so we will add AC for each Quarter 
                    //We could add all of these to the offset loop instead of getting them individually, but would need to choose how to put them into the score class
                    var scoreAddress = new IntPtr(team.FinalScorePointer);
                    ReadProcessMemory(processHandle, scoreAddress, scoreBuffer, scoreBuffer.Length, out bytesRead);
                    team.Score.Final = scoreBuffer[0];
                    //1st Quarter
                    scoreAddress = IntPtr.Add(scoreAddress, 0xAC);
                    ReadProcessMemory(processHandle, scoreAddress, scoreBuffer, scoreBuffer.Length, out bytesRead);
                    team.Score.Q1 = scoreBuffer[0];
                    //2nd Quarter
                    scoreAddress = IntPtr.Add(scoreAddress, 0xAC);
                    ReadProcessMemory(processHandle, scoreAddress, scoreBuffer, scoreBuffer.Length, out bytesRead);
                    team.Score.Q2 = scoreBuffer[0];
                    //3rd Quarter
                    scoreAddress = IntPtr.Add(scoreAddress, 0xAC);
                    ReadProcessMemory(processHandle, scoreAddress, scoreBuffer, scoreBuffer.Length, out bytesRead);
                    team.Score.Q3 = scoreBuffer[0];
                    //4th Quarter
                    scoreAddress = IntPtr.Add(scoreAddress, 0xAC);
                    ReadProcessMemory(processHandle, scoreAddress, scoreBuffer, scoreBuffer.Length, out bytesRead);
                    team.Score.Q4 = scoreBuffer[0];
                    //OT
                    scoreAddress = IntPtr.Add(scoreAddress, 0xAC);
                    ReadProcessMemory(processHandle, scoreAddress, scoreBuffer, scoreBuffer.Length, out bytesRead);
                    team.Score.OT = scoreBuffer[0];
                    
                    //Now we loop through each spot on the depth chart
                    byte[] statBuffer;
                    foreach (var player in team.Players)
                    {
                        var statAddressValue = new IntPtr(player.DynamicPlayerPointer);

                        //loop through each stat offset and get the value for the given player
                        foreach (var offset in _playerStatOffsets)
                        {
                            //delcare type objects to find the stat by name and set it
                            var t = player.GetType();
                            PropertyInfo statProperty;
                            //Go to the address the pointer pointed you to and add the offset to get to the specific stat
                            var finalStatAddr = IntPtr.Add(statAddressValue, offset.OffsetInt);
                            //case to handle different data types
                            switch (offset.Type)
                            {
                                case StatDataType.TwoByteInt:
                                    //set the buffer array to be the number of bytes of the address that we're trying to read
                                    statBuffer = new byte[2];
                                    
                                    ReadProcessMemory(processHandle, finalStatAddr, statBuffer, statBuffer.Length,
                                        out bytesRead);

                                    var int16Value = BitConverter.ToInt16(statBuffer, 0);

                                    //Get the property from the player class by name and set the value
                                    //This requires the Name in the offset object to match a property in the Player class
                                    statProperty = t.GetProperty(offset.Name);
                                    statProperty.SetValue(player, int16Value, null);
                                    break;
                                case StatDataType.Float:
                                    //set the buffer array to be the number of bytes of the address that we're trying to read
                                    statBuffer = new byte[4];
                                    ReadProcessMemory(processHandle, finalStatAddr, statBuffer, statBuffer.Length,
                                        out bytesRead);

                                    var floatValue = BitConverter.ToSingle(statBuffer, 0);

                                    //Get the property from the player class by name and set the value
                                    //This requires the Name in the offset object to match a property in the Player class
                                    statProperty = t.GetProperty(offset.Name);
                                    statProperty.SetValue(player, floatValue, null);
                                    break;
                            }
                        }
                    }
                    //find which players on the court
                    //first reset which players are on the floor
                    foreach (var player in team.Players)
                        player.OnFloor(0);

                    /* not needed in 2k22 - direct address
                    //read the pointer to get the dynamic address where the positions are held
                    var positionPointerBuffer = new byte[8];
                    var positionPointerAddress = new IntPtr(team.PlayersOnFloorPointer);
                    //Get the value of the pointer, which will show you where the player in this position is being held in dynamic memory
                    ReadProcessMemory(processHandle, positionPointerAddress, positionPointerBuffer, positionPointerBuffer.Length,
                        out bytesRead);
                    //convert the byte array to an integer and then to an IntPtr
                    var positionAddress = BitConverter.ToInt64(positionPointerBuffer, 0);
                    */
                    for (var p = 1; p <= 5; p++)
                    {
                        //read the pointer to get the dynamic address where the this position is held
                        var positionPlayerBuffer = new byte[8];
                        var positionPlayerAddress = new IntPtr(team.PlayersOnFloorPointer + (8 * (p - 1)));
                        //Get the value of the pointer, which will show you where the player in this position is being held in dynamic memory
                        ReadProcessMemory(processHandle, positionPlayerAddress, positionPlayerBuffer, positionPlayerBuffer.Length,
                            out bytesRead);
                        //convert the byte array to an integer and then to an IntPtr
                        var playerAddress = BitConverter.ToInt64(positionPlayerBuffer, 0);

                        //check to see which player matches up with the pointer
                        foreach (var player in team.Players.Where(player => player.DynamicPlayerPointer == playerAddress))
                            player.OnFloor(p);
                    }
                    
                    //find which player has the ball (and therefore which team also)
                    //read the pointer to get the dynamic address of the player with the ball
                    var ballPointerBuffer = new byte[8];
                    var ballPointerAddress = new IntPtr(_game.PlayerWithBallPointer);
                    //Get the value of the pointer, which will show you where the player with the ball is being held in dynamic memory
                    ReadProcessMemory(processHandle, ballPointerAddress, ballPointerBuffer, ballPointerBuffer.Length, out bytesRead);
                    //convert the byte array to an integer and then to an IntPtr
                    var ballAddressValue = BitConverter.ToInt64(ballPointerBuffer, 0);
                    //loop through every player and if the player's dynamic address matches the address with the ball, then set true else false
                    foreach (var player in team.Players)
                    {
                        //see if this is who has the ball
                        if (player.DynamicPlayerPointer == ballAddressValue)
                        {
                            player.Possession = true;
                            _game.CurrentPlayerWithBall = player.DynamicPlayerPointer;
                            //check to see if this player had the ball before. If so, update the end time of the curret touch. If not, if not add a new touch
                            if (_game.CurrentPlayerWithBall == prevGame.CurrentPlayerWithBall)
                            {
                                player.HasBall(_game.SecondsRemaining);
                            }
                            else
                            {
                                 player.GotBall(_game.CurrentQuarter,_game.SecondsRemaining);
                            }
                        }
                        else
                        {
                            player.Possession = false;
                        }

                    }

                    //Deal with lineups - first make sure there is a valid lineup to begin with
                    if (team.Players.Any(player => player.InGame == "*PG*-") && team.Players.Any(player => player.InGame == "*SG*-") && team.Players.Any(player => player.InGame == "*SF*-") && team.Players.Any(player => player.InGame == "*PF*-") && team.Players.Any(player => player.InGame == "*C*-"))
                    {
                        var currentPG = team.Players.FirstOrDefault(player => player.InGame == "*PG*-").DepthChartPos;
                        var currentSG = team.Players.FirstOrDefault(player => player.InGame == "*SG*-").DepthChartPos;
                        var currentSF = team.Players.FirstOrDefault(player => player.InGame == "*SF*-").DepthChartPos;
                        var currentPF = team.Players.FirstOrDefault(player => player.InGame == "*PF*-").DepthChartPos;
                        var currentC = team.Players.FirstOrDefault(player => player.InGame == "*C*-").DepthChartPos;

                        var currentLinup = team.Lineups.FirstOrDefault(lineup => lineup.PG == currentPG && lineup.SG == currentSG && lineup.SF == currentSF && lineup.PF == currentPF && lineup.C == currentC);
                        if (currentLinup == default(Lineup))
                        {
                            //first see if there is an already active lineup and if so deactivate it
                            var activeLineup = team.Lineups.FirstOrDefault(lineup => lineup.Active);
                            if (activeLineup != null)
                                activeLineup.Active = false;
                            //this means no matching lineup was found so we need to add it to the list of lineups and set it as active (happens by default)
                            //also include the game variable to be stored as the check in point for this new lineup
                            var pgName = team.Players.FirstOrDefault(d => d.DepthChartPos == currentPG).FullName;
                            var sgName = team.Players.FirstOrDefault(d => d.DepthChartPos == currentSG).FullName;
                            var sfName = team.Players.FirstOrDefault(d => d.DepthChartPos == currentSF).FullName;
                            var pfName = team.Players.FirstOrDefault(d => d.DepthChartPos == currentPF).FullName;
                            var cName = team.Players.FirstOrDefault(d => d.DepthChartPos == currentC).FullName;
                            var lineupDesc = pgName + "=" + sgName + "=" + sfName + "=" + pfName + "=" + cName;
                            team.Lineups.Add(new Lineup(currentPG, currentSG, currentSF, currentPF, currentC, _game.ToGameSituation(teamInt), lineupDesc, teamInt));
                        }
                        else
                        {
                            //this means that the lineup already exists 
                            //we need to see if it was already active - if it was then we need to add stats to it
                            //if it wasn't then we need to add stats to the one that was active and then activate this one
                            if (currentLinup.Active)
                            {
                                currentLinup.Playing(teamInt, _game.ToGameSituation(teamInt));
                            }
                            else
                            {
                                //this means an already existing lineup checked back in. need to deactivate the active one and check this one in
                                var activeLineup = team.Lineups.FirstOrDefault(lineup => lineup.Active);
                                if (activeLineup != null)
                                    activeLineup.Active = false;
                                currentLinup.CheckIn(teamInt, _game.ToGameSituation(teamInt));
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("NBA 2k22 Isn't Even Open!!");
            }
        }

        private string ExportStats(string exportFilePath, string fileFormat, string gameTime, string finalBoxPath, string finalPLayByPlayPath)
        {
            string filePath;
            //If this is an html file, just use Live.html, if not, use the timestamp and team names in the file name
            if (fileFormat == "html")
                filePath = exportFilePath + "Live.html";
            else
            {
                string baseFileName = "nba2k22stats_" + _game.StartTime.ToString("yyyyMMdd_HHmmss_");
                filePath = exportFilePath + baseFileName + _game.Teams[0].Name + "-" + _game.Teams[1].Name + "." + fileFormat;
            }
                    
            var boxScoreOutputFile = new StreamWriter(filePath);
                
            //If HTML format, then put the tags and meta data in to have it auto refresh
            if (fileFormat == "html")
            {
                boxScoreOutputFile.WriteLine(@"<!DOCTYPE html><html><head><meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" /><meta http-equiv=""refresh"" content=""" + _exportFrequency + @""">    <title>" + _game.Teams[0].Name + " vs " + _game.Teams[1].Name + " - Box Score</title></head><body><pre>");
                if (finalBoxPath != null)
                    boxScoreOutputFile.Write("FINAL - ");
            }
            //Output header
            boxScoreOutputFile.WriteLine(_game.Teams[0].Name + " vs " + _game.Teams[1].Name);
            boxScoreOutputFile.WriteLine(_game.Teams[1].Name + "'s Arena");
            boxScoreOutputFile.WriteLine(_game.StartTime.ToLongDateString() + ", " + _game.StartTime.ToLongTimeString() + " - [" + _game.GameTime + "]");
            boxScoreOutputFile.WriteLine();

            //Output scores
            //old output with team stat data: boxScoreOutputFile.WriteLine(FormatString, "", "Q1", "Q2", "Q3", "Q4", "OT", "Final", "", "", "PtsNPnt", "2ndCPts", "FBPts", "PtsTO", "Dunks", "", "", "", "", "", "", "");
            boxScoreOutputFile.WriteLine(FormatString, "", "Q1", "Q2", "Q3", "Q4", "OT", "Final", "", "", "", "", "", "", "", "", "", "","","","","","","");
            foreach (var team in _game.Teams)
                boxScoreOutputFile.WriteLine(team.ScoreToString(FormatString,gameTime));

            //Loop through teams for players
            foreach (var team in _game.Teams)
            {
                //put an extra line in the file for spacing
                boxScoreOutputFile.WriteLine();

                //write stat header for the team
                boxScoreOutputFile.WriteLine(FormatString, "Name", "Mins", "FG", "3P", "FT", "Points", "OffReb", "DefReb", "Reb", "Ast", "Stl", "Blk", "TO", "PF", "+/-", "PRF", "PtsNPnt", "2ndCPts", "FBPts", "PtsTO", "Dunks", "Touches", "TmWBall");

                //Loop through each player on the team
                foreach (var player in team.Players)
                {
                    boxScoreOutputFile.WriteLine(player.ToString(FormatString,gameTime));
                }
                boxScoreOutputFile.WriteLine(team.TotalsToString(FormatString));
                boxScoreOutputFile.WriteLine(team.PercentagesToString(FormatString));
            }
            //Loop through teams for lineups
            foreach (var team in _game.Teams)
            {
                //put an extra line in the file for spacing
                boxScoreOutputFile.WriteLine();

                //write stat header for the team
                boxScoreOutputFile.WriteLine(LineupFormatString, "Lineup", "Mins", "FG", "3P", "FT", "Points", "OffReb", "DefReb", "Reb", "Ast", "Stl", "Blk", "TO", "PF", "+/-", "PRF", "PtsNPnt", "2ndCPts", "FBPts", "PtsTO", "Dunks", "Apprncs");

                //Loop through each player on the team
                foreach (var lineup in team.Lineups)
                {
                    boxScoreOutputFile.WriteLine(lineup.TotalsToString(LineupFormatString));
                    boxScoreOutputFile.WriteLine(lineup.OppTotalsToString(LineupFormatString));
                }
                boxScoreOutputFile.WriteLine();
            }
            if (fileFormat == "txt")
            {
                boxScoreOutputFile.WriteLine();
                boxScoreOutputFile.WriteLine("Player of the Game: " + _game.PlayerOfTheGame);
            }
            if (fileFormat == "html")
            {
                if (finalBoxPath != null)
                {
                    boxScoreOutputFile.WriteLine();
                    boxScoreOutputFile.WriteLine(finalBoxPath.Split('\\').Last());
                    boxScoreOutputFile.WriteLine(finalPLayByPlayPath.Split('\\').Last());
                }
                boxScoreOutputFile.WriteLine(@"</pre></body></html>");
            }
            boxScoreOutputFile.Close();

            return filePath;
        }

        private string ExportPlayByPlay(string exportFilePath, string fileFormat)
        {
            //If this is an html file, just use Live.html, if not, use the timestamp and team names in the file name
            string filePath;
            if (fileFormat == "html")
                filePath = exportFilePath + "Live_pbyp.html";
            else
            {
                string baseFileName = "nba2k22stats_" + _game.StartTime.ToString("yyyyMMdd_HHmmss_");
                filePath = exportFilePath + baseFileName + _game.Teams[0].Name + "-" + _game.Teams[1].Name + "_pbyp." + fileFormat;
            }
            var playByPlayOutputFile = new StreamWriter(filePath);

            //If HTML format, then put the tags and meta data in to have it auto refresh
            if (fileFormat == "html")
                playByPlayOutputFile.WriteLine(@"<!DOCTYPE html><html>  <head>    <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" />    <meta http-equiv=""refresh"" content=""" + _exportFrequency + @""">    <title>" + _game.Teams[0].Name + " vs " + _game.Teams[1].Name + @" - Play-By-Play</title>  </head>  <body>    <pre>");

            //Output header
            playByPlayOutputFile.WriteLine(_game.Teams[0].Name + " vs " + _game.Teams[1].Name);
            playByPlayOutputFile.WriteLine(_game.Teams[1].Name + "'s Arena");
            playByPlayOutputFile.WriteLine(_game.StartTime.ToLongDateString() + ", " + _game.StartTime.ToLongTimeString() + " - [" + _game.GameTime + "]");

            string prevGameStatus = "";
            for (int g = _game.GameEvents.Count()-1; g >= 0 ;g-- )
            {
                var gameEvent = _game.GameEvents[g];
                if (gameEvent.GameStatus != prevGameStatus)
                    playByPlayOutputFile.WriteLine("----------");
                string eventText = "[" + gameEvent.GameStatus + "] [" + gameEvent.Team + "]" + gameEvent.Player + " [" + gameEvent.StatChange + "]";
                playByPlayOutputFile.WriteLine(eventText);
                prevGameStatus = gameEvent.GameStatus;
            }
            if (fileFormat == "html")
                playByPlayOutputFile.WriteLine(@"</pre> <a id=""bottom""></a> </body></html>");
            playByPlayOutputFile.Close();

            return filePath;
        }

        private string ExportIndex(string exportFilePath, string fileFormat, string gameTime, string finalBoxPath, string finalPLayByPlayPath)
        {
            string filePath;
            //If this is an html file, just use Live.html, if not, use the timestamp and team names in the file name
            if (fileFormat == "html")
                filePath = exportFilePath + "index.html";
            else
            {
                string baseFileName = "nba2k22stats_" + _game.StartTime.ToString("yyyyMMdd_HHmmss_");
                filePath = exportFilePath + baseFileName + _game.Teams[0].Name + "-" + _game.Teams[1].Name + "." + fileFormat;
            }

            var boxScoreOutputFile = new StreamWriter(filePath);

            //If HTML format, then put the tags and meta data in to have it auto refresh
            if (fileFormat == "html")
            {
                boxScoreOutputFile.WriteLine(@"<!DOCTYPE html><html><head><meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"" /><meta http-equiv=""refresh"" content=""" + _exportFrequency + @""">    <title>" + _game.Teams[0].Name + " vs " + _game.Teams[1].Name + " - Box Score</title></head><body><pre>");
                if (finalBoxPath != null)
                    boxScoreOutputFile.Write("FINAL - ");
            }
            //Output header
            boxScoreOutputFile.WriteLine(_game.Teams[0].Name + " vs " + _game.Teams[1].Name);
            boxScoreOutputFile.WriteLine(_game.Teams[1].Name + "'s Arena");
            boxScoreOutputFile.WriteLine(_game.StartTime.ToLongDateString() + ", " + _game.StartTime.ToLongTimeString() + " - [" + _game.GameTime + "]");
            boxScoreOutputFile.WriteLine();

            //Output scores
            //old output with team stat data: boxScoreOutputFile.WriteLine(FormatString, "", "Q1", "Q2", "Q3", "Q4", "OT", "Final", "", "", "PtsNPnt", "2ndCPts", "FBPts", "PtsTO", "Dunks", "", "", "", "", "", "", "");
            boxScoreOutputFile.WriteLine(FormatString, "", "Q1", "Q2", "Q3", "Q4", "OT", "Final", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "");
            foreach (var team in _game.Teams)
                boxScoreOutputFile.WriteLine(team.ScoreToString(FormatString, gameTime));

            //Loop through teams for players
            foreach (var team in _game.Teams)
            {
                //put an extra line in the file for spacing
                boxScoreOutputFile.WriteLine();

                //write stat header for the team
                boxScoreOutputFile.WriteLine(FormatString, "Name", "Mins", "FG", "3P", "FT", "Points", "OffReb", "DefReb", "Reb", "Ast", "Stl", "Blk", "TO", "PF", "+/-", "PRF", "PtsNPnt", "2ndCPts", "FBPts", "PtsTO", "Dunks", "Touches", "TmWBall");

                //Loop through each player on the team
                foreach (var player in team.Players)
                {
                    boxScoreOutputFile.WriteLine(player.ToString(FormatString, gameTime));
                }
                boxScoreOutputFile.WriteLine(team.TotalsToString(FormatString));
                boxScoreOutputFile.WriteLine(team.PercentagesToString(FormatString));
            }
            if (fileFormat == "txt")
            {//Loop through teams for lineups
                foreach (var team in _game.Teams)
                {
                    //put an extra line in the file for spacing
                    boxScoreOutputFile.WriteLine();

                    //write stat header for the team
                    boxScoreOutputFile.WriteLine(LineupFormatString, "Lineup", "Mins", "FG", "3P", "FT", "Points", "OffReb", "DefReb", "Reb", "Ast", "Stl", "Blk", "TO", "PF", "+/-", "PRF", "PtsNPnt", "2ndCPts", "FBPts", "PtsTO", "Dunks", "Apprncs");

                    //Loop through each player on the team
                    foreach (var lineup in team.Lineups)
                    {
                        boxScoreOutputFile.WriteLine(lineup.TotalsToString(LineupFormatString));
                        boxScoreOutputFile.WriteLine(lineup.OppTotalsToString(LineupFormatString));
                    }
                    boxScoreOutputFile.WriteLine();
                }

                boxScoreOutputFile.WriteLine();
                boxScoreOutputFile.WriteLine("Player of the Game: " + _game.PlayerOfTheGame);
            }
            if (fileFormat == "html")
            {
                string prevGameStatus = "";
                for (int g = _game.GameEvents.Count() - 1; g >= 0; g--)
                {
                    var gameEvent = _game.GameEvents[g];
                    if (gameEvent.GameStatus != prevGameStatus)
                        boxScoreOutputFile.WriteLine("----------");
                    string eventText = "[" + gameEvent.GameStatus + "] [" + gameEvent.Team + "]" + gameEvent.Player + " [" + gameEvent.StatChange + "]";
                    boxScoreOutputFile.WriteLine(eventText);
                    prevGameStatus = gameEvent.GameStatus;
                }
                //Loop through teams for lineups
                foreach (var team in _game.Teams)
                {
                    //put an extra line in the file for spacing
                    boxScoreOutputFile.WriteLine();

                    //write stat header for the team
                    boxScoreOutputFile.WriteLine(LineupFormatString, "Lineup", "Mins", "FG", "3P", "FT", "Points", "OffReb", "DefReb", "Reb", "Ast", "Stl", "Blk", "TO", "PF", "+/-", "PRF", "PtsNPnt", "2ndCPts", "FBPts", "PtsTO", "Dunks", "Apprncs");

                    //Loop through each player on the team
                    foreach (var lineup in team.Lineups)
                    {
                        boxScoreOutputFile.WriteLine(lineup.TotalsToString(LineupFormatString));
                        boxScoreOutputFile.WriteLine(lineup.OppTotalsToString(LineupFormatString));
                    }
                    boxScoreOutputFile.WriteLine();
                }
                if (finalBoxPath != null)
                {
                    boxScoreOutputFile.WriteLine();
                    boxScoreOutputFile.WriteLine(finalBoxPath.Split('\\').Last());
                    boxScoreOutputFile.WriteLine(finalPLayByPlayPath.Split('\\').Last());
                }
                boxScoreOutputFile.WriteLine(@"</pre></body></html>");
            }
            boxScoreOutputFile.Close();

            return filePath;
        }

        public static T Clone<T>(T realObject)
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, realObject);
                objectStream.Seek(0, SeekOrigin.Begin);
                return (T)formatter.Deserialize(objectStream);
            }
        }

        private void chkAutoOpen_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoOpenHTML = chkAutoOpen.Checked;
            Properties.Settings.Default.Save();
        }

        private void txtFolder_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.FilePath = txtFolder.Text;
            Properties.Settings.Default.Save();
        }

        private void chkAutoClose_CheckedChanged(object sender, EventArgs e)
        {

            Properties.Settings.Default.AutoClose = chkAutoClose.Checked;
            Properties.Settings.Default.Save();
        }

        private int SetInt(IntPtr processHandle, Int64 address, Int32 valueToSet, int offset = 0)
        {
            var value = BitConverter.GetBytes(valueToSet);
            int bytesWritten = 0;
            var addressPtr = new IntPtr(address + offset);
            WriteProcessMemory(processHandle, addressPtr, value, 8, out bytesWritten);

            return bytesWritten;
        }

        /// <summary>
        /// this method gets the range of bytes, changes the specific bits within them and then sets those bytes to the end result
        /// </summary>
        /// <param name="address"></param>
        /// <param name="numBytes"></param>
        /// <param name="startBit"></param>
        /// <param name="numBits"></param>
        /// <param name="valueToSet"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private int SetBitsInInt(IntPtr processHandle, Int64 address, int numBytes, int startBit, int numBits, int valueToSet, int offset = 0)
        {
            var buffer = new byte[numBytes];
            IntPtr bytesRead;
            var addressPtr = new IntPtr(address + offset);
            ReadProcessMemory(processHandle, addressPtr, buffer, buffer.Length, out bytesRead);
            var bitArray = new BitArray(buffer);

            int[] valueToSetArray = new int[] { valueToSet };

            //get the bits for the value, then inject them into the bit array
            var setValueBitArray = new BitArray(valueToSetArray);

            //where we start in the value bit array it is the number of bytes times 8 and then subtract the number of bits expected. 
            //This is because if something is expected to have 6 bytes we don't want to set the first 2 (that are expected to be zero) and then the next 4 - we want to set the last 6

            for (int setValueBit = 0; setValueBit < numBits; setValueBit++)
            {
                var bitNumberInBytes = startBit + setValueBit;
                bitArray[bitNumberInBytes] = setValueBitArray[setValueBit];
            }
            var intValue = BitArrayToInt(bitArray, 0, bitArray.Length);
            var setByteValue = BitConverter.GetBytes(intValue);
            int bytesWritten = 0;
            WriteProcessMemory(processHandle, addressPtr, setByteValue, (uint)numBytes, out bytesWritten);

            return bytesWritten;
        }

        private int BitArrayToInt(BitArray bitArray, int startBit, int numBits)
        {
            int value;
            BitArray newBitArray = new BitArray(numBits);
            for (int x = 0; x < numBits; x++)
            {
                newBitArray[x] = bitArray[startBit + x];
            }
            int[] array = new int[1];
            newBitArray.CopyTo(array, 0);
            value = array[0];
            return value;
        }

        private void btnGameInput_Click(object sender, EventArgs e)
        {
            //wait for user to switch to 2k as active window
            Thread.Sleep(5000);

            //run start game procedure
            var input = new Input();
            input.StartGame((int)cboHomeTeam.SelectedValue, (int)cboAwayTeam.SelectedValue);

            //wait for game to start and then start extractor
            WaitOnGameLoaded();
            btnExport_Click(this, null);
        }

        private void chkAutoReplay_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoReplay = chkAutoReplay.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
