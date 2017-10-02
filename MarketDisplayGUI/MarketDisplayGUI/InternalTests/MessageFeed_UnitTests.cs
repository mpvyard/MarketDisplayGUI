using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarketDisplayGUI.InternalTests {
    /// <summary>
    /// Tests the Message Feed by using the internal test feed
    /// to send test data too confirm it is receiving.
    /// </summary>
    [TestClass]
    public class MessageFeed_UnitTests {

        private MessageFeed.MessageFeed openFeed;
        private MessageFeed.InternalTestFeed testSendFeed;
        
        private List<MessageFeed.Message> receivedMessages = new List<MessageFeed.Message>();
        private System.Threading.ManualResetEvent waitUntilMessagesReceived = new System.Threading.ManualResetEvent(false);

        [TestInitialize]
        public void Setup() {
            this.receivedMessages.Clear();
            this.waitUntilMessagesReceived.Reset();
            this.openFeed = new MessageFeed.MessageFeed(IPAddress.Loopback, 1337, OnReceivedMessage);
            this.testSendFeed = new MessageFeed.InternalTestFeed(IPAddress.Loopback, 1337);
        }

        [TestCleanup]
        public void Teardown() {
            this.testSendFeed.Dispose();
            this.openFeed.OnMessageReceived -= OnReceivedMessage;
            this.openFeed.Dispose();
        }
        
        private void OnReceivedMessage(MessageFeed.Message message) {
            lock (this.receivedMessages) {
                this.receivedMessages.Add(message);
                this.waitUntilMessagesReceived.Set();
            }
        }

        private MessageFeed.Message[] GetReceivedMessages() {
            lock (this.receivedMessages) {
                this.waitUntilMessagesReceived.Reset();
                var results = this.receivedMessages.ToArray();
                this.receivedMessages.Clear();
                return results;
            }
        }

        private MessageFeed.Message messageA = new MessageFeed.Message(
            MessageFeed.Message.MessageTypeEnum.PRICE,
            "IBM",
            MessageFeed.Message.ActionEnum.ADD,
            MessageFeed.Message.BidAskEnum.BID,
            100,
            23
        );
        private MessageFeed.Message messageB = new MessageFeed.Message(
            MessageFeed.Message.MessageTypeEnum.ORDER,
            "MSFT",
            MessageFeed.Message.ActionEnum.MODIFY,
            MessageFeed.Message.BidAskEnum.ASK,
            99.5,
            29
        );

        private void AssertMessageEqual(MessageFeed.Message expected, MessageFeed.Message actual) {
            Assert.AreEqual(expected.MessageType, actual.MessageType);
            Assert.AreEqual(expected.Symbol, actual.Symbol);
            Assert.AreEqual(expected.Action, actual.Action);
            Assert.AreEqual(expected.BidAsk, actual.BidAsk);
            Assert.AreEqual(expected.Price, actual.Price);
            Assert.AreEqual(expected.Quantity, actual.Quantity);
        }        

        [TestMethod]
        public void OneMessage_Test() {
            Assert.IsFalse(this.waitUntilMessagesReceived.WaitOne(0), "No messages should be waiting");
            this.testSendFeed.SendMessage(new[] { this.messageA });

            Assert.IsTrue(this.waitUntilMessagesReceived.WaitOne(100), "Never received message");
            var messages = GetReceivedMessages();
            Assert.AreEqual(1, messages.Length);
            AssertMessageEqual(this.messageA, messages[0]);
        }

        [TestMethod]
        public void TwoMessages_Test() {
            Assert.IsFalse(this.waitUntilMessagesReceived.WaitOne(0), "No messages should be waiting");
            this.testSendFeed.SendMessage(new[] { this.messageA });

            Assert.IsTrue(this.waitUntilMessagesReceived.WaitOne(100), "Never received message 1");
            var messages = GetReceivedMessages();
            Assert.AreEqual(1, messages.Length);
            AssertMessageEqual(this.messageA, messages[0]);

            this.testSendFeed.SendMessage(new[] { this.messageB });
            Assert.IsTrue(this.waitUntilMessagesReceived.WaitOne(100), "Never received message 2");
            messages = GetReceivedMessages();
            Assert.AreEqual(1, messages.Length);
            AssertMessageEqual(this.messageB, messages[0]);

        }

        // send one message, disconnect, reconnect, send the second
        [TestMethod]
        public void TwoSequentialSenders_Test() {
            Assert.IsFalse(this.waitUntilMessagesReceived.WaitOne(0), "No messages should be waiting");
            this.testSendFeed.SendMessage(new[] { this.messageA });

            Assert.IsTrue(this.waitUntilMessagesReceived.WaitOne(100), "Never received message 1");
            var messages = GetReceivedMessages();
            Assert.AreEqual(1, messages.Length);
            AssertMessageEqual(this.messageA, messages[0]);

            this.testSendFeed.Dispose();
            this.testSendFeed = new MessageFeed.InternalTestFeed(IPAddress.Loopback, 1337);

            this.testSendFeed.SendMessage(new[] { this.messageB });
            Assert.IsTrue(this.waitUntilMessagesReceived.WaitOne(1000), "Never received message 2");
            messages = GetReceivedMessages();
            Assert.AreEqual(1, messages.Length);
            AssertMessageEqual(this.messageB, messages[0]);
        }

        // two clients, both send message, receive both
        [TestMethod]
        public void TwoParallelSenders_Test() {
            using (var secondTestFeed = new MessageFeed.InternalTestFeed(IPAddress.Loopback, 1337)) {
                var waitToSendSimultaneously = new System.Threading.ManualResetEventSlim(false);

                CreateSenderThread(() => this.testSendFeed.SendMessage(new[] { this.messageA}), waitToSendSimultaneously);
                CreateSenderThread(() => secondTestFeed.SendMessage(new[] {this.messageB}), waitToSendSimultaneously);

                waitToSendSimultaneously.Set();

                Assert.IsTrue(this.waitUntilMessagesReceived.WaitOne(100));
                System.Threading.Thread.Sleep(10); // wait a little longer to make sure both are received
                var messages = this.GetReceivedMessages();
                Assert.AreEqual(2, messages.Length);                
            }
        }

        private void CreateSenderThread(Action action, System.Threading.ManualResetEventSlim signal) {
            var thread = new System.Threading.Thread(() => {
                signal.Wait();
                action();
            });
            thread.IsBackground = false;
            thread.SetApartmentState(System.Threading.ApartmentState.MTA);
            thread.Start();
        }

    }
}
