using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerAndroidConnection
{
    public class ServerUDP
    {
        static int width = 1920;
        static int height = 1080;
        static int qualityValue = 15;

        bool live = false;
        string errorString = "Нет ошибок";
        int errorCode = 0;

        public TextBox textBoxState;

        UdpClient udpClient;

        IPEndPoint LocalIp;
        IPEndPoint RemotePoint;

        static Bitmap BackGround = new Bitmap(width, height);
        Graphics graphics = Graphics.FromImage(BackGround);

        Form1 mainForm;

        public ServerUDP(string hostname, int port, Form1 _mainForm)
        {
            mainForm = _mainForm;
            IPAddress LocalAddr = IPAddress.Parse(hostname);
            LocalIp = new IPEndPoint(LocalAddr, port);

            udpClient = new UdpClient();

            //var ip = mainForm?ClientList?.First()?.Ip?.ToString() ?? "0.0.0.0";

            //RemotePoint = new IPEndPoint(IPAddress.Parse(ip), port);


            //RemotePoint = new IPEndPoint(IPAddress.Parse("192.168.0.12"), 8888); 
            //RemotePoint = new IPEndPoint(IPAddress.Parse("169.254.219.139"), 8888); // Serega
            //RemotePoint = new IPEndPoint(IPAddress.Parse("100.124.160.151"), 8888); // Ne Serega
        }

        /// <summary>
        /// Отправить картинку UDP
        /// </summary>
        public async void SendAsyncUDP()
        {
            int a = 0;

            //if (udpClient == null)
            //{
            //    WriteInLog("Клиент не доступен!");
            //    return;
            //}
            using var udpClient = new UdpClient();

            // отправляемые данные
            string message = "Hello METANIT.COM";
            // преобразуем в массив байтов
            byte[] data = Encoding.UTF8.GetBytes(message);
            // определяем конечную точку для отправки данных
            IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse("100.124.160.151"), 8888);
            // отправляем данные
            int bytes = await udpClient.SendAsync(data, remotePoint);
            WriteInLog($"Отправлено {bytes} байт");
        }

        /// <summary>
        /// Отправить снимок экрана
        /// </summary>
        public async void SendScreenAsyncUDP()
        {
            // Получаем снимок экрана
            graphics.CopyFromScreen(0, 0, 0, 0, BackGround.Size);

            // Получаем размеры окна рабочего стола
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

                // Сжимаем
                Bitmap CompressedImage = new Bitmap(CompressBitmap(bitmap, qualityValue));

                // Конвертируем картинку в массив байтов
                var data = ImageToByteArray(CompressedImage);
                List<byte[]> lst = CutMsg(data);
                for (int i = 0; i < lst.Count; i++)
                {
                    // Отправляем картинку клиенту
                    int bytes = await udpClient.SendAsync(lst[i], lst[i].Length, RemotePoint);
                }
            }
        }

        /// <summary>
        /// Принять сообщение (заглушка)
        /// </summary>
        /// <param name="socket"></param>
        private async void ReceiveUDPAsync(Socket socket)
        {
            using var udpServer = new UdpClient(5555);
            Console.WriteLine("UDP-сервер запущен...");

            // получаем данные
            var result = await udpServer.ReceiveAsync();
            // предположим, что отправлена строка, преобразуем байты в строку
            var message = Encoding.UTF8.GetString(result.Buffer);

            Console.WriteLine($"Получено {result.Buffer.Length} байт");
            Console.WriteLine($"Удаленный адрес: {result.RemoteEndPoint}");
            Console.WriteLine(message);
        }

        #region Service Methods

        /// <summary>
        /// Запись в текстбокс лога изнутри 
        /// </summary>
        /// <param name="message"> Сообщение </param>
        private void WriteInLog(string message)
        {
            if (textBoxState != null)
                textBoxState.Text += "\r\n" + message;
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

        private List<byte[]> CutMsg(byte[] bt)
        {
            int Lenght = bt.Length;
            byte[] temp;
            List<byte[]> msg = new List<byte[]>();

            using (var memoryStream = new MemoryStream())
            {
                // Записываем в первые 2 байта количество пакетов
                memoryStream.Write(BitConverter.GetBytes((short)((Lenght / 65500) + 1)), 0, 2);

                // Далее записываем первый пакет
                memoryStream.Write(bt, 0, bt.Length);

                memoryStream.Position = 0;
                // Пока все пакеты не разделили - делим КЭП
                while (Lenght > 0)
                {
                    temp = new byte[65500];
                    memoryStream.Read(temp, 0, 65500);
                    msg.Add(temp);
                    Lenght -= 65500;
                }
                return msg;
            }
        }


        public Image CompressBitmap(Bitmap bitmap, int qualityValue)
        {
            // Создаем объект EncoderParameter для установки параметров сжатия
            EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualityValue);

            // Создаем объект Encoder для установки формата сжатия
            ImageCodecInfo jpegCodec = GetEncoderInfo(ImageFormat.Jpeg);
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            // Создаем MemoryStream для сохранения сжатого изображения
            using (MemoryStream ms = new MemoryStream())
            {
                // Сжимаем Bitmap-изображение и записываем его в MemoryStream
                bitmap.Save(ms, jpegCodec, encoderParams);

                // Возвращаем сжатое изображение в виде массива байт
                return Image.FromStream(ms);
            }
        }

        // Метод для получения объекта ImageCodecInfo для указанного формата изображения
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            return codecs[1];
        }

        /// <summary>
        /// Конвертация изображения в массив битов
        /// </summary>
        /// <param name="imageIn"> Изображение </param>
        /// <returns></returns>
        static byte[] ImageToByteArray(Image imageIn)
        {
            // 5 строчек магии (мутим параметры для сохранения потока памяти)
            var myEncoder = System.Drawing.Imaging.Encoder.Quality;
            var myEncoderParameters = new EncoderParameters(1);
            var myEncoderParameter = new EncoderParameter(myEncoder, 15L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            var myImageCodecInfo = GetEncoderInfo("image/jpeg");

            // Создаем объект EncoderParameter для установки параметров сжатия
            //EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 15L);

            // Создаем объект Encoder для установки формата сжатия
            ImageCodecInfo jpegCodec = GetEncoderInfo(ImageFormat.Jpeg);
            EncoderParameters encoderParams = new EncoderParameters(1);
            //encoderParams.Param[0] = qualityParam;

            using (var ms = new MemoryStream())
            {
                //ImageFormat format = ImageFormat.Png;
                //switch (imageToConvert.MimeType())
                //{
                //    case "image/png":
                //        format = ImageFormat.Png;
                //        break;
                //    case "image/gif":
                //        format = ImageFormat.Gif;
                //        break;
                //    default:
                //        format = ImageFormat.Jpeg;
                //        break;
                //}

                //imageIn.Save(ms, jpegCodec, encoderParams);
                //imageIn.Save(ms, format);
                // Сохраняем поток памяти
                imageIn.Save(ms, myImageCodecInfo, myEncoderParameters);
                return ms.ToArray();
            }
        }

        private byte[] ConvertToByte(Bitmap bmp)
        {
            MemoryStream memoryStream = new MemoryStream();
            // Конвертируем в массив байтов с сжатием Jpeg
            bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
            return memoryStream.ToArray();
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
