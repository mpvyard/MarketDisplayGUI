using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MarketDisplayGUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private DispatcherTimer myTimer = new DispatcherTimer();
        private ConcurrentQueue<MessageFeed.Message> myPendingMessages = new ConcurrentQueue<MessageFeed.Message>();

        // All products currently displayed. They could be combined into a single class
        private ObservableCollection<DisplayedProduct> displayedProducts = new ObservableCollection<DisplayedProduct>();
        // dictionary version so that updates do not need to search through all available products.
        private Dictionary<String, DisplayedProduct> cachedPriceData = new Dictionary<String, DisplayedProduct>();
        private PriceWindowTracker priceWindowTracker = new PriceWindowTracker();

        private MessageFeed.MessageFeed openFeed;
        private MessageFeed.InternalTestFeed testFeed;

        public MainWindow(System.Net.IPAddress listenerIpAddress, int listenerPort, bool useInternalTestFeed) {
            InitializeComponent();

            // GUI update timer
            this.myTimer.Tick += myTimer_Tick;
            this.myTimer.Interval = TimeSpan.FromMilliseconds(20);
            this.myTimer.Start();

            this.openFeed = new MessageFeed.MessageFeed(listenerIpAddress, listenerPort, OnMessageReceived);

            this.marketViewDataGrid.ItemsSource = displayedProducts;

            // this should be controlled by a popup GUI
            if (useInternalTestFeed) {
                this.testFeed = new MessageFeed.InternalTestFeed(listenerIpAddress, listenerPort);
                this.testFeed.StartRandomGeneration();
            }
        }

        /// <summary>
        /// Used to centralize GUI updates instead of relying on external triggers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// </remarks>
        void myTimer_Tick(object sender, EventArgs e) {
            var timeOfUpdate = DateTime.Now;

            // dequeue all product updates
            MessageFeed.Message message = null;
            while (this.myPendingMessages.TryDequeue(out message)) {
                if (this.cachedPriceData.ContainsKey(message.GetIDKey()) == false) {
                    InsertNewPriceSorted(message);
                }
                this.cachedPriceData[message.GetIDKey()].UpdateWith(message, timeOfUpdate);

                List<Double> additionalPricesToAdd = this.priceWindowTracker.CheckMessage(message);
                if (additionalPricesToAdd.Count > 0) {
                    foreach (double newPrice in additionalPricesToAdd) {
                        var newMessage = new MessageFeed.Message(
                            MessageFeed.Message.MessageTypeEnum.PRICE,
                            message.Symbol,
                            MessageFeed.Message.ActionEnum.ADD,
                            MessageFeed.Message.BidAskEnum.BID,
                            newPrice,
                            0
                        );
                        InsertNewPriceSorted(newMessage);
                    }
                }
            }

            // Loop over all products to reset any data-changed times 
            // **NOTE** This has O(n) complexity, which if that was undesirable... it could be restructured
            //          to store all change times, objects, and properties in a queue where it would merely
            //          process any that have expired and whose color must be changed back. But that is a
            //          moderately complex solution for an evaluation project...
            foreach (var displayedProduct in this.displayedProducts) {
                displayedProduct.ClearChangedTimes(timeOfUpdate);
            }
        }

        private void InsertNewPriceSorted(MessageFeed.Message message) {
            var newDisplayedPrice = new DisplayedProduct(message.Symbol, message.Price);
            this.cachedPriceData[message.GetIDKey()] = newDisplayedPrice;

            // Sequential search to find correct location to add
            for (int index = 0; index < displayedProducts.Count; index++) {
                if ((newDisplayedPrice.Symbol.CompareTo(this.displayedProducts[index].Symbol) < 0)
                    || (newDisplayedPrice.Symbol == this.displayedProducts[index].Symbol && this.displayedProducts[index].Price < newDisplayedPrice.Price)) {
                    this.displayedProducts.Insert(index, newDisplayedPrice);
                    return;
                }
            }
            // To get here, it must be sorted after all products
            this.displayedProducts.Add(newDisplayedPrice);
        }
        
        private void OnMessageReceived(MessageFeed.Message message) {
            this.myPendingMessages.Enqueue(message);
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e) {
            if (this.testFeed != null) {
                this.testFeed.Dispose();
                this.testFeed = null;
            }

            if (this.openFeed != null) {
                this.openFeed.Dispose();
                this.openFeed = null;
            }
        }
    }
}
