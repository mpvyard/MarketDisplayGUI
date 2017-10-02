using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketDisplayGUI {
    /// <summary>
    /// Wraps products so that the datagrid can bind to each field and reduce GUI load with updates.
    /// </summary>
    public class DisplayedProduct : INotifyPropertyChanged {
        private const int CHANGED_TIME_TIMEOUT_IN_MILLISECONDS = 1000; // Not a great place for this here, but good enough for now.

        public string Symbol { get; private set; }
        public double Price { get; private set; }

        public int BidSize { get; private set; }
        public DateTime? BidSize_ChangedTime { get; private set; }
        public int AskSize { get; private set; }
        public DateTime? AskSize_ChangedTime { get; private set; }

        public int BidOrders { get; private set; }
        public DateTime? BidOrders_ChangedTime { get; private set; }
        public int AskOrders { get; private set; }
        public DateTime? AskOrders_ChangedTime { get; private set; }

        internal DisplayedProduct(string symbol, double price) {
            this.Symbol = symbol;
            this.Price = price;
        }

        /// <summary>
        /// Updates the state of this product with the latest message.
        /// </summary>
        /// <param name="message"></param>
        /// <remarks>
        /// I could see this needing to exist as a more formal MarketStatus system
        /// that takes messages and holds the whole known market state. For the
        /// requirements provided though, this eems sufficient.
        /// </remarks>
        internal void UpdateWith(MessageFeed.Message message, DateTime timeOfUpdate) {
            if (this.Symbol != message.Symbol || this.Price != message.Price) {
                // this should not happen, but definitely want to crash hard if it ever did
                throw new InvalidOperationException("Attempted to update a DisplayedProduct with the wrong Symbol or Price");
            }

            int newValue = 0;
            switch (message.Action) {
                case MessageFeed.Message.ActionEnum.ADD:
                    newValue = message.Quantity;
                    break;
                case MessageFeed.Message.ActionEnum.MODIFY:
                    newValue = message.Quantity;
                    break;
                case MessageFeed.Message.ActionEnum.DELETE:
                    newValue = 0;
                    break;
            }

            if (message.MessageType == MessageFeed.Message.MessageTypeEnum.ORDER) {
                if (message.BidAsk == MessageFeed.Message.BidAskEnum.BID) {
                    if (this.BidOrders != newValue) {
                        this.BidOrders = newValue;
                        this.BidOrders_ChangedTime = timeOfUpdate;
                        RaisePropertyChanged("BidOrders");
                        RaisePropertyChanged("BidOrders_ChangedTime");
                    }
                } else {
                    if (this.AskOrders != newValue) {
                        this.AskOrders = newValue;
                        this.AskOrders_ChangedTime = timeOfUpdate;
                        RaisePropertyChanged("AskOrders");
                        RaisePropertyChanged("AskOrders_ChangedTime");
                    }
                }
            } else if (message.MessageType == MessageFeed.Message.MessageTypeEnum.PRICE) {
                if (message.BidAsk == MessageFeed.Message.BidAskEnum.BID) {
                    if (this.BidSize != newValue) {
                        this.BidSize = newValue;
                        this.BidSize_ChangedTime = timeOfUpdate;
                        RaisePropertyChanged("BidSize");
                        RaisePropertyChanged("BidSize_ChangedTime");
                    }
                } else {
                    if (this.AskSize != newValue) {
                        this.AskSize = newValue;
                        this.AskSize_ChangedTime = timeOfUpdate;
                        RaisePropertyChanged("AskSize");
                        RaisePropertyChanged("AskSize_ChangedTime");
                    }
                }
            }
        }

        public void ClearChangedTimes(DateTime timeOfUpdate) {
            if (BidOrders_ChangedTime.HasValue &&
                timeOfUpdate.Subtract(BidOrders_ChangedTime.Value).TotalMilliseconds > CHANGED_TIME_TIMEOUT_IN_MILLISECONDS) {
                BidOrders_ChangedTime = null;
                RaisePropertyChanged("BidOrders_ChangedTime");
            }

            if (AskOrders_ChangedTime.HasValue &&
                timeOfUpdate.Subtract(AskOrders_ChangedTime.Value).TotalMilliseconds > CHANGED_TIME_TIMEOUT_IN_MILLISECONDS) {
                AskOrders_ChangedTime = null;
                RaisePropertyChanged("AskOrders_ChangedTime");
            }

            if (BidSize_ChangedTime.HasValue &&
                timeOfUpdate.Subtract(BidSize_ChangedTime.Value).TotalMilliseconds > CHANGED_TIME_TIMEOUT_IN_MILLISECONDS) {
                BidSize_ChangedTime = null;
                RaisePropertyChanged("BidSize_ChangedTime");
            }

            if (AskSize_ChangedTime.HasValue &&
                timeOfUpdate.Subtract(AskSize_ChangedTime.Value).TotalMilliseconds > CHANGED_TIME_TIMEOUT_IN_MILLISECONDS) {
                AskSize_ChangedTime = null;
                RaisePropertyChanged("AskSize_ChangedTime");
            }
        }


        private void RaisePropertyChanged(string propertyName) {
            var callback = PropertyChanged;
            if (callback != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
