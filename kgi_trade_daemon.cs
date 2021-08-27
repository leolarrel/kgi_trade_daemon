using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ServiceBody
{
	private Socket ConnectSocket;
	private Thread ServeThread;

	private void ThreadProc()
	{
		byte[] buffer = new byte[128];
		int len;

		while(true)
		{
			len = ConnectSocket.Receive(buffer);
			Console.WriteLine("get data len: " + len.ToString());
			if (len != 0)
			{
				ConnectSocket.Send(buffer, len, SocketFlags.None);
				continue;
			}

			ConnectSocket.Shutdown(SocketShutdown.Both);
			ConnectSocket.Close();
			Console.WriteLine("Socket Closed");
			return;
		}
	}

	public ServiceBody(Socket x)
	{
		ConnectSocket = x;
	}

	public void ServeBackground()
	{
		ServeThread = new Thread(ThreadProc);
		ServeThread.Start();
	}
}

public class ServiceMain
{
	public static void StartService()
	{
		string WelcomString = "KGI option trade TCP/IP Daemon";
		Console.WriteLine(WelcomString);

		//Socket will bind on any IPv4 endpoint system have
		IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 44444);
		try {
			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listener.Bind(localEndPoint);
			listener.Listen(10);

			while (true) {
				Socket client = listener.Accept();
				ServiceBody n = new ServiceBody(client);
				n.ServeBackground();
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}

	public static int Main(String[] args)
	{
		StartService();
		return 0;
	}
}
