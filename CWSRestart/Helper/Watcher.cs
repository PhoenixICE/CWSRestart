﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace CWSRestart.Helper
{
    public sealed class Watcher : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly Watcher instance = new Watcher();

        Timer watcher;

        public void Dispose()
        {
            watcher.Stop();
            watcher.Dispose();
            GC.SuppressFinalize(this);
        }


        private Watcher()
        {
            watcher = new Timer(1000);
            watcher.Elapsed += watcher_Elapsed;
        }

        async void watcher_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (ServerService.Helper.General.Working)
            {
                IsBlocked = true;
            }
            else
            {
                CurrentStep++;
                IsBlocked = false;

                if (CurrentStep > IntervallSeconds)
                {
                    watcher.Stop();
                    IsBlocked = true;

                    Helper.Logging.OnLogMessage("Time to check if the server is still running", ServerService.MessageType.Info);

                    ServerService.ServerErrors errors = await ServerService.Validator.Instance.Validates(ServerService.Helper.Settings.Instance.IgnoreAccess);

                    if (errors != 0)
                    {
                        Helper.Logging.OnLogMessage("A restart is required", ServerService.MessageType.Info);

                        if (!errors.HasFlag(ServerService.ServerErrors.ProcessDead))
                        {
                            ServerService.Helper.General.RestartServer();
                        }
                        else
                        {
                            ServerService.Helper.General.StartServer();
                        }
                    }

                    CurrentStep = 0;
                    IsBlocked = false;
                    watcher.Start();
                }
            }
        }

        public static Watcher Instance
        {
            get
            {
                return instance;
            }
        }

        #region intervall
        private UInt32 intervallSeconds = Settings.Instance.WatcherTimeout;

        public UInt32 IntervallSeconds
        {
            get
            {
                return intervallSeconds;
            }
            set
            {
                if (intervallSeconds != value)
                {
                    if (value == 0)
                        value = 60;

                    intervallSeconds = value;
                    notifyPropertyChanged();

                    Settings.Instance.WatcherTimeout = value;
                }
            }
        }
        #endregion

        #region isRunning
        private bool isRunning = false;

        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            private set
            {
                if (isRunning != value)
                {
                    isRunning = value;

                    if (isRunning)
                        ButtonText = "Stop watcher";
                    else
                        ButtonText = "Start watcher";

                    notifyPropertyChanged();
                }
            }
        }

        private string buttonText = "Start watcher";

        public string ButtonText
        {
            get
            {
                return buttonText;
            }
            private set
            {
                if (buttonText != value)
                {
                    buttonText = value;
                    notifyPropertyChanged();
                }
            }
        }
        #endregion

        #region isBlocked
        private bool isBlocked = false;

        public bool IsBlocked
        {
            get
            {
                return isBlocked;
            }
            private set
            {
                if (isBlocked != value)
                {
                    isBlocked = value;
                    notifyPropertyChanged();
                }
            }
        }
        #endregion

        #region current step
        private int currentStep = 0;
        public int CurrentStep
        {
            get
            {
                return currentStep;
            }
            private set
            {
                if (currentStep != value)
                {
                    currentStep = value;
                    notifyPropertyChanged();
                }
            }
        }
        #endregion

        public void Toggle()
        {
            if (!watcher.Enabled)
            {
                Logging.OnLogMessage("Watcher started", ServerService.MessageType.Info);
                watcher.Start();
            }
            else
            {
                Logging.OnLogMessage("Watcher stopped", ServerService.MessageType.Info);
                watcher.Stop();
            }

            IsRunning = !IsRunning;
        }

        public void Start()
        {
            if (!watcher.Enabled)
            {
                Logging.OnLogMessage("Watcher started", ServerService.MessageType.Info);
                watcher.Start();
                IsRunning = true;
            }
        }

        public void Stop()
        {
            if (watcher.Enabled && !IsBlocked)
            {
                Logging.OnLogMessage("Watcher stopped", ServerService.MessageType.Info);
                watcher.Stop();
                IsRunning = false;
            }
        }

        private void notifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
