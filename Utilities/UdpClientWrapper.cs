using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SharpBridge.Interfaces;

namespace SharpBridge.Utilities
{
    public class UdpClientWrapper : IUdpClientWrapper
    {
        private readonly UdpClient _client;

        public UdpClientWrapper(UdpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            
            // Set read timeout to match Rust implementation's 2-second timeout
            _client.Client.ReceiveTimeout = 2000;
        }

        public int Available => _client.Available;

        public void Dispose()
        {
            _client.Dispose();
        }

        public bool Poll(int microseconds, SelectMode mode)
        {
            return _client.Client.Poll(microseconds, mode);
        }

        public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken token)
        {
            return await _client.ReceiveAsync(token);
        }

        public Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port)
        {
            return _client.SendAsync(datagram, bytes, hostname, port);
        }
    }
} 