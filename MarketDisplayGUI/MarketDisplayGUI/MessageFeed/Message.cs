using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketDisplayGUI.MessageFeed {
    /// <summary>
    /// A receied message, representing an event in the state of the market
    /// </summary>
    /// <remarks>
    /// Could see this being a protocol buffer in a production application.
    /// Properties are read-only so the same instance could be used by multiple threads.
    /// </remarks>
    internal class Message {
        public enum MessageTypeEnum {
            PRICE,
            ORDER,
        }

        public enum ActionEnum {
            ADD,
            MODIFY,
            DELETE
        }

        public enum BidAskEnum {
            BID,
            ASK
        }

        public MessageTypeEnum MessageType { get; private set; }
        public String Symbol { get; private set; }
        public ActionEnum Action { get; private set; }
        public BidAskEnum BidAsk { get; private set; }
        public Double Price { get; private set; }
        public Int32 Quantity { get; private set; }

        public Message(
            MessageTypeEnum messageType,
            String symbol,
            ActionEnum action,
            BidAskEnum bidAsk,
            Double price,
            Int32 quantity) {

            this.MessageType = messageType;
            this.Symbol = symbol;
            this.Action = action;
            this.BidAsk = bidAsk;
            this.Price = price;
            this.Quantity = quantity;
        }

        public override string ToString() {
            return String.Format(
                "{0}:{1}:{2}:{3}:{4}:{5}",
                this.MessageType,
                this.Symbol,
                this.Action,
                this.BidAsk,
                this.Price,
                this.Quantity
            );
        }

        /// <summary>
        /// Returns a string that uniquely identifies this particular product
        /// </summary>
        /// <returns></returns>
        public string GetIDKey() {
            return this.Symbol + this.Price.ToString();
        }

    }
}
