using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StatsdClient
{
    public class StatsdUDP : IDisposable, IStatsdUDP
    {
        private int MaxUDPPacketSize { get; set; } // In bytes; default is MetricsConfig.DefaultStatsdMaxUDPPacketSize.
        // Set to zero for no limit.
        public IPEndPoint IPEndpoint { get; private set; }
        private Socket UDPSocket { get; set; }
        private string Name { get; set; }
        private int Port { get; set; }

        public StatsdUDP(string name, int port, int maxUDPPacketSize = StatsdConfig.DefaultStatsdMaxUDPPacketSize)
        {
            Name = name;
            Port = port;
            MaxUDPPacketSize = maxUDPPacketSize;

            UDPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var ipAddress = GetIpv4Address(name);

            IPEndpoint = new IPEndPoint(ipAddress, Port);
        }

        private IPAddress GetIpv4Address(string name)
        {
            IPAddress ipAddress;
            bool isValidIPAddress = IPAddress.TryParse(name, out ipAddress);

            if (!isValidIPAddress)
            {
                ipAddress = null;
#if NET451
                IPAddress[] addressList = Dns.GetHostEntry(name).AddressList;
#else
                IPAddress[] addressList = Dns.GetHostEntryAsync(name).Result.AddressList;
#endif
                //The IPv4 address is usually the last one, but not always
                for(int positionToTest = addressList.Length - 1; positionToTest >= 0; --positionToTest)
                {
                    if(addressList[positionToTest].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = addressList[positionToTest];
                        break;
                    }
                }

                //If no IPV4 address is found, throw an exception here, rather than letting it get squashed when encountered at sendtime
                if(ipAddress == null)
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
            }
            return ipAddress;
        }

        public void Send(string command)
        {
            SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(command))).Wait();
        }

        public Task SendAsync(string command)
        {
            return SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(command)));
        }

        private async Task SendAsync(ArraySegment<byte> encodedCommand)
        {
            if (MaxUDPPacketSize > 0 && encodedCommand.Count > MaxUDPPacketSize)
            {
                // If the command is too big to send, linear search backwards from the maximum
                // packet size (taking into account the offset in the array)
                // to see if we can find a newline delimiting two stats. If we can,
                // split the message across the newline and try sending both componenets individually
                byte newline = Encoding.UTF8.GetBytes("\n")[0];
                for (int i = MaxUDPPacketSize + encodedCommand.Offset; i > encodedCommand.Offset; i--)
                {
                    if (encodedCommand.Array[i] == newline)
                    {
                        var encodedCommandFirst = new ArraySegment<byte>(encodedCommand.Array, encodedCommand.Offset, i);

                        await SendAsync(encodedCommandFirst).ConfigureAwait(false);

                        int remainingCharacters = encodedCommand.Count - i - 1;
                        if (remainingCharacters > 0)
                        {
                            await SendAsync(new ArraySegment<byte>(encodedCommand.Array, i + 1, remainingCharacters)).ConfigureAwait(false);
                        }

                        return; // We're done here if we were able to split the message.
                    }
                    // At this point we found an oversized message but we weren't able to find a
                    // newline to split upon. We'll still send it to the UDP socket, which upon sending an oversized message
                    // will fail silently if the user is running in release mode or report a SocketException if the user is
                    // running in debug mode.
                    // Since we're conservative with our MAX_UDP_PACKET_SIZE, the oversized message might even
                    // be sent without issue.
                }
            }
            await UDPSocket.SendToAsync(encodedCommand, SocketFlags.None, IPEndpoint).ConfigureAwait(false);
        }

        public void Dispose()
        {
            UDPSocket.Dispose();
        }
    }
}
