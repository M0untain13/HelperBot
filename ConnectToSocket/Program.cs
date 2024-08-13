using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ConnectToSocket;

public class Program
{
	static void Main(string[] args)
	{
		if(args.Length < 1)
		{
			Console.WriteLine("Необходим аргумент: порт");
			return;
		}

		var port = Convert.ToInt32(args[0]);
		try
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			var address = host.AddressList[0];
			var endPoint = new IPEndPoint(address, port);
			var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			try
			{
                socket.Connect(endPoint);

				Console.WriteLine("Socket connected to -> {0} ", socket.RemoteEndPoint?.ToString());
				Console.WriteLine("Введите через пробелы логин, имя и фамилию.");

				var input = Console.ReadLine();
				if(input?.Replace("<EOF>", "").Split().Length != 3)
				{
					throw new Exception("Ошибка! Получено не три слова.");
				}

                byte[] messageSent = Encoding.ASCII.GetBytes(input + "<EOF>");
				int byteSent = socket.Send(messageSent);
				byte[] messageReceived = new byte[1024];
				int byteRecv = socket.Receive(messageReceived);
				Console.WriteLine("Message from Server -> {0}", Encoding.ASCII.GetString(messageReceived, 0, byteRecv));

				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}

			catch (ArgumentNullException ane)
			{

				Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
			}

			catch (SocketException se)
			{

				Console.WriteLine("SocketException : {0}", se.ToString());
			}

			catch (Exception e)
			{
				Console.WriteLine("Unexpected exception : {0}", e.ToString());
			}
		}

		catch (Exception e)
		{

			Console.WriteLine(e.ToString());
		}
	}
}
