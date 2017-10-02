using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarketDisplayGUI.MessageFeed {
    /// <summary>
    /// Establishes a connection (or in this case, waits for the connection) to receive
    /// market event messages.
    /// </summary>
    internal class MessageFeed : IDisposable {

        public Action<Message> OnMessageReceived;

        // Active-object variables
        private ManualResetEventSlim signalShouldShutdown = new ManualResetEventSlim(false);
        private ManualResetEventSlim waitUntilActiveThreadShutdown = new ManualResetEventSlim(false);
                
        // Listener Address/Port
        private System.Net.IPAddress ListenerAddress;
        private int ListenerPort;
        private byte[] ReceiveBuffer = new byte[100];

        /// <summary>
        /// Establishes this feed which can accept connection
        /// to receive market event messages.
        /// </summary>
        /// <param name="listenerAddress"></param>
        /// <param name="listenerPort"></param>
        public MessageFeed(System.Net.IPAddress listenerAddress, int listenerPort, Action<Message> initialCallback) {
            this.ListenerAddress = listenerAddress;
            this.ListenerPort = listenerPort;
            if (initialCallback != null) {
                OnMessageReceived += initialCallback;
            }

            // establish listener socket
            var activeObjectThread = new Thread(ACTIVE_ListenerServerThread);
            activeObjectThread.SetApartmentState(ApartmentState.MTA); // not a UI thread;
            activeObjectThread.IsBackground = false; // Intentional, to show that the dispose/shutdown works
            activeObjectThread.Name = "MessageFeed ActiveObject Thread";
            activeObjectThread.Start();
        }

        public void Dispose() {
            this.signalShouldShutdown.Set();
            this.waitUntilActiveThreadShutdown.Wait();
        }

        /// <summary>
        /// Internal thread that listens for client connections.
        /// </summary>
        private void ACTIVE_ListenerServerThread() {
            var listenerSocket = new TcpListener(this.ListenerAddress, this.ListenerPort);
            listenerSocket.Start();
            ListenForClientConnections(listenerSocket);
            listenerSocket.Stop();

            waitUntilActiveThreadShutdown.Set();
            // ---- If there was a problem shutting down without checking on all the TcpClient
            //      threads, then all of those threads could be added to a list and this could
            //      call .Join on each one to make sure they've exited.
        }

        /// <summary>
        /// Waits and accepts one client connection at a time.        
        /// </summary>
        /// <param name="listener"></param>
        /// <remarks>
        /// Due to the nature of this test program, I see no reason to spin off accepted
        /// TcpClient sockets onto their own threads and otherwise accept more than one
        /// connection at a time. If that was needed... then that could be done without
        /// requiring changing any other functions other than this one.
        /// </remarks>
        private void ListenForClientConnections(TcpListener listener) {
            do {
                if (listener.Pending() == true) {
                    // tiny bit of race condition where the pending connetion could presumably
                    // be dropped by the client, between these two calls, causing this to 
                    // actually blocked
                    var tcpClient = listener.AcceptTcpClient();
                    var clientThread = new Thread(x => ACTIVE_RunClient(tcpClient));
                    clientThread.SetApartmentState(ApartmentState.MTA);
                    clientThread.IsBackground = false; // 
                    clientThread.Name = "MessageFeed Client Socket Thread";
                    clientThread.Start();        
                }
            } while (this.signalShouldShutdown.Wait(1) == false);
        }

        /// <summary>
        /// Receives messages for a single client.
        /// </summary>
        /// <param name="client"></param>
        private void ACTIVE_RunClient(TcpClient client) {
            using (var stream = client.GetStream()) {
                var parser = new MessageParser();

                do {
                    try {
                        int bytesRead = stream.Read(ReceiveBuffer, 0, ReceiveBuffer.Length);
                        var newMessages = parser.AddBytes(ReceiveBuffer, bytesRead);
                        RaiseMessagesReeived(newMessages);
                    } catch (System.IO.IOException) {
                        // can indicates that the socket has been closed. Exit this
                        // method so that the listener can accept new sockets
                        return;
                    }
                } while (client.Connected == true && this.signalShouldShutdown.Wait(1) == false);
            }
            client.Close();
        }

        private void RaiseMessagesReeived(List<Message> messages) {
            var callback = OnMessageReceived;
            if (callback != null) {
                for (int index = 0; index < messages.Count; index++) {
                    callback(messages[index]);
                }
            }
        }

    }
}
