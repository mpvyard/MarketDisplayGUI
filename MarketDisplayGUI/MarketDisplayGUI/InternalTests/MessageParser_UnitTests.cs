using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketDisplayGUI.InternalTests {
    [TestClass]
    public class MessageParser_UnitTests {

        private MessageFeed.MessageParser parser;

        [TestInitialize]
        public void Setup() {
            parser = new MessageFeed.MessageParser();
        }

        private void AssertMessage(
            MessageFeed.Message message,
            MessageFeed.Message.MessageTypeEnum messageType,
            string symbol,
            MessageFeed.Message.ActionEnum action,
            MessageFeed.Message.BidAskEnum bidAsk,
            double price,
            int quantity) {

            Assert.AreEqual(messageType, message.MessageType);
            Assert.AreEqual(symbol, message.Symbol);
            Assert.AreEqual(action, message.Action);
            Assert.AreEqual(bidAsk, message.BidAsk);
            Assert.AreEqual(price, message.Price);
            Assert.AreEqual(quantity, message.Quantity);
        }
        
        [TestMethod]
        public void OneMessage_Test() {
            var bytes = System.Text.Encoding.Default.GetBytes("PRICE:IBM:ADD:BID:100:23\r\n");

            var messages = parser.AddBytes(bytes, bytes.Length);
            Assert.AreEqual(1, messages.Count);
            AssertMessage(messages[0], MessageFeed.Message.MessageTypeEnum.PRICE, "IBM", MessageFeed.Message.ActionEnum.ADD, MessageFeed.Message.BidAskEnum.BID, 100, 23);     
        }
                
        [TestMethod]
        public void BadMessage_Test() {
            var bytes = System.Text.Encoding.Default.GetBytes("PRICE:IBM:FOO:BID:100:23\r\n");

            var message = parser.AddBytes(bytes, bytes.Length);
            Assert.AreEqual(0, message.Count);
        }

        [TestMethod]
        public void TwoMessages_Test() {
            var bytes = System.Text.Encoding.Default.GetBytes(
                "PRICE:IBM:ADD:BID:100:23\r\n" +
                "ORDER:MSFT:MODIFY:ASK:99.5:29\r\n"
            );

            var messages = parser.AddBytes(bytes, bytes.Length);
            Assert.AreEqual(2, messages.Count);
            AssertMessage(messages[0], MessageFeed.Message.MessageTypeEnum.PRICE, "IBM", MessageFeed.Message.ActionEnum.ADD, MessageFeed.Message.BidAskEnum.BID, 100, 23);
            AssertMessage(messages[1], MessageFeed.Message.MessageTypeEnum.ORDER, "MSFT", MessageFeed.Message.ActionEnum.MODIFY, MessageFeed.Message.BidAskEnum.ASK, 99.5, 29);     
        }

        // two messages in three separate pieces
        [TestMethod]
        public void TwoMessage_InThreePieces_Test() {
            var bytes1 = System.Text.Encoding.Default.GetBytes("PRICE:IBM:ADD:B");
            var bytes2 = System.Text.Encoding.Default.GetBytes("ID:100:23\r\nORDER");
            var bytes3 = System.Text.Encoding.Default.GetBytes(":MSFT:MODIFY:ASK:99.5:29\r\n");

            var messages1 = parser.AddBytes(bytes1, bytes1.Length);
            Assert.AreEqual(0, messages1.Count);
            
            var messages2 = parser.AddBytes(bytes2, bytes2.Length);
            Assert.AreEqual(1, messages2.Count);
            AssertMessage(messages2[0], MessageFeed.Message.MessageTypeEnum.PRICE, "IBM", MessageFeed.Message.ActionEnum.ADD, MessageFeed.Message.BidAskEnum.BID, 100, 23);

            var messages3 = parser.AddBytes(bytes3, bytes3.Length);
            Assert.AreEqual(1, messages3.Count);
            AssertMessage(messages3[0], MessageFeed.Message.MessageTypeEnum.ORDER, "MSFT", MessageFeed.Message.ActionEnum.MODIFY, MessageFeed.Message.BidAskEnum.ASK, 99.5, 29);
        }

        // good-bad-good message
        [TestMethod]
        public void GoodBadGoodMessage_Test() {
            var bytes = System.Text.Encoding.Default.GetBytes(
                "PRICE:IBM:ADD:BID:100:23\r\n" +
                "PRICE:MSFT:BID:100:23\r\n" +
                "ORDER:MSFT:MODIFY:ASK:99.5:29\r\n"
            );

            var messages = parser.AddBytes(bytes, bytes.Length);
            Assert.AreEqual(2, messages.Count);
            AssertMessage(messages[0], MessageFeed.Message.MessageTypeEnum.PRICE, "IBM", MessageFeed.Message.ActionEnum.ADD, MessageFeed.Message.BidAskEnum.BID, 100, 23);
            AssertMessage(messages[1], MessageFeed.Message.MessageTypeEnum.ORDER, "MSFT", MessageFeed.Message.ActionEnum.MODIFY, MessageFeed.Message.BidAskEnum.ASK, 99.5, 29);     
        }

    }
}
