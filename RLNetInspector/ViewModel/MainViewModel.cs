using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLNetInspector.Model;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Windows;
using System.ComponentModel;
using System.Timers;
using System.Windows.Documents;

namespace RLNetInspector.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        int pingCount = 0, pongCount = 0;
        private IStreamListener rlListener;
        private string savedFile = "";
        private string connectionStat = "";
        private WSPacket _selectedPacket;

        public RLNetPaketModel PacketModel { get; set; } = new RLNetPaketModel();
        public WSPacket SelectedPacket { get => _selectedPacket; set { _selectedPacket = value; updateDetailsView(); } }
        public ICommand ConnectServer { get; set; }
        public ICommand SaveFile { get; set; }
        public ICommand OpenFile { get; set; }
        public ICommand RequestLoaded { get; set; }
        public ICommand Exit { get; set; }

        public ICommand ClearList { get; set; }

        public DetailsPage requestPage { get; set; }
        public DetailsPage responsePage { get; set; }
        public String StatusConnection { get => connectionStat; set { connectionStat = value; RaisePropertyChanged(nameof(StatusConnection)); } }
        public String StatusFile { get => savedFile; set { savedFile = value; RaisePropertyChanged(nameof(StatusFile)); } }
        public String FilterText { get; set; }

        public Timer guiTimer;

        public MainViewModel()
        {
            SelectedPacket = new WSPacket();
            rlListener = new SocketListener();
            assignSocketTask();

            ConnectServer = new RelayCommand(o => { StartConnection(); }, o => {
                if (rlListener.ConnectionStatus == ConnectionState.NotConnected)
                    return true;
                else
                    return false;
            });
            SaveFile = new RelayCommand(o => { saveFile(); }, o => true);
            OpenFile = new RelayCommand(o => { openFile(); }, o => true);
            ClearList = new RelayCommand(o => { PacketModel.RLNetPackets.Clear(); requestPage.SummaryRichTextBox.Document.Blocks.Clear(); responsePage.SummaryRichTextBox.Document.Blocks.Clear(); }, o => true);
            Exit = new RelayCommand(o => { App.Current.MainWindow.Close(); }, o => true);

            requestPage = new DetailsPage();
            requestPage.Loaded += LoadCompletedRequests;

            responsePage = new DetailsPage();
            responsePage.Loaded += LoadCompletedResponses;

            guiTimer = new Timer();
            guiTimer.Interval = 500;
            guiTimer.Elapsed += updateGUIbyTimer;
            guiTimer.Start();

        }

        public void updateGUIbyTimer(object o, ElapsedEventArgs e)
        {
            if (rlListener == null)
                return;

            StatusConnection = updateConnStatus(rlListener.ConnectionStatus);

        }

        public void updateDetailsView()
        {
            if (responsePage == null)
                return;
            if (requestPage == null)
                return;
            if (SelectedPacket == null)
                return;

            requestPage.SummaryRichTextBox.Document.Blocks.Clear();
            responsePage.SummaryRichTextBox.Document.Blocks.Clear();

            if (SelectedPacket.RequestHeader.Count > 0) {

                Paragraph p = new Paragraph();

                foreach(string key in SelectedPacket.RequestHeader.Keys)
                {
                    Bold b = new Bold(new Run(key+": "));
                    Run r = new Run(SelectedPacket.RequestHeader[key]);
                    p.Inlines.Add(b);
                    p.Inlines.Add(r);
                    p.Inlines.Add(new LineBreak());
                }
                requestPage.SummaryRichTextBox.Document.Blocks.Add(p);

                requestPage.SummaryRichTextBox.Document.Blocks.Add(new Paragraph(new Run(WSPacket.PrettyJson(SelectedPacket.RequestBody))));
            }

            if (SelectedPacket.ResponseHeader.Count < 1)
                return;

            Paragraph pr = new Paragraph();

            foreach (string key in SelectedPacket.ResponseHeader.Keys)
            {
                Bold b = new Bold(new Run(key+": "));
                Run r = new Run(SelectedPacket.ResponseHeader[key]);
                pr.Inlines.Add(b);
                pr.Inlines.Add(r);
                pr.Inlines.Add(new LineBreak());
            }
            responsePage.SummaryRichTextBox.Document.Blocks.Add(pr);

            responsePage.SummaryRichTextBox.Document.Blocks.Add(new Paragraph(new Run(WSPacket.PrettyJson(SelectedPacket.ResponseBody))));
        }

        public void LoadCompletedRequests(object o, RoutedEventArgs e)
        {

            requestPage.HeaderTitle.Text = "Request Details";
            
        }

        public void LoadCompletedResponses(object o, RoutedEventArgs e)
        {
            responsePage.HeaderTitle.Text = "Response Details";

            StatusConnection = "Not connected";
            StatusFile = "";
        }

        public string updateConnStatus(ConnectionState state)
        {
            if (rlListener == null)
                return null;

            switch (state)
            {
                case ConnectionState.Connecting:
                    return "Connecting";
                case ConnectionState.NotConnected:
                    return "Not connected";
                case ConnectionState.Connected:
                    return "Connected";
                case ConnectionState.Reconnecting:
                    return "Reconnecting";
                default:
                    return null;
            }
        }

        public void openFile()
        {
            if (PacketModel.RLNetPackets.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("Items are in list. Do you want to purge data and load file?", "Do you want to continue?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) 
                    return;
            }

            OpenFileDialog dialog = new OpenFileDialog();

            if(dialog.ShowDialog() == true)
            {
                StatusFile = dialog.FileName;
                string content = File.ReadAllText(StatusFile);

                List<WSPacket> packets = JsonConvert.DeserializeObject<List<WSPacket>>(content);

                PacketModel.RLNetPackets.Clear();
                packets.ForEach(p => { PacketModel.RLNetPackets.Add(p); });

            }
        }

        public void saveFile()
        {
            if (!File.Exists(StatusFile))
                StatusFile = "";
            
            if(StatusFile == "")
            {
                SaveFileDialog dialog = new SaveFileDialog();
                if(dialog.ShowDialog() == true)
                {
                    StatusFile = dialog.FileName;
                }
                
            }
            if (StatusFile == "")
                return;

            string jsonContent = JsonConvert.SerializeObject(PacketModel.RLNetPackets, Formatting.Indented);

            File.WriteAllText(StatusFile, jsonContent);
            MessageBox.Show("Saved");

        }

        public void StartConnection()
        {
            Console.WriteLine("connect clicked");
            Task.Run((Action)delegate
            {
                rlListener.ConnectOnRestart();
            });
        }

        private void assignSocketTask()
        {
            MemoryStream ms = new MemoryStream();
            bool lastMessageReceived = false;

            rlListener.ReadBytesAction((bytesRead, readBuffer) =>
            {
                lastMessageReceived = bytesRead < 32678;

                ms.Write(readBuffer, 0, bytesRead);

                if (!lastMessageReceived)
                    return;

                Console.WriteLine("Last Message received");

                string content = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)ms.Length);
                ms = new MemoryStream();
                bool messagefromServer = false;

                string[] entries = content.Split("\n".ToCharArray());
                Dictionary<string, string> headers = new Dictionary<string, string>();
                string reqbody = entries[entries.Length - 1].Trim(new char[] { ' ', '\r', '\n' }).TrimStart(new char[] { ' ', '\r', '\n' });

                if (entries.Length > 1)
                {

                    for (int i = 0; i < entries.Length - 1; i++)
                    {
                        if (entries[i] == "\r")
                            continue;
                        if (entries[i] == "\n")
                            continue;
                        if (entries[i] == " ")
                            continue;

                        string key = entries[i].Substring(0, entries[i].IndexOf(":"));
                        string val = entries[i].Substring(entries[i].IndexOf(":") + 1);
                        if (key == "PsyRequestID" || key == "PsyResponseID")
                        {
                            key = "PsyNetID";
                        }
                        if(!headers.ContainsKey("PsyNetID"))
                            headers.Add(key, val);
                    }

                    // process messages

                    if (headers.ContainsKey("PsyPing"))
                    {
                        PacketModel.AddPing("Ping_" + pingCount++, content);

                    }
                    else if (headers.ContainsKey("PsyPong"))
                    {
                        messagefromServer = true;
                        PacketModel.AddPong("Ping_" + pongCount++, content); // we use Ping here instead of Pong, to match the corresponding item in list
                    }
                    else if (headers.ContainsKey("Server"))
                    {
                        messagefromServer = true;
                        PacketModel.AddResponse(headers["PsyNetID"].Trim(), content);
                    } 
                    else // Requests
                    {
                        PacketModel.AddRequest(headers["PsyNetID"].Trim(), content);
                    }
                }
            });

        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
