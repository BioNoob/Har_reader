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
            LoseSnd,
            CrushSound
        }
        private bool defAlertChecked;
        private bool defWinChecked;
        private bool defLoseChecked;
        private bool defCrushChecked;
        private bool muteAlertChecked;
        private bool muteWinChecked;
        private bool muteLoseChecked;
        private bool muteCrushChecked;
        private string winPath;
        private string alertPath;
        private string losePath;
        private string crushPath;
        private OpenFileDialog ofd;
        private CommandHandler playcmd;
        SoundPlayer ad = new SoundPlayer(Resources.alert);
        SoundPlayer wd = new SoundPlayer(Resources.win);
        SoundPlayer ld = new SoundPlayer(Resources.lose);
        SoundPlayer cd = new SoundPlayer(Resources.crush);
        SoundPlayer a = new SoundPlayer();
        SoundPlayer w = new SoundPlayer();
        SoundPlayer l = new SoundPlayer();
        SoundPlayer c = new SoundPlayer();
        public bool DefAlertChecked { get => defAlertChecked; set => SetProperty(ref defAlertChecked, value); }
        public bool DefLoseChecked { get => defLoseChecked; set => SetProperty(ref defLoseChecked, value); }
        public bool DefWinChecked { get => defWinChecked; set => SetProperty(ref defWinChecked, value); }
        public bool DefCrushChecked { get => defCrushChecked; set => SetProperty(ref defCrushChecked, value); }
        public bool MuteAlertChecked { get => muteAlertChecked; set => SetProperty(ref muteAlertChecked, value); }
        public bool MuteLoseChecked { get => muteLoseChecked; set => SetProperty(ref muteLoseChecked, value); }
        public bool MuteWinChecked { get => muteWinChecked; set => SetProperty(ref muteWinChecked, value); }
        public bool MuteCrushChecked { get => muteCrushChecked; set => SetProperty(ref muteCrushChecked, value); }
        public string AlertPath { get => alertPath; set { SetProperty(ref alertPath, value); SetProperty("AlertName"); a.SoundLocation = value; if (!string.IsNullOrEmpty(value)) DefAlertChecked = false; } }
        public string AlertName => Path.GetFileName(AlertPath);
        public string WinPath { get => winPath; set { SetProperty(ref winPath, value); SetProperty("WinName"); w.SoundLocation = value; if (!string.IsNullOrEmpty(value)) DefWinChecked = false; } }
        public string WinName => Path.GetFileName(WinPath);
        public string LosePath { get => losePath; set { SetProperty(ref losePath, value); SetProperty("LoseName"); l.SoundLocation = value; if (!string.IsNullOrEmpty(value)) DefLoseChecked = false; } }
        public string LoseName => Path.GetFileName(LosePath);
        public string CrushPath { get => crushPath; set { SetProperty(ref crushPath, value); SetProperty("CrushName"); c.SoundLocation = value; if (!string.IsNullOrEmpty(value)) DefCrushChecked = false; } }
        public string CrushName => Path.GetFileName(CrushPath);
        public SoundPlayer GetSound(SoundEnum type)
        {
            switch (type)
            {
                case SoundEnum.AlertSnd:
                    if (!MuteAlertChecked)
                        return DefAlertChecked ? ad : a;
                    else
                        return null;
                case SoundEnum.WinSnd:
                    if (!MuteWinChecked)
                        return DefWinChecked ? wd : w;
                    else
                        return null;
                case SoundEnum.LoseSnd:
                    if (!MuteLoseChecked)
                        return DefLoseChecked ? ld : l;
                    else
                        return null;
                case SoundEnum.CrushSound:
                    if (!MuteCrushChecked)
                        return DefCrushChecked ? cd : c;
                    else
                        return null;
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

            AlertPath = Sound.Default.AlertPathWav;
            LosePath = Sound.Default.LosePathWav;
            WinPath = Sound.Default.WinPathWav;
            CrushPath = Sound.Default.CrushPathWav;

            DefAlertChecked = Sound.Default.AlertUseDef;
            DefWinChecked = Sound.Default.WinUseDef;
            DefLoseChecked = Sound.Default.LoseUseDef;
            DefCrushChecked = Sound.Default.CrushUseDef;

            MuteAlertChecked = Sound.Default.MuteAlertSound;
            MuteCrushChecked = Sound.Default.MuteCrushSound;
            MuteLoseChecked = Sound.Default.MuteLoseSound;
            MuteWinChecked = Sound.Default.MuteWinSound;
        }
        public void SaveSettings()
        {
            Sound.Default.AlertPathWav = AlertPath;
            Sound.Default.LosePathWav = LosePath;
            Sound.Default.WinPathWav = WinPath;
            Sound.Default.CrushPathWav = CrushPath;

            Sound.Default.AlertUseDef = DefAlertChecked;
            Sound.Default.WinUseDef = DefWinChecked;
            Sound.Default.LoseUseDef = DefLoseChecked;
            Sound.Default.CrushUseDef = DefCrushChecked;

            Sound.Default.MuteAlertSound = MuteAlertChecked;
            Sound.Default.MuteCrushSound = MuteCrushChecked;
            Sound.Default.MuteLoseSound = MuteLoseChecked;
            Sound.Default.MuteWinSound = MuteWinChecked;

            Sound.Default.Save();
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
                        case "CR":
                            if (DefCrushChecked)
                                cd.Play();
                            else
                                c.Play();
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
                case "CR":
                    ofd.Title = "Select crush sound";
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
                        case "CR":
                            CrushPath = ofd.FileName;
                            var ncr = $"{Environment.CurrentDirectory}\\{CrushName}";
                            CrushPath = ncr;
                            File.Copy(CrushPath, ncr, true);
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
