using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace EgorClient
{
    class Start
    {
        static void Main(string[] args)
        {
            Console.Write("Введите ip сервера -> ");
            IPAddress ip;
            if (!IPAddress.TryParse(Console.ReadLine(), out ip)) { Console.Clear(); Main(args); return; }

            Console.Write("Введите порт сервера -> ");
            int serverPort;
            if (!int.TryParse(Console.ReadLine(), out serverPort)) { Console.Clear(); Main(args); return; }

            try
            {
                Client client = new Client();
                client.Connect(ip, serverPort);      
                client.Start();

                Console.Write("Connected!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }


        }
    }
    class Client
    {
        private AutoResetEvent arevent = new AutoResetEvent(false);
        private IPEndPoint endPoint;
        private Socket socket;
        public string[] Strings = new string[] { "123", "321", "121" };

        public void Connect(IPAddress ip, int port)
        {
            endPoint = new IPEndPoint(ip, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);
        }

        public void Start()
        {

            //Отправка
            new Thread(new ThreadStart(Send)).Start();

            //Прием
            new Thread(new ThreadStart(Receive)).Start();

        }

        public void Send()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = Encoding.Unicode.GetBytes(Strings[new Random().Next(Strings.Length)]);

                    socket.Send(buffer);

                    // задержка между запросами
                    Thread.Sleep(new Random().Next(2700, 10000));
                }
                catch
                {
                    Console.WriteLine("ААА щщит сервер упал!");
                    break;
                }
            }
        }

        public void Receive()
        {
            while (true)
            {
                try
                {
                    SocketError err;
                    //Получение
                    var data = new byte[256]; // буфер для ответа
                    socket.BeginReceive(data, 0, data.Length, SocketFlags.None, out err, ReceiveHandle, null);
                    arevent.WaitOne();
                    var receivedString = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0');

                    Console.WriteLine("Ответ сервера -> " + receivedString.ToString());
                }
                catch
                {
                    Console.WriteLine("ААА щщит сервер упал!");
                    break;
                }
            }
        }

        private void ReceiveHandle(IAsyncResult ar)
        {
            arevent.Set();
        }

       


    }
}
