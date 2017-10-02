using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MarketDisplayGUI.MessageFeed {
    /// <summary>
    /// Internal feed used for development and performance testing, when
    /// a real message feed is not available.
    /// </summary>
    /// <remarks>
    /// Not in the \InternalTests folder as it can be used by the
    /// program in deployment.
    /// </remarks>
    internal class InternalTestFeed : IDisposable {
        private System.Net.IPAddress ServerAddress;
        private int ServerPort;
        private TcpClient openClient;
        private NetworkStream openStream;

        private const int RANDOM_GENERATION_UPDATE_DELAY_IN_MILLISECONDS = 500;
        private System.Threading.Thread randomThread;
        private System.Threading.ManualResetEvent signalShouldShutdown = new System.Threading.ManualResetEvent(false);
        
        public InternalTestFeed(System.Net.IPAddress address, int port) {
            this.ServerAddress = address;
            this.ServerPort = port;
        }

        public void Dispose() {
            if (randomThread != null) {
                signalShouldShutdown.Set();
                randomThread.Join();
            }

            CloseConnection();
        }

        public void StartRandomGeneration() {
            if (randomThread == null) {
                randomThread = new System.Threading.Thread(ACTIVE_GenerateRandomMessages);
                randomThread.SetApartmentState(System.Threading.ApartmentState.MTA);
                randomThread.IsBackground = true; // false 
                randomThread.Name = "InternalTestFeed Random Message Generation";
                randomThread.Start();
            }
        }

        /// <summary>
        /// Used to send message. Primarily used internally,
        /// but exposed externally for any dev-time-testing.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(IEnumerable<Message> messages) {
            if (messages.Any() == false) return;

            // Appends new line before AND after, to indicate a full
            // message and to clear out any unfinished messages that may
            // have been partially transferred due to any connectivity issues
            var messageString = new System.Text.StringBuilder();
            messageString.Append("\n"); // Ensures that any leftover bytes from
                                        // any partially failed sends are cleared out
            foreach (var message in messages) {
                messageString.Append(message.ToString());
                messageString.Append("\n");
            }
            var bytes = System.Text.Encoding.Default.GetBytes(messageString.ToString());

            while (true) {
                EstablishConnection();
                bool success = SendBytes(bytes);
                if (success) return;

                // The send failed, so wait a moment and attempt to re-connect and re-send again
                System.Threading.Thread.Sleep(1);
            }
        }

        private List<Message> currentRandomMarketState;

        private void ACTIVE_GenerateRandomMessages() {
            // Generate initial messages
            currentRandomMarketState = new List<Message>(new[] {
                // these are intermixed to test the sorting by symbol and price
                new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 099, 2),
                new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 100, 4),
                new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.ADD, Message.BidAskEnum.ASK, 101, 6),
                new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 102, 0),
                new Message(Message.MessageTypeEnum.PRICE, "MSFT", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 99, 2),
                new Message(Message.MessageTypeEnum.PRICE, "MSFT", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 100, 4),
                new Message(Message.MessageTypeEnum.ORDER, "IBM", Message.ActionEnum.ADD, Message.BidAskEnum.ASK, 101, 15),
                new Message(Message.MessageTypeEnum.PRICE, "MSFT", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 101, 0),
                new Message(Message.MessageTypeEnum.PRICE, "MSFT", Message.ActionEnum.ADD, Message.BidAskEnum.ASK, 101, 6),
                new Message(Message.MessageTypeEnum.ORDER, "MSFT", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 99, 15),
            });
            this.SendMessage(currentRandomMarketState);

            // For this, rather than some half-competent data generation that changes data
            // intelligentally based on current market state... I'll just define a set of market states that this loops through.
            Message[][] marketStates = new[] {
                new Message[] { 
                    new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.MODIFY, Message.BidAskEnum.BID, 100, 2),

                },

                new Message[] {
                    new Message(Message.MessageTypeEnum.ORDER, "MSFT", Message.ActionEnum.ADD, Message.BidAskEnum.ASK, 101, 42),
                },

                new Message[] {                    
                    new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.DELETE, Message.BidAskEnum.BID, 100, 0),
                    //new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.DELETE, Message.BidAskEnum.ASK, 100, 0),
                    new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.MODIFY, Message.BidAskEnum.BID, 099, 5),
                    new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.MODIFY, Message.BidAskEnum.ASK, 100, 10 ),
                },

                new Message[] {
                    new Message(Message.MessageTypeEnum.ORDER, "MSFT", Message.ActionEnum.MODIFY, Message.BidAskEnum.BID, 98, 5),
                },

                new Message[] {                    
                    new Message(Message.MessageTypeEnum.ORDER, "MSFT", Message.ActionEnum.DELETE, Message.BidAskEnum.ASK, 101, 0),
                },

                new Message[] {                    
                    new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.DELETE, Message.BidAskEnum.ASK, 100, 0),
                    new Message(Message.MessageTypeEnum.PRICE, "IBM", Message.ActionEnum.ADD, Message.BidAskEnum.BID, 100, 8),
                },

                new Message[] {

                },

                new Message[] {
                    new Message(Message.MessageTypeEnum.ORDER, "MSFT", Message.ActionEnum.MODIFY, Message.BidAskEnum.BID, 98, 10),
                }

            };
                
            int currentMarketState = 0;
            do {
                this.SendMessage(marketStates[currentMarketState]);
                currentMarketState++;
                if (currentMarketState == marketStates.Length) currentMarketState = 0;

            } while (this.signalShouldShutdown.WaitOne(RANDOM_GENERATION_UPDATE_DELAY_IN_MILLISECONDS) == false);
        }

        private void EstablishConnection() {
            if (openClient != null) {
                if (openClient.Connected == true) {
                    return;
                } else {
                    CloseConnection();
                }
            }

            openClient = new TcpClient();

            try {
                openClient.Connect(this.ServerAddress, this.ServerPort);
                openStream = openClient.GetStream();                

            } catch (SocketException) {
                // This means it failed to open. Just return to the caller. Any
                // attempted Send will fail, and that send call will just loop
                // back around and attempt to connect again.
            }
        }

        private void CloseConnection() {
            if (openStream != null) {
                try {
                    openStream.Close();
                } catch (Exception) { }
                openStream = null;
            }
            if (openClient != null) {
                try {
                    openClient.Close();                    
                } catch (Exception) { }
                openClient = null;
            }
        }


        private bool SendBytes(byte[] bytes) {
            if (openStream == null) return false;

            try {
                //var task = openStream.WriteAsync(bytes, 0, bytes.Length);
                //task.Wait();

                openStream.Write(bytes, 0, bytes.Length);
                return true;
            } catch (System.IO.IOException) {
                // Send failure
                return false;
            }            
        }
    }
}
