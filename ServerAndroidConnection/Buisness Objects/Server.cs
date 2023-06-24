using ServerAndroidConnection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace ServerAndroidConnection
{
    public class Server
    {
        static int width = 1920;
        static int height = 1080;

        bool live = false;
        string errorString = "Нет ошибок";
        int errorCode = 0;

        IPEndPoint LocalIp;
        
        // Список подключенных клиентов
        public List<Client> ClientList = new List<Client>();

        static Bitmap BackGround = new Bitmap(width, height);
        Graphics graphics = Graphics.FromImage(BackGround);

        Form1 mainForm;

        byte[] data;

        public Server(string hostname, int port, Form1 _mainForm)
        {
            mainForm = _mainForm;
            IPAddress LocalAddr = IPAddress.Parse(hostname);
            LocalIp = new IPEndPoint(LocalAddr, port);

            ClientList = new List<Client>();

            TcpListener server = new TcpListener(LocalIp);
            server.Start();  // Запускаем сервер
            WriteInLog("Сервер запущен");
            live = true;

            Listening(server);  // Ждем подключений
        }

        /// <summary>
        /// Ожидание подключений
        /// </summary>
        /// <param name="server"></param>
        private async void Listening(TcpListener server)
        {
            while (live)
            {
                WriteInLog("Ожидание подключений...");

                // получаем входящее подключение
                Client client = new Client();
                                 
                client.tcpSocket = await server.Server.AcceptAsync();

                if (client.tcpSocket != null && client.tcpSocket.RemoteEndPoint != null)
                    client.Ip = (IPEndPoint)client.tcpSocket.RemoteEndPoint;

                // Определить ping
                var pingResult = CheckPingSync(client);
                client.ping = pingResult.RoundtripTime;

                // Фиксируем адрес клиента
                mainForm.clientsListBox.Items.Add(client.Ip.Address);

                ClientList.Add(client);
                UpdateClientList();
                ReceiveTCPAsync(client);

                WriteInLog($"Адрес подключенного клиента: {client.Ip.Address}:{client.Ip.Port}");
            }
            // Получить публичный ключ клиента
            // Отправить свой публичный ключ
        }

        /// <summary>
        /// Отправить сообщение
        /// </summary>
        public async void SendAsyncTCP(Client client, string message)
        {
            if (client.tcpSocket == null)
            {
                WriteInLog("Клиент не доступен!");
                return;
            }

            var data1 = Encoding.ASCII.GetBytes(message);
            await client.tcpSocket.SendAsync(data1, SocketFlags.None);
            
            WriteInLog("Сообщение отправлено");
        }
        
        /// <summary>
        /// Отправить сообщение sync
        /// </summary>
        public void SendSyncTCP(Client client, string message)
        {
            if (client.tcpSocket == null)
            {
                WriteInLog("Клиент не доступен!");
                return;
            }

            var data1 = Encoding.ASCII.GetBytes(message);
            client.tcpSocket.Send(data1, SocketFlags.None);
            
            WriteInLog("Сообщение отправлено");
        }


        /// <summary>
        /// Отправить снимок экрана
        /// </summary>
        public async void SendScreenAsyncTCP(Client client)
        {
            // Проверяем, есть ли клиент, чтобы все к херам не развалилось
            if (client.tcpSocket == null)
            {
                WriteInLog("Клиент не доступен!");
                return;
            }
            // Получаем снимок экрана
            graphics.CopyFromScreen(0, 0, 0, 0, BackGround.Size);

            Rectangle bounds = Screen.GetBounds(Point.Empty);
            // Создаем пустое изображения размером с экран устройства
            using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                // Создаем объект на котором можно рисовать
                using (var g = Graphics.FromImage(bitmap))
                {
                    // Перерисовываем экран на наш графический объект
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                // Конвертируем картинку в массив байтов
                var data = ImageToByteArray(bitmap);

                // Асинхронная отправка
                await client.tcpSocket.SendAsync(data, SocketFlags.None);
            }
            WriteInLog("Сообщение отправлено");
        }

        /// <summary>
        /// Принять сообщение
        /// </summary>
        /// <param name="socket"></param>
        public async void ReceiveTCPAsync(Client client)
        {
            if (client.tcpSocket == null)
            {
                //WriteInLog("Клиент не пдключен!");
                return;
            }
            data = new byte[50];

            // получаем данные из потока
            await client.tcpSocket.ReceiveAsync(data, SocketFlags.None);
            if (data.Any())
            {
                WriteInLog(Encoding.Default.GetString(data));
            }
        }

        public PingReply CheckPingSync(Client client)
        {
            if (client.tcpSocket == null || client.Ip == null)
            {
                //WriteInLog("Клиент не пдключен!");
                return null;
            }

            Ping ping = new Ping();
            PingReply reply = null;

            try
            {
                reply = ping.Send(client.Ip.Address, 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: {0}", ex.Message);
            }

            return reply;
        }
        
        public void UpdateClientList()
        {
            if (ClientList.Any())
            {
                mainForm.clientsListBox.Items.Clear();
                foreach (var clientItem in ClientList)
                {
                    if (clientItem == null || clientItem.tcpSocket == null || clientItem.Ip == null)
                    {
                        continue;
                    }

                    var result = CheckPingSync(clientItem);
                    clientItem.ping = result.RoundtripTime;

                    // Фиксируем адрес клиента
                    mainForm.clientsListBox.Items.Add(clientItem.Ip.Address + " | " + clientItem.ping + " | " + clientItem.fps + " | ");
                }
            }
        }

        #region Service Methods

        /// <summary>
        /// Запись в текстбокс лога изнутри 
        /// </summary>
        /// <param name="message"> Сообщение </param>
        private void WriteInLog(string message)
        {
            if (mainForm != null && mainForm.messageTextBox != null)
                mainForm.messageTextBox.Text += "\r\n" + message;
        }

        /// <summary>
        /// Запись в текстбокс лога извне
        /// </summary>
        /// <param name="message"> Сообщение </param>
        /// <param name="_textBoxState"> Целевой текстбокс </param>
        public static void WriteInLog(string message, TextBox _textBoxState)
        {
            if (_textBoxState != null)
                _textBoxState.Text += "\r\n" + message;
        }

        /// <summary>
        /// Конвертация изображения в массив битов
        /// </summary>
        /// <param name="imageIn"> Изображение </param>
        /// <returns></returns>
        static byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                // 5 строчек магии (мутим параметры для сохранения потока памяти)
                var myEncoder = System.Drawing.Imaging.Encoder.Quality;
                var myEncoderParameters = new EncoderParameters(1);
                var myEncoderParameter = new EncoderParameter(myEncoder, 50L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                var myImageCodecInfo = GetEncoderInfo("image/jpeg");

                // Сохраняем поток памяти
                imageIn.Save(ms, myImageCodecInfo, myEncoderParameters);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Получить encoder
        /// Вспомогательный метод для конвертации
        /// </summary>
        /// <param name="mimeType"> Какое-то говно </param>
        /// <returns> Кодек инфо </returns>
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        #endregion Service Methods

        public enum ProtocolType
        {
            TCP = 1,
            UDP = 2,
        }
    }
}
