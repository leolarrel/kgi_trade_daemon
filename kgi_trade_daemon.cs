using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using Intelligence;
using Smart;
using Package;

public class KgiTradeApi
{
	private TaiFexCom tfc;
	private UTF8Encoding encoding = new UTF8Encoding();

	public void TradeApiLog(string msg)
	{
		Console.WriteLine(msg);
		tfc.WriterLog(msg);
	}

	private void OnRcvMessage(object sender, PackageBase package)
	{
		Console.WriteLine("OnRcvMessage()");
	}

	private void OnGetStatus(object sender, COM_STATUS s, byte[] msg)
	{
		TradeApiLog("OnGetStatus()");

		TaiFexCom ApiSender = (TaiFexCom)sender;
		string Utf8Msg = null;
		string OMsg = null;
//		OMsg = String.Format("status {0}", s);
//		Console.WriteLine(OMsg);

		switch (s) {
			case COM_STATUS.LOGIN_READY:	//登入成功
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("login ready {0} {1}",  ApiSender.Accounts, Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.LOGIN_FAIL:	//登入失敗
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("login failed {0} {1}",  ApiSender.Accounts, Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.LOGIN_UNKNOW:	//登入狀態不明
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("login unknow {0} {1}",  ApiSender.Accounts, Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.CONNECT_READY:	//連線成功
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("connect ready [[{1}:{2}] [{3}]",
						     ApiSender.ServerHost, ApiSender.ServerPort, Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.CONNECT_FAIL:	//連線失敗
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("connect failed {0}", Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.DISCONNECTED:	//斷線
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("disconnected {0}", Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.SUBSCRIBE:
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("subscribe {0}", Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.UNSUBSCRIBE:
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("unsubscribe {0}", Utf8Msg);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.ACK_REQUESTID:	//下單或改單第一次回覆
				long RequestId = BitConverter.ToInt64(msg, 0);
				byte status = msg[8];

				OMsg = String.Format("ack requestid id:{0} status:{1}", RequestId, status);
				TradeApiLog(OMsg);
				break;

			case COM_STATUS.QUEUE_WARNING :
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("queue warning {0}", Utf8Msg);
				TradeApiLog(OMsg);
				break;

			default:
				Utf8Msg = encoding.GetString(msg);
				OMsg = String.Format("unsupport status {0} {1}",s , Utf8Msg);
				TradeApiLog(OMsg);
				break;
		}
	}

	private void OnRcvServerTime(Object sender, DateTime serverTime, int ConnQuality)
	{
		Console.WriteLine("OnRcvServerTime()");
	}

	private void OnRecoverStatus(object sender, string Topic, RECOVER_STATUS status, uint RecoverCount)
	{
		Console.WriteLine("OnRecoverStatus()");
	}

	public void Create()
	{
		tfc = new TaiFexCom("", 0, "API");

		Console.WriteLine("Kgi Trade api version " + tfc.version);
		tfc.OnRcvMessage += OnRcvMessage;
		tfc.OnGetStatus += OnGetStatus;
		tfc.OnRcvServerTime += OnRcvServerTime;
		tfc.OnRecoverStatus += OnRecoverStatus;
	}

	public void Login()
	{
		TradeApiLog("Login()");

		tfc.ServerHost = "211.20.186.12";
		tfc.ServerPort = 443;
		tfc.AutoRedirectPushLogin = false;
		tfc.ServerHost2 = "";
		tfc.ServerPort2 = 0;
		tfc.LoginTimeout = 8000;

		tfc.AutoRetriveProductInfo = true;
		tfc.AutoSubReport = true;
		tfc.AutoSubReportForeign = true;
		//tfc.AutoRecoverReport = true;
		//tfc.AutoRecoverReportForeign = true;

		tfc.LoginDirect(tfc.ServerHost, tfc.ServerPort, "account_id", "0000", ' ');
	}

	public void Logout()
	{
	}

	public void Order(byte BuySell, byte ContractId, ushort Amount, int Price)
	{
		string OMsg = String.Format("Order() {0},{1},{2},{3}",
					    BuySell == 0 ? "Buy" : "Sell",
					    ContractId == 0 ? "TX" : "MTX",
					    Amount,
					    Price);
		TradeApiLog(OMsg);
	}
}

public class ServiceBody
{
	private Socket ConnectSocket;
	private Thread ServeThread;
	private KgiTradeApi ApiRef;

	//CommandDate[0] : 0 buy, 1 sell
	//CommandData[1] : 0 TX, 1 MTX
	//CommandData[2] : order amount
	//CommandData[3~6] : price, little endian, 0xffffffff(a.k.a -1) is market order
	private byte ExecCommandStr(byte[] CommandData)
	{
		ushort Amount = (ushort) CommandData[2];
		int Price = BitConverter.ToInt32(CommandData, 3);

		//Console.WriteLine("1:" + CommandData[0].ToString());
		//Console.WriteLine("2:" + CommandData[1].ToString());
		//Console.WriteLine("3: {0:X}", amount);
		//Console.WriteLine("4: {0:X}", price);
		ApiRef.Order(CommandData[0], CommandData[1], Amount, Price);
		return 0;
	}

	private void ThreadProc()
	{
		byte[] RecvBuffer = new byte[7];
		byte[] Ret = new byte[1];
		int Len;

		while(true)
		{
			Len = ConnectSocket.Receive(RecvBuffer);
			if (Len != 0)
			{
				Console.WriteLine(String.Format("*** receive length {0} bytes", Len));
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

	public ServiceBody(Socket x, KgiTradeApi y)
	{
		ConnectSocket = x;
		ApiRef = y;
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

		KgiTradeApi api = new KgiTradeApi();
		api.Create();
		api.Login();
		
		//Socket will bind on any IPv4 endpoint system have
		IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 44444);
		try {
			Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listener.Bind(localEndPoint);
			listener.Listen(10);

			while (true) {
				Socket client = listener.Accept();
				ServiceBody n = new ServiceBody(client, api);
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
