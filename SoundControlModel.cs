using Har_reader.Properties;
using Microsoft.Win32;
using System;
using System.IO;
using System.Media;

namespace Har_reader
{
    public class SoundControlModel : Proper
    {
        public enum SoundEnum
        {
            AlertSnd,
            WinSnd,
            LoseSnd
        }
        private bool defAlertChecked;
        private bool defWinChecked;
        private bool defLoseChecked;
        private string winPath;
        private string alertPath;
        private string losePath;
        private OpenFileDialog ofd;
        private CommandHandler playcmd;
        SoundPlayer ad = new SoundPlayer(Resources.alert);
        SoundPlayer wd = new SoundPlayer(Resources.win);
        SoundPlayer ld = new SoundPlayer(Resources.lose);
        SoundPlayer a = new SoundPlayer();
        SoundPlayer w = new SoundPlayer();
        SoundPlayer l = new SoundPlayer();
        public bool DefAlertChecked { get => defAlertChecked; set => SetProperty(ref defAlertChecked, value); }
        public bool DefLoseChecked { get => defLoseChecked; set => SetProperty(ref defLoseChecked, value); }
        public bool DefWinChecked { get => defWinChecked; set => SetProperty(ref defWinChecked, value); }
        public string AlertPath { get => alertPath; set { SetProperty(ref alertPath, value); SetProperty("AlertName"); a.SoundLocation = value; if (!string.IsNullOrEmpty(value)) DefAlertChecked = false; } }
        public string AlertName => Path.GetFileName(AlertPath);
        public string WinPath { get => winPath; set { SetProperty(ref winPath, value); SetProperty("WinName"); w.SoundLocation = value; if (!string.IsNullOrEmpty(value)) DefWinChecked = false; } }
        public string WinName => Path.GetFileName(WinPath);
        public string LosePath { get => losePath; set { SetProperty(ref losePath, value); SetProperty("LoseName"); l.SoundLocation = value; if (!string.IsNullOrEmpty(value)) DefLoseChecked = false; } }
        public string LoseName => Path.GetFileName(LosePath);
        public SoundPlayer GetSound(SoundEnum type)
        {
            switch (type)
            {
                case SoundEnum.AlertSnd:
                    return DefAlertChecked ? ad : a;
                case SoundEnum.WinSnd:
                    return DefWinChecked ? wd : w;
                case SoundEnum.LoseSnd:
                    return DefLoseChecked ? ld : l;
                default:
                    return null;
            }
        }
        public SoundControlModel()
        {
            ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.RestoreDirectory = true;
            ofd.DefaultExt = "wav";
            ofd.Filter = "Wav sound (*.wav)|*.wav";
            ofd.InitialDirectory = Environment.CurrentDirectory;
            AlertPath = Settings.Default.AlertPathWav;
            LosePath = Settings.Default.LosePathWav;
            WinPath = Settings.Default.WinPathWav;
            DefAlertChecked = Settings.Default.AlertUseDef;
            DefWinChecked = Settings.Default.WinUseDef;
            DefLoseChecked = Settings.Default.LoseUseDef;
        }
        public void SaveSettings()
        {
            Settings.Default.AlertPathWav = AlertPath;
            Settings.Default.LosePathWav = LosePath;
            Settings.Default.WinPathWav = WinPath;
            Settings.Default.AlertUseDef = DefAlertChecked;
            Settings.Default.WinUseDef = DefWinChecked;
            Settings.Default.LoseUseDef = DefLoseChecked;
            Settings.Default.Save();
        }
        public CommandHandler PlayCommand
        {
            get
            {
                return playcmd ??= new CommandHandler(obj =>
                {
                    switch (obj as string)
                    {
                        case "A":
                            if (DefAlertChecked)
                                ad.Play();
                            else
                                a.Play();
                            break;
                        case "L":
                            if (DefLoseChecked)
                                ld.Play();
                            else
                                l.Play();
                            break;
                        case "W":
                            if (DefWinChecked)
                                wd.Play();
                            else
                                w.Play();
                            break;
                    }
                },
                (obj) => true
                );
            }
        }
        public void SetPath(string tag)
        {
            switch (tag)
            {
                case "A":
                    ofd.Title = "Select alert sound";
                    break;
                case "W":
                    ofd.Title = "Select win sound";
                    break;
                case "L":
                    ofd.Title = "Select lose sound";
                    break;
            }
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    switch (tag)
                    {
                        case "A":
                            AlertPath = ofd.FileName;
                            var na = $"{Environment.CurrentDirectory}\\{AlertName}";
                            AlertPath = na;
                            File.Copy(AlertPath, na, true);
                            break;
                        case "W":
                            WinPath = ofd.FileName;
                            var nw = $"{Environment.CurrentDirectory}\\{WinName}";
                            WinPath = nw;
                            File.Copy(WinPath, nw, true);
                            break;
                        case "L":
                            LosePath = ofd.FileName;
                            var nl = $"{Environment.CurrentDirectory}\\{LoseName}";
                            LosePath = nl;
                            File.Copy(LosePath, nl, true);
                            break;
                    }
                }
                catch (IOException ex)
                {
                    if (!ex.Message.Contains("The process cannot access the file"))
                        throw;
                }

            }
        }




    }
}
