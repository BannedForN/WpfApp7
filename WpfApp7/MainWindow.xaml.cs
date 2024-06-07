using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp7
{
    public partial class MainWindow : Window
    {
        private Socket _socket;
        private List<Socket> _clients = new List<Socket>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isClosing = false;

        public MainWindow()
        {
            InitializeComponent();

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(ipPoint);
            _socket.Listen(1000);

            Task.Run(() => ListenToClients(_cancellationTokenSource.Token));
        }

        private async Task ListenToClients(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _socket.AcceptAsync();
                _clients.Add(client);
                ReceiveMessage(client);
            }
        }

        private async Task ReceiveMessage(Socket client)
        {
            while (!_isClosing)
            {
                byte[] bytes = new byte[1024];
                await client.ReceiveAsync(bytes, SocketFlags.None);
                string message = Encoding.UTF8.GetString(bytes);

                Dispatcher.Invoke(() =>
                {
                    MessageLbx.Items.Add($"[{DateTime.Now.ToString()}][Message from {client.RemoteEndPoint}]: {message}");
                });

                foreach (var item in _clients)
                {
                    if (item != client)
                    {
                        SendMessage(item, message);
                    }
                }
            }
        }

        private async Task SendMessage(Socket client, string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await client.SendAsync(bytes, SocketFlags.None);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _isClosing = true;

            foreach (var client in _clients)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }

            _cancellationTokenSource.Cancel();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _clients)
            {
                SendMessage(item, TextTbx.Text);
            }
            TextTbx.Text = string.Empty;
        }
    }
}
