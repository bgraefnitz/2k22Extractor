using System;
using System.Threading;
using static _2k21Extractor.Keyboard;

namespace _2k21Extractor
{
    class Input
    {
        private Keyboard keyboard;
        private readonly int interMenuDelay = Properties.Settings.Default.InterMenuDelay;
        private readonly int intraMenuDelay = Properties.Settings.Default.IntraMenuDelay;
        private readonly int keyUpDelay = Properties.Settings.Default.KeyUpDelay;

        public Input()
        {
            keyboard = new Keyboard();
        }

        public void RestartGame(int homeTeamId, int awayTeamId)
        {
            QuitCompletedGame();
            StartGame(homeTeamId, awayTeamId);
        }

        public void StartGame(int homeTeamId, int awayTeamId)
        {
            //this assumes starting within play now screen
            GoToQuickGame();
            LoadRosters();
            SelectTeams(homeTeamId, awayTeamId);
        }

        public void Continue()
        {
            SendKey(ScanCodeShort.SPACE);
        }

        private void GoToQuickGame()
        {
            SendKey(ScanCodeShort.SPACE);
            //wait for choose teams to load
            Thread.Sleep(interMenuDelay);
        }

        private void QuitCompletedGame()
        {
            SendKey(ScanCodeShort.KEY_W);
            SendKey(ScanCodeShort.SPACE);
            Thread.Sleep(interMenuDelay);
        }

        private void LoadRosters()
        {
            //go into load roster menu
            SendKey(ScanCodeShort.NUMPAD4);
            //move right to user created
            SendKey(ScanCodeShort.KEY_D);
            //select user created
            SendKey(ScanCodeShort.SPACE);
            //select top roster
            SendKey(ScanCodeShort.SPACE);
            //TODO: figure out non-top roster loading
        }

        private void SelectTeams(int homeTeamId, int awayTeamId)
        {
            //move to home team (defaults to center after roster load)
            SendKey(ScanCodeShort.KEY_D);
            //choose home team (defaults to sixers after roster load - so base 0)
            for(var i = 0; i < homeTeamId;  i++)
            {
                SendKey(ScanCodeShort.SQUARE_BRACKET_RIGHT);
            }
            //move to away team
            SendKey(ScanCodeShort.KEY_A);
            SendKey(ScanCodeShort.KEY_A);
            //choose away team (defaults to bucks after roster load - so base 1 - move so it is base 0)
            SendKey(ScanCodeShort.SQUARE_BRACKET_LEFT);
            for (var i = 0; i < awayTeamId; i++)
            {
                SendKey(ScanCodeShort.SQUARE_BRACKET_RIGHT);
            }
            //controller back to middle
            SendKey(ScanCodeShort.KEY_D);
            //start the game
            SendKey(ScanCodeShort.SPACE);
        }

        private void SendKey(ScanCodeShort scanCode)
        {
            keyboard.SendKey(scanCode);
            //Sleep is so that the game registers the keypress before keyup
            Thread.Sleep(keyUpDelay);
            keyboard.SendKeyUp(scanCode);
            //also sleeping here to make sure it doesn't collide with next key being sent or anything
            Thread.Sleep(intraMenuDelay);
        }
    }
}
