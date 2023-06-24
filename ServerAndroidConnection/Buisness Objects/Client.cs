using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ServerAndroidConnection
{
    public class Client
    {
        public Socket tcpSocket;
        public Socket udpSocket;

        public IPEndPoint Ip;

        public long ping;
        public int fps;
        public int qualityValue;

        public System.Threading.Timer timer;
        public Thread thread;        
    }
}
