using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

public class ServiceBody
{
	private Socket ConnectSocket;
	private Thread ServeThread;

	//CommandDate[0] : 0 buy, 1 sell
	//CommandData[1] : 0 TX, 1 MTX
	//CommandData[2~4] : price, little endian, 0xffffffff(a.k.a -1) is market order
	private byte ExecCommandStr(byte[] CommandData)
	{
		int k;

		k = BitConverter.ToInt32(CommandData, 2);
		Console.WriteLine("1:" + CommandData[0].ToString());
		Console.WriteLine("2:" + CommandData[1].ToString());
		Console.WriteLine("3: {0:X}", k);
		return 0;
	}

	private void ThreadProc()
	{
		Encoding ascii = Encoding.ASCII;
		byte[] RecvBuffer = new byte[6];
		byte[] Ret = new byte[1];
		int len;

		while(true)
		{
			len = ConnectSocket.Receive(RecvBuffer);
			if (len != 0)
			{
				Console.WriteLine("*** receive length {0} bytes", len);
				Ret[0] = ExecCommandStr(RecvBuffer);
				ConnectSocket.Send(Ret, 1, SocketFlags.None);
			} else {
				ConnectSocket.Shutdown(SocketShutdown.Both);
				ConnectSocket.Close();
				Console.WriteLine("Socket Closed");
				return;
			}
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
