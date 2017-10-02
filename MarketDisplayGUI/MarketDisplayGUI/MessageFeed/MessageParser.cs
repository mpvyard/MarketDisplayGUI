using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketDisplayGUI.MessageFeed {
    /// <summary>
    /// Takes in various bytes (presumably read off of a socket) and converts them
    /// into Messages, storing the bytes for any unfinished messages until the
    /// rest of message is received.
    /// </summary>
    internal class MessageParser {
        
        private StringBuilder unfinishedMessage = new StringBuilder(); 

        public List<Message> AddBytes(byte[] bytes, int length) {
            var results = new List<Message>();

            // default encoding is ANSI, so this should end up receiving only one byte out of a multi-byte 
            // Unicode character
            var chars = System.Text.Encoding.Default.GetChars(bytes, 0, length);

            for (int index = 0; index < chars.Length; index++) {
                if (chars[index] == '\r' || chars[index] == '\n') {
                    var newMessage = ExtractCurrentMessage();
                    if (newMessage != null) results.Add(newMessage);
                } else {
                    unfinishedMessage.Append(chars[index]);
                }
            }

            return results;
        }

        /// <summary>
        /// Attempts to convert the current stringbuilder into a valid message. If it
        /// is invalid, then this returns null;
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This could definitely be more efficient by parsing out each byte into fields of the message
        /// instead of storing as a string and splitting it, but I have limited time to do this coding sample...
        /// </remarks>
        private Message ExtractCurrentMessage() {
            if (unfinishedMessage.Length == 0) {
                // non-existant message (Line Feed vs Carriage return? blank line?) Don't log an error here.
                return null;
            }

            var splits = unfinishedMessage.ToString().Split(':');
            unfinishedMessage.Clear();

            if (splits.Length != 6) {
                // ---- use more formal logging to log the issue of a corrupt message
                System.Diagnostics.Debug.Print("Received message was not 6 parts separate by a colon");
                return null;
            }

            Message message = null;
            try {
                message = new Message(
                    (Message.MessageTypeEnum)Enum.Parse(typeof(Message.MessageTypeEnum), splits[0]),
                    splits[1],
                    (Message.ActionEnum)Enum.Parse(typeof(Message.ActionEnum), splits[2]),
                    (Message.BidAskEnum)Enum.Parse(typeof(Message.BidAskEnum), splits[3]),
                    Double.Parse(splits[4]),
                    Int32.Parse(splits[5])
                );
            } catch (Exception ex) {
                // ---- use more formal logging in production
                System.Diagnostics.Debug.Print("Error parsing fields into message {0}", ex.Message);
                return null;
            }

            // Any remaining message quality validation
            if (message.Symbol.Length == 0) {
                // ---- use more formal logging in production
                System.Diagnostics.Debug.Print("Message as a zero-length symbol");
                return null;
            }

            return message;
        }
    }
}