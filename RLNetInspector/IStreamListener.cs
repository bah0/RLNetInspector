using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RLNetInspector
{
    public interface IStreamListener : IDisposable
    {
        void Connect();
        void ConnectOnRestart();
        void ReadBytesAction(Action<int, byte[]> action);
        void Close();
        Action<int, byte[]> readBytesAction { get; set; }
        bool Connected { get; }

        ConnectionState ConnectionStatus { get; set; }

    }
    public enum ConnectionState
    {
        Connecting,
        NotConnected,
        Connected,
        Reconnecting
    }
}
