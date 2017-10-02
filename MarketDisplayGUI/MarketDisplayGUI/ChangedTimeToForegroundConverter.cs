using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketDisplayGUI {
    /// <summary>
    /// If the cell was recently changed, this flashes the foreround black, and then otherwise
    /// reverts it back to the passed-in brush.
    /// </summary>
    public class ChangedTimeToForegroundConverter : System.Windows.Data.IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            // Skip the type-checks, they should not be necessary anyway.            

            DateTime? timeChanged = value as DateTime?;
            if (timeChanged.HasValue == false) {
                return parameter;
            } else {
                return System.Windows.Media.Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            // not needed
            throw new NotImplementedException();
        }
    }
}
