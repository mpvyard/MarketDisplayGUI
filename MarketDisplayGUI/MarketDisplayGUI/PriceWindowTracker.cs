using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketDisplayGUI {
    /// <summary>
    /// Keeps track of which prices are displayed per symbol, based on the best bid/ask on the market
    /// </summary>
    /// <remarks>
    /// I keep track of prices in messages as double, but to prevent any problems with 
    /// significant digits creating invalid prices, this converts them all to integers internally.
    /// </remarks>
    internal class PriceWindowTracker {
        private const int PRICE_WINDOW = 10;

        internal class SymbolState {
            public int? bestBidPrice;
            public int? bestAskPrice;
            public int? highestPrice;
            public int? lowestPrice;
        }

        private Dictionary<string, SymbolState> symbolStates = new Dictionary<string, SymbolState>();

        public List<Double> CheckMessage(MessageFeed.Message message) {
            // Update the internal state of prices that exist for each symbol
            if (symbolStates.ContainsKey(message.Symbol) == false) {
                symbolStates[message.Symbol] = new SymbolState();
            }
            var symbolState = symbolStates[message.Symbol];

            if (message.BidAsk == MessageFeed.Message.BidAskEnum.BID && message.Quantity > 0) {
                if (symbolState.bestBidPrice.HasValue == false || message.Price > symbolState.bestBidPrice.Value) {
                    symbolState.bestBidPrice = (int)message.Price;
                }
            } else if (message.BidAsk == MessageFeed.Message.BidAskEnum.ASK && message.Quantity > 0) {
                if (symbolState.bestAskPrice.HasValue == false || message.Price < symbolState.bestAskPrice.Value) {
                    symbolState.bestAskPrice = (int)message.Price;
                }
            }
            if (symbolState.highestPrice.HasValue == false || message.Price > symbolState.highestPrice) {
                symbolState.highestPrice = (int)message.Price;
            }
            if (symbolState.lowestPrice.HasValue == false || message.Price < symbolState.lowestPrice) {
                symbolState.lowestPrice = (int)message.Price;
            }

            // Last, see if there should be any prices added due to movement of the bestBid/bestAsk
            var newPrices = new List<Double>();
            if (symbolState.highestPrice.HasValue == false || symbolState.lowestPrice.HasValue == false) {
                // This should not happen, but if it does, exit the algorithm now.
                return newPrices;
            }

            int expectedLowest = symbolState.highestPrice.Value;
            int expectedHighest = symbolState.highestPrice.Value;
            if (symbolState.bestBidPrice.HasValue == true) {
                //expectedHighest = Math.Max(expectedHighest, symbolState.bestBidPrice.Value + PRICE_WINDOW);
                expectedLowest = Math.Min(expectedLowest, symbolState.bestBidPrice.Value - PRICE_WINDOW);
            }
            if (symbolState.bestAskPrice.HasValue == true) {
                expectedHighest = Math.Max(expectedHighest, symbolState.bestAskPrice.Value + PRICE_WINDOW);
                //expectedLowest = Math.Min(expectedLowest, symbolState.bestAskPrice.Value - PRICE_WINDOW);
            }
            expectedLowest = Math.Max(expectedLowest, 0); // Cap at 0; no negative prices.
                        
            for (int price = expectedLowest; price <= expectedHighest; price += 1) {
                if (price < symbolState.lowestPrice || price > symbolState.highestPrice) {
                    newPrices.Add((double)price);
                }
            }
            symbolState.lowestPrice = expectedLowest;
            symbolState.highestPrice = expectedHighest;

            return newPrices;
        }
    }
}
