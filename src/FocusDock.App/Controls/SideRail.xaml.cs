using System.Windows;
using System.Windows.Controls;

namespace FocusDock.App.Controls
{
    /// <summary>
    /// Side rail navigation with expand-on-hover behavior.
    /// Collapsed: 64px • Expanded: 220px • Accent active indicator
    /// </summary>
    public partial class SideRail : System.Windows.Controls.UserControl
    {
        public SideRail()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event fired when any navigation button is clicked.
        /// Use the button's Name property to determine target page.
        /// </summary>
        public event RoutedEventHandler? NavigationRequested;

        private void OnNavigationClick(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button clickedButton) return;

            // Clear all active states
            ClearActiveStates();

            // Set clicked button as active
            clickedButton.Tag = "Active";

            // Raise navigation event for parent window to handle
            NavigationRequested?.Invoke(sender, e);
        }

        private void ClearActiveStates()
        {
            BtnHome.Tag = null;
            BtnCalendar.Tag = null;
            BtnTasks.Tag = null;
            BtnStudy.Tag = null;
            BtnAnalytics.Tag = null;
            BtnWorkspace.Tag = null;
            BtnAutomations.Tag = null;
            BtnSettings.Tag = null;
        }

        /// <summary>
        /// Programmatically set the active navigation item.
        /// </summary>
        /// <param name="buttonName">Name of the button (e.g., "BtnHome")</param>
        public void SetActiveItem(string buttonName)
        {
            ClearActiveStates();

            var button = FindName(buttonName) as System.Windows.Controls.Button;
            if (button != null)
            {
                button.Tag = "Active";
            }
        }
    }
}
