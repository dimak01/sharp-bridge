using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBridge.Interfaces
{
    public interface IUdpClientWrapper : IDisposable
    {
        Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port);
        Task<UdpReceiveResult> ReceiveAsync(CancellationToken token);
        int Available { get; }
        bool Poll(int microseconds, SelectMode mode);
    }
} 