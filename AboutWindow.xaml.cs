using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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

namespace FcomGui
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			this.Resources.Add("client_version", GetClientVersion());
			InitializeComponent();
		}

		/// <summary>
		/// Handles hyperlinks
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
			e.Handled = true;
		}

		private static string GetClientVersion()
		{
			try
			{
				string versionNumber = AssemblyName.GetAssemblyName("FcomClient.exe").Version.ToString();
				return versionNumber.Substring(0, versionNumber.Length-2);
			}
			catch (System.IO.FileNotFoundException)
			{
				return "null (FcomClient.exe not found!)";
			}
		}

	}

}
