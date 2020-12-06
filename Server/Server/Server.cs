using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EgorServer
{
    class Start
    {
        static void Main(string[] args)
        {
            Console.Write("Введите порт сервера -> ");
            int serverPort;
            if (!int.TryParse(Console.ReadLine(), out serverPort)) { Console.Clear(); Main(args); return; }
            Server server = new Server();
            server.Start(serverPort);

            Console.Write("Started!\n");

        }
    }

    class Server
    {
        private Dictionary<string, Task<byte[]>> tasks = new Dictionary<string, Task<byte[]>>();

        private AutoResetEvent arevent = new AutoResetEvent(false);
        private int port;
        private IPEndPoint endPoint;
        private Socket socket;

        private List<Client> clients = new List<Client>();
        
        private void Initialize()
        {
            endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start(int Port)
        {
            port = Port;
            Initialize();

            socket.Bind(endPoint);
            socket.Listen(6);

            Thread t = new Thread(new ThreadStart(Listening));
            t.Start();
        }

        public void Listening()
        {
            int id = 0;
            while (true)
            {
                Socket handler = socket.Accept();
                Client client = new Client { Id = id++, socket = handler, are = new AutoResetEvent(false) };
                clients.Add(client);
                Console.WriteLine($"Client№ {client.Id} Подключился");
                new Thread(ReceiveAndAnswer).Start(client);

            }
        }

        private class Client {
            public int Id;
            public Socket socket;
            public AutoResetEvent are;

        }

        public void ReceiveAndAnswer(object state)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Client client = (Client)state;
            Socket socket = client.socket;

            SocketError err;

            Task<byte[]> currentTask = null;

            while (true)
            {
                try
                {
                    //Получение------------------   
                    byte[] data = new byte[256];
                   
                    socket.BeginReceive(data, 0, data.Length, SocketFlags.None, out err, ReceiveHandle, client);
                    client.are.WaitOne();

                    var receivedString = Encoding.Unicode.GetString(data, 0, data.Length).Trim('\0');

                    Console.WriteLine($"Client: {client.Id} -> " + receivedString + " :" + DateTime.Now.ToLongTimeString());
                    //-------------------------------------------------------------


                    //Таски
                    if (tasks.ContainsKey(receivedString))
                    {
                        currentTask = tasks[receivedString];
                    }
                    else
                    {
                        if (currentTask != null) cts.Cancel();
                        cts = new CancellationTokenSource();
                        currentTask = Task.Factory.StartNew(() => WORK((receivedString, client.Id), cts.Token), cts.Token);
                        tasks.Add(receivedString, currentTask);
                    }



                    currentTask.ContinueWith(t =>
                    {
                        try
                        {
                            var result = t.Result;
                            if (result != null) Send(socket, result);
                        }
                        catch
                        {
                            Console.WriteLine($"Client№ {client.Id} Офнул");
                        }

                    }, TaskContinuationOptions.NotOnCanceled);
                }
                catch
                {
                    Console.WriteLine($"Client№ {client.Id} Офнул");
                }

            }

        }
        

        private void ReceiveHandle(IAsyncResult ar)
        {
            (ar.AsyncState as Client).are.Set();
        }

        private byte[] WORK(object input, object token)
        {
            (string msg, int id) inpt = ((string msg, int id))input;
            try
            {
                var cancel = (CancellationToken)token;
                cancel.ThrowIfCancellationRequested();

                // готовим ответочку
                int parsed;
                var data = Encoding.Unicode.GetBytes(int.TryParse(inpt.msg, out parsed) ? (parsed * 5).ToString() : "не число!");
                cancel.ThrowIfCancellationRequested();

                // ждем . . .
                Thread.Sleep(6500);
                cancel.ThrowIfCancellationRequested();

                // Завершаем
                tasks.Remove(inpt.msg);

                return data;
            }
            catch(OperationCanceledException ex)
            {                
                tasks.Remove(inpt.msg);

                Console.WriteLine($"Client: {inpt.id} -> " + ex.Message);
                return null; 
            }
        }
      
        private void Send(object _socket, object _data)
        {

            var socket = (Socket)_socket;
            var data = (byte[])_data;

            // отправляем ответочку
            socket.Send(data);
        }
    }
}
