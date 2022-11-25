using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 4)
        {
            System.Console.WriteLine("Usage: tcp2udp <Bind Adress> <TCP Listen Port> <UDP Destination> <UDP Destination Port> <Max Connection>");
            return;
        }
        try
        {
            string bindAdress = args[0];
            int listenPort = int.Parse(args[1]);
            string destination = args[2];
            int destPort = int.Parse(args[3]);
            int maxConnection = int.Parse(args[4]);

            tcp2udp.bindAdress = bindAdress;
            tcp2udp.bindPort = listenPort;
            tcp2udp.destination = destination;
            tcp2udp.destinationPort = destPort;
            tcp2udp.maxConnection = maxConnection;

            tcp2udp.StartListener();
        }
        catch
        {
            System.Console.WriteLine("Usage: tcp2udp <Bind Adress> <TCP Listen Port> <UDP Destination> <UDP Destination Port> <Max Connection>");
            return;
        }
    }


}

class tcp2udp
{
    static Socket tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public static int maxConnection = 0;
    public static int currentConnection = 0;
    public static string bindAdress = "";
    public static int bindPort = 0;
    public static string destination = "";
    public static int destinationPort = 0;
    public static void StartListener()
    {

        tcpListener.Bind(new IPEndPoint(IPAddress.Parse(bindAdress), bindPort));
        tcpListener.Listen();

        while (true)
        {
            Socket tcpConnection = tcpListener.Accept();
            if (currentConnection < maxConnection)
            {
                //connection accepted
                currentConnection++;
                Connection connection = new Connection();
                Thread thread = new Thread(new ParameterizedThreadStart(connection.Start));
                thread.Start((object)tcpConnection);
            }
            else
            {
                //connection refused
                tcpConnection.Disconnect(false);
                tcpConnection.Dispose();
            }
        }
    }
}
class Connection
{
    public Socket tcpConnection, udpConnection;
    public byte[] tcpBuffer = new byte[4096], udpBuffer = new byte[4096];
    public bool isActive = true;
    public void Destroy()
    {
        if (isActive == true)
        {
            isActive = false;
            tcp2udp.currentConnection--;
        }
    }
    public void Start(object obj)
    {
        tcpConnection = (Socket)obj;
        udpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            udpConnection.Connect(tcp2udp.destination, tcp2udp.destinationPort);
            tcpConnection.BeginReceive(tcpBuffer, 0, tcpBuffer.Length, SocketFlags.None, TcpReceive, null);
            udpConnection.BeginReceive(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, UdpReceive, null);
        }
        catch
        {
            tcpConnection.Close();
            udpConnection.Close();
            Destroy();
        }

    }

    void TcpReceive(IAsyncResult result)
    {
        try
        {
            int len = tcpConnection.EndReceive(result);
            if (len <= 0)
            {
                //disconnect
                udpConnection.Close();
                tcpConnection.Close();
                Destroy();
                return;
            }
            byte[] data = new byte[len];
            Array.Copy(tcpBuffer, data, len);

            udpConnection.BeginSend(data, 0, data.Length, SocketFlags.None, null, null);

            tcpConnection.BeginReceive(tcpBuffer, 0, tcpBuffer.Length, SocketFlags.None, tcpReceive, null);
        }
        catch
        {
            udpConnection.Close();
            tcpConnection.Close();
            Destroy();
            return;
        }
    }
    void UdpReceive(IAsyncResult result)
    {
        try
        {
            int len = udpConnection.EndReceive(result);

            if (len <= 0)
            {
                //disconnect
                udpConnection.Close();
                tcpConnection.Close();
                Destroy();
                return;
            }
            byte[] data = new byte[len];
            Array.Copy(udpBuffer, data, len);

            tcpConnection.BeginSend(data, 0, data.Length, SocketFlags.None, null, null);

            udpConnection.BeginReceive(udpBuffer, 0, udpBuffer.Length, SocketFlags.None, udpReceive, null);
        }
        catch
        {
            //disconnect
            udpConnection.Close();
            tcpConnection.Close();
            Destroy();
            return;
        }
    }
}