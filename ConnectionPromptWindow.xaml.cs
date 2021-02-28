using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FcomGui
{
    /// <summary>
    /// Interaction logic for ConnectionPromptWindow.xaml
    /// </summary>
    public partial class ConnectionPromptWindow : Window
    {
        public ConnectionPromptWindow()
        {
            InitializeComponent();
        }

		private void SubmitConnectionChoice(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
    }
}
