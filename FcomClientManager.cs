using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace FcomGui
{

    class FcomClientManager : IDisposable
    {
		NamedPipeServerStream guiServer;
		private readonly string FCOM_GUI_PIPE_NAME = "FcomGuiPipe";

		public FcomClientManager()
		{
			guiServer = new NamedPipeServerStream(FCOM_GUI_PIPE_NAME);
			guiServer.WaitForConnection();	

		}

		private static readonly FcomClientManager instance = new FcomClientManager();

		public static FcomClientManager GetInstance()
		{
			return instance;
		}

		public void Dispose()
		{
			guiServer.Dispose();
		}



	}
}
