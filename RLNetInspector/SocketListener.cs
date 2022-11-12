using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace RLNetInspector
{
    public class SocketListener : IStreamListener
    {
        private Socket socket;
        private IPEndPoint endPoint;
        private IPAddress address;
        private Task socketTask;
        private bool lastConnectedStatus = false;
        public Action<int, byte[]> readBytesAction { get; set; }

        public ConnectionState ConnectionStatus { get; set; } = ConnectionState.NotConnected;

        public SocketListener(string address = "127.0.0.1", int Port = 4041, bool connectOnStart = false)
        {
            this.address = IPAddress.Parse(address);
            endPoint = new IPEndPoint(this.address, Port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if (connectOnStart)
                Connect();

        }

        public void ConnectOnRestart()
        {
            while (true)
            {
                try
                {
                    if (this.Connected)
                    {
                        Console.WriteLine("Socket already connected");
                        return;
                    }

                    if (lastConnectedStatus != socket.Connected)
                    {
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    }

                    Console.WriteLine("Socket connecting");
                    ConnectionStatus = ConnectionState.Connecting;
                    socket.Connect(endPoint);
                    lastConnectedStatus = Connected;

                    if (!socket.Connected)
                        continue;

                    Console.WriteLine("Socket connected");
                    ConnectionStatus = ConnectionState.Connected;

                    if (socketTask != null)
                        socketTask.Dispose();

                    socketTask = Task.Run((Action)delegate
                    {
                        receiveData();
                    });

                    socketTask.Wait();

                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Socket could not connect");
                    ConnectionStatus = ConnectionState.NotConnected;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                Console.WriteLine("Reconnecting socket");

                ConnectionStatus = ConnectionState.Reconnecting;
            }
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        private void receiveData()
        {
            try
            {
                byte[] readBuffer = new byte[32678];
                int bytesRead = 0;

                do
                {
                    bytesRead = socket.Receive(readBuffer, readBuffer.Length, SocketFlags.None);
                    Console.WriteLine("Bytes Read:{0}", bytesRead);
                    readBytesAction(bytesRead, readBuffer);

                } while (bytesRead > 0);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Socket not connected anymore");
            }
            finally
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(true);
            }
        }

        public void ReadBytesAction(Action<int, byte[]> action)
        {
            readBytesAction = action;
        }
        public bool Connected { get => socket.Connected; }
        public void Close()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(true);
            socket.Close();
        }
        public void Dispose()
        {
            Close();
            readBytesAction = null;
            socket = null;
        }

    }
}
