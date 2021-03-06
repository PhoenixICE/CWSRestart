﻿using ServerService.Access.Entries;
using ServerService.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerService.Access
{
    public enum AccessMode
    {
        Whitelist,
        Blacklist
    }

    public sealed class AccessControl : INotifyPropertyChanged
    {
        private static readonly AccessControl instance = new AccessControl();
        public event PropertyChangedEventHandler PropertyChanged;

        public static AccessControl Instance
        {
            get
            {
                return instance;
            }
        }

        private ObservableCollection<AccessListEntry> accessList = new ObservableCollection<AccessListEntry>();
        public ObservableCollection<AccessListEntry> AccessList
        {
            get
            {
                return accessList;
            }
            private set
            {
                if (accessList != value)
                {
                    accessList = value;
                    notifyPropertyChanged();
                }
            }
        }

        public void Enforce()
        {
            if (AccessList.Count > 0)
            {
                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] connectionInformation = ipGlobalProperties.GetActiveTcpConnections();

                IEnumerator enumerator = connectionInformation.GetEnumerator();

                Logging.OnLogMessage(String.Format("Enforcing {0}", Mode), MessageType.Info);

                while (enumerator.MoveNext())
                {
                    TcpConnectionInformation info = (TcpConnectionInformation)enumerator.Current;

                    if (info.LocalEndPoint.Port == Settings.Instance.Port && info.State == TcpState.Established)
                    {
                        int ret = 0;
                        bool actionTaken = false;
                        bool playerFound = PlayerInList(info);

                        switch (Mode)
                        {
                            case AccessMode.Whitelist:

                                if(!playerFound)
                                {
                                    ret = Helper.DisconnectWrapper.CloseRemoteIP(info.RemoteEndPoint.Address.ToString(), info.RemoteEndPoint.Port);
                                    actionTaken = true;
                                }
                                
                                break;

                            case AccessMode.Blacklist:

                                if(playerFound)
                                {
                                    ret = Helper.DisconnectWrapper.CloseRemoteIP(info.RemoteEndPoint.Address.ToString(), info.RemoteEndPoint.Port);
                                    actionTaken = true;
                                }

                                break;
                        }

                        if (actionTaken)
                        {
                            switch(ret)
                            {
                                case 0:
                                    Logging.OnLogMessage(String.Format("The user {0} has been kicked", info.RemoteEndPoint.Address.ToString()), MessageType.Info);
                                    break;
                                case 317:
                                    Logging.OnLogMessage(String.Format("Could not kick user {0}: Access denied", info.RemoteEndPoint.Address.ToString()), MessageType.Warning);
                                    break;
                                case 0x5:
                                    Logging.OnLogMessage(String.Format("Could not kick user {0}. You might lack the the required privileges.", info.RemoteEndPoint.Address.ToString()), MessageType.Warning);
                                    break;
                                case 0x57:
                                    Logging.OnLogMessage("Internal error: wrong parameter", MessageType.Error);
                                    break;
                                case 0x32:
                                    Logging.OnLogMessage("IPv4 Transport is not configured properly", MessageType.Error);
                                    break;
                                default:
                                    Logging.OnLogMessage(String.Format("Disconnecting returned with {0}", ret), MessageType.Info);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public bool PlayerInList(TcpConnectionInformation info)
        {
            foreach (AccessListEntry e in accessList)
            {
                if (e.Matches(info.RemoteEndPoint.Address))
                {
                    return true;
                }
            }
            return false;
        }

        public bool PlayerInList(string ip)
        {
            foreach (AccessListEntry e in accessList)
            {
                if (e.Matches(IPAddress.Parse(ip)))
                {
                    return true;
                }
            }
            return false;
        }

        private AccessMode mode = AccessMode.Blacklist;
        public AccessMode Mode
        {
            get
            {
                return mode;
            }
            set
            {
                if (mode != value)
                {
                    mode = value;
                    notifyPropertyChanged();
                }
            }
        }

        private AccessControl()
        {
        }

        private void notifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SaveList(string filepath)
        {
            StringBuilder sb = new StringBuilder();

            foreach (AccessListEntry e in AccessList)
                sb.AppendLine(e.ToString());

            File.WriteAllText(filepath, sb.ToString());
        }

        public void RestoreList(string filepath)
        {
            using (FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read))
            {
                AccessList = new ObservableCollection<AccessListEntry>();
                StreamReader sr = new StreamReader(fs);
                
                string line;
                while((line = sr.ReadLine()) != null)
                {
                    AccessListEntry e;
                    if (GenerateEntryFromString(line, out e))
                        if (!AccessList.Contains(e))
                            AccessList.Add(e);
                }

                //sr.Close();
            }
        }

        public static bool GenerateEntryFromString(string s, out AccessListEntry target)
        {
            if (!AccessIP.TryParse(s, out target))
                if (!AccessIPRange.TryParse(s, out target))
                    return false;

            return true;
        }

        public void SetAccessList(ObservableCollection<AccessListEntry> observableCollection)
        {
            AccessList = observableCollection;
        }
    }
}
