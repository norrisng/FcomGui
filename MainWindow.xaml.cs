using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
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
using RestSharp;

namespace FcomGui
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// PID of the FcomClient process. -1 is used as a sentinel value.
		/// </summary>
		private int clientPid = -1;

		// Track program state
		private bool isRegistered = false;
		private bool isCapturing = false;

		private readonly string FCOM_GUI_PIPE_NAME = "FcomGuiPipe";

		private Thread pipeServerThread;

		public MainWindow()
		{
			InitializeComponent();
			// TODO: Start pipe server here
		}

		private void OnKeyDownHandler(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				Register(sender);
		}

		/// <summary>
		/// Handles clicking of the "Register" button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RegisterButton_Click(object sender, RoutedEventArgs e)
		{
			Register(sender);
		}

		/// <summary>
		/// Takes the provided callsign and verification code, and passes them to FcomClient.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Register(object sender)
		{
			callsignInput.IsEnabled = false;
			tokenInput.IsEnabled = false;

			if (clientPid < 0)
			{
				pipeServerThread = new Thread(PipeServer);
				pipeServerThread.Start();
			}

			string callsign = callsignInput.Text;
			string token = tokenInput.Text;

			if (callsign == "" || token == "")
				MessageBox.Show("Please enter your callsign and/or verification code!");
			else if (clientPid >= 0)
				MessageBox.Show("Already registered and connected!");
			else
			{
				try
				{
					using (Process fcomProcess = new Process())
					{
						//fcomProcess.StartInfo.UseShellExecute = true;
						fcomProcess.StartInfo.FileName = "FcomClient.exe";
						fcomProcess.StartInfo.Arguments = String.Format("{0} {1}", callsign, token);
						fcomProcess.Start();
						clientPid = fcomProcess.Id;

						isRegistered = true;
						isCapturing = true;

						SetIsConnectedText(sender, true);
					}

				}
				catch (Exception ex)
				{
					if (ex.Message == "The system cannot find the file specified")
						MessageBox.Show("Could not find FcomClient.exe!");
					else
						MessageBox.Show(ex.Message);
				}

				// once the FcomClient process is running, fire up the pipe server


			}
		}
		/// <summary>
		/// Opens the "About" window
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AboutButton_Click(object sender, RoutedEventArgs e)
		{
			AboutWindow about = new AboutWindow();
			about.Show();
		}

		/// <summary>
		/// Event handler for the "Pause" button.
		/// Kills the FcomClient process started with "Register"
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void KillClient_Click(object sender, RoutedEventArgs e)
		{
			if (!KillClient())
				MessageBox.Show("Message forwarding not active!");

			else
			{
				SetIsConnectedText(sender, false);

				MessageBox.Show("Messages forwarding over Discord paused.\n" +
								"To resume forwarding, just click on \"Register\" again.");
			}

			callsignInput.IsEnabled = true;
		}
		/// <summary>
		/// Event handler for the "Stop Forwarding" button.
		/// Kills the FcomClient process started with "Register", 
		/// and sends a DELETE HTTP request to the API.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void KillAndDeregister_Click(object sender, RoutedEventArgs e)
		{
			KillClient();
			string server_address = System.IO.File.ReadAllText("server_location.txt");
			string deregister_uri = String.Format("api/v1/deregister/{0}", tokenInput.Text);

			var client = new RestClient(server_address);
			var request = new RestRequest(deregister_uri, Method.DELETE);
			client.UserAgent = "FcomGui/r20190329";
			IRestResponse response = client.Execute(request);
			var content = response.Content;

			callsignInput.IsEnabled = true;
			tokenInput.IsEnabled = true;

			if (string.Equals("ok", content))
				MessageBox.Show("Messages will no longer be forwarded over Discord.\n" +
								"To forward messages again, please get a new Discord code from the bot.");
			else
			{
				MessageBox.Show("Messages will no longer be forwarded over Discord.\n" +
								"Please double-check your status throught the Discord bot using the \"status\" command");
			}
		}

		private bool KillClient()
		{
			if (clientPid < 0)
			{
				// Registration was never started
				if (pipeServerThread != null)
					pipeServerThread.Abort();
				return false;
			}
			else
			{
				// retrieve based on PID and kill
				Process[] procList = Process.GetProcesses();

				foreach (Process p in procList)
				{
					if (p.Id == clientPid)
					{
						p.Kill();
						break;
					}
				}

				// Reset saved PID
				clientPid = -1;

				if (pipeServerThread != null)
					pipeServerThread.Abort();

				return true;
			}
		}

		private void Gui_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (pipeServerThread != null)
				pipeServerThread.Abort();
			KillClient();
			Environment.Exit(0);
		}

		private void LoadConnectionsList(object sender, RoutedEventArgs e)
		{
			List<string> connections = new List<string>();

			foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				connections.Add(nic.Name + "/");
			}

			var comboBox = sender as ComboBox;
			comboBox.ItemsSource = connections;
			comboBox.SelectedIndex = 0;
		}

		private void SetIsConnectedText(object sender, bool isConnected)
		{
			//if (isConnected)
			//	s.StatusText = "Connected!";
			//else
			//	s.StatusText = "";
		}

		private void PipeServer()
		{
			NamedPipeServerStream pipeServer
				= new NamedPipeServerStream("ca.norrisng.fcom", PipeDirection.InOut, 1, PipeTransmissionMode.Message);
			pipeServer.WaitForConnection();
			try
			{
				while (true)
				{					
					StringBuilder messageBuilder = new StringBuilder();
					string messageChunk = string.Empty;
					byte[] messageBuffer = new byte[5];

					// parse incoming pipe message
					do
					{
						pipeServer.Read(messageBuffer, 0, messageBuffer.Length);
						messageChunk = Encoding.UTF8.GetString(messageBuffer);
						messageBuilder.Append(messageChunk);
						messageBuffer = new byte[messageBuffer.Length];
					}
					while (!pipeServer.IsMessageComplete);

					string pipeMessage = messageBuilder.ToString();
					string userMessage = "";


					// Parse pipe messages and display MessageBox with error details
					// NOTE: null terminator ("\0") must be included

					if (pipeMessage.Equals("") || pipeMessage.Equals("\0"))
					{
						// don't display empty messages
					}
					else
					{
						if (pipeMessage.Equals("FCOM_CLIENT_CRASH\0"))
						{
							userMessage = "The FcomClient background service has crashed. Please restart FCOM." +
								"Further details on the crash can be found in log.txt";
						}
						else if (pipeMessage.Equals("FCOM_API_ERROR\0"))
						{
							userMessage = "Couldn't register with the bot. Please close the console window (if open) and restart FCOM.\n" +
										"Please check your internet connection or your Discord code (it may have expired).";
						}
						else if (pipeMessage.Equals("FCOM_CLIENT_BAD_CALLSIGN\0"))
							userMessage = "Callsign format invalid. Please follow the instructions in the console window.";
						else
							userMessage = pipeMessage;

						MessageBox.Show(userMessage);
					}
				}
			}
			// This Catch block allows the parent to safely call abort() on this thread
			catch (ThreadAbortException)
			{
				if (pipeServer != null)
					pipeServer.Dispose();
			}
		} 

	}

	//private void statusMonitor()
	//{

	//}



}
