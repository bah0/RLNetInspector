using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Threading;


namespace NamedPipeListener
{
    public class NamedPipeListener
    {
        private NamedPipeClientStream pipeClient;
        private string serverName, pipeName;
        private bool reconnectOnDisconnect;
        private Task task;
        private bool stopRequested;

        public NamedPipeListener(string serverName, string pipeName, bool reconnect = false)
        {
            this.serverName = serverName;
            this.pipeName = pipeName;
            pipeClient = new NamedPipeClientStream(serverName, pipeName, PipeDirection.In,PipeOptions.Asynchronous);
            reconnectOnDisconnect = reconnect;
        }

        public void Start(Action<int, byte[]> action)
        {
            task = new Task((Action)delegate
            {
                int failedAttempts = 0;
                int maxFailedAttempts = 100;
                while (reconnectOnDisconnect && !stopRequested)
                {
                    do
                    {
                        if (failedAttempts > maxFailedAttempts)
                        {
                            Console.WriteLine("Connection Failed after 30 attempts. Exiting");
                            stopRequested = true;
                            break;
                        }

                        try
                        {
                            if (!pipeClient.IsConnected)
                                Connect();
                        }
                        catch (TimeoutException ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Timed Out: {0}", pipeName);
                            failedAttempts++;
                            //throw ex;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("semaphore timeout");
                            //throw ex;
                        }
                        finally
                        {
                        }
                    }
                    while (!pipeClient.IsConnected);
                    failedAttempts = 0;

                    processNetworkPacket(ref action);

                }

                Console.WriteLine("exit task");
                pipeClient.Close();
            });

            Console.WriteLine("Starting Task for: {0}", pipeName);
            task.Start();
        }

        private void Connect()
        {
            pipeClient.Connect(1000);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Connected Pipe {0}", pipeName);
            Console.ResetColor();
        }

        private void processNetworkPacket(ref Action<int,byte[]> action)
        {
            byte[] pipeBuffer = new byte[4096];
            MemoryStream memoryStream = new MemoryStream();

            while (pipeClient.IsConnected && !stopRequested)
            {

                int readBytes = pipeClient.Read(pipeBuffer, 0, pipeBuffer.Length);

                action(readBytes, pipeBuffer); // callback func to make it generic

                memoryStream.Write(pipeBuffer, 0, readBytes);
                memoryStream.Flush();

                Console.WriteLine("{0} Read bytes {1}", pipeName, readBytes);

            }
        }

        public void requestStop() => stopRequested = true;
        public void Wait() => task.Wait();
        public void Close() => pipeClient.Close();

    }
}
