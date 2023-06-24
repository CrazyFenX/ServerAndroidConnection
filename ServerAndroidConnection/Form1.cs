namespace ServerAndroidConnection
{
    public partial class Form1 : Form
    {
        Server tcpServer;
        public Form1()
        {
            InitializeComponent();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            var IP = ipTextBox.Text;
            var Port = Convert.ToInt32(portTextBox.Text);
            try
            {
                tcpServer = new Server(IP, Port, this);
                Server.WriteInLog("TCP Server is ON", messageTextBox);
                //pingTimer.Start();
            }
            catch (Exception ex)
            {
                Server.WriteInLog(ex.Message, messageTextBox);
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            var text = sendMessageTextBox.Text;
            var multicasting = multicastCheckBox.Checked;

            if (multicasting)
                foreach (var client in tcpServer.ClientList)
                    //tcpServer.SendAsyncTCP(client, text);
                    tcpServer.SendSyncTCP(client, text);
        }
    }
}