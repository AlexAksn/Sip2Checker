using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sip2Form
{
    internal class Sip2Connection : IDisposable
    {
        private readonly string _hostname;
        private readonly int _port;
        private NetworkStream _ns;
        private TcpClient _sipClient;


        public Sip2Connection(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
        }


        public void Dispose()
        {
            _sipClient?.Close();
            _ns?.Close();
        }


        public void Open()
        {
            _sipClient = new TcpClient { ReceiveTimeout = 10 * 1000, NoDelay = true };
            if (!_sipClient.ConnectAsync(_hostname, _port).Wait(TimeSpan.FromSeconds(60)))
                throw new TimeoutException();
            _ns = _sipClient.GetStream();
        }


        public string SendMessage(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            _ns.Write(buffer, 0, buffer.Length);
            // read response
            var response = new byte[512];
            var responseData = new StringBuilder();
            var duration = TimeSpan.FromSeconds(5);
            var sw = Stopwatch.StartNew();
            do
            {
                var bytes = _ns.Read(response, 0, response.Length);
                responseData.Append(Encoding.UTF8.GetString(response, 0, bytes));
            } while (sw.Elapsed < duration && _ns.CanRead &&
                        !(responseData.ToString().EndsWith("\r") || responseData.ToString().EndsWith("\n")));

            var result = responseData.ToString();
            return result;
        }
    }
}
