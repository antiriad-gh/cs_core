using System.Net;
using System.Net.Sockets;
using Antiriad.Core.Collections;
using Antiriad.Core.Log;
using Antiriad.Core.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Antiriad.Core.IO;

/// <summary>
/// Socket implementation for Connector class
/// </summary>
public class SocketConnector : Connector
{
    private Socket? socket;
    private SocketType socketType = SocketType.Stream;
    private readonly Dictionary<SocketOptionName, int> options = new();
    private string? bindToAddress;
    private readonly bool asyncIO;
    private byte[]? packetBuffer;

    public SocketEndPoint? Address { get; private set; }

    public SocketType SocketType
    {
        get { return this.socketType; }
        set
        {
            if (this.socket == null)
                this.socketType = value;
        }
    }

    public string RemoteHost
    {
        get
        {
            return this.Address?.Host ?? string.Empty;
        }
    }

    public int RemotePort
    {
        get
        {
            return this.Address?.Port ?? 0;
        }
    }

    public Socket? Socket { get { return this.socket; } }

    public string LocalAddress
    {
        get
        {
            var client = this.socket;
            return client != null && (client.LocalEndPoint is IPEndPoint ep) ? ep.Address.ToString() : string.Empty;
        }
    }

    public string RemoteAddress
    {
        get
        {
            var client = this.socket;
            return client != null && client.Connected ? ((IPEndPoint)client.RemoteEndPoint!).Address.ToString() : string.Empty;
        }
    }

    public string LocalHost
    {
        get
        {
            var client = this.socket;
            return client != null && (client.LocalEndPoint is IPEndPoint ep) ? ep.Address.ToString() : string.Empty;
        }
    }

    public int LocalPort
    {
        get
        {
            var client = this.socket;
            return client != null && (client.LocalEndPoint is IPEndPoint ep) ? ep.Port : 0;
        }
    }

    public override string ToString()
    {
        return this.Address != null ? this.Address.ToString() : "notconnected";
    }

    public override bool IsConnected
    {
        get
        {
            var client = this.socket;
            return client != null && (this.socketType == SocketType.Dgram || client.Connected);
        }
    }

    public uint KeepAliveInterval { get; set; }

    public void Bind(string address, SocketType socketType = SocketType.Unknown)
    {
        this.bindToAddress = address;

        if (socketType != SocketType.Unknown)
            this.SocketType = socketType;

        if (this.socket != null)
            this.CheckBind();
    }

    public bool Shutdown()
    {
        return this.Shutdown(true, true);
    }

    public bool Shutdown(bool send, bool receive)
    {
        var client = this.socket;
        var isok = false;

        if ((send || receive) && client != null && client.Connected)
        {
            try
            {
                client.Shutdown(send && receive ? SocketShutdown.Both : send ? SocketShutdown.Send : SocketShutdown.Receive);
                isok = true;
            }
            catch (System.Exception ex)
            {
                Trace.Exception(ex);
            }
        }

        return isok;
    }

    public void SetOption(SocketOptionName name, object value)
    {
        this.options[name] = (int)(value is bool boolean ? boolean ? 1 : 0 : value);
    }

    public SocketConnector(bool asyncIO = false)
    {
        this.asyncIO = asyncIO;
    }

    public SocketConnector(SocketType type, string? address = null, bool asyncIO = false) : this(address)
    {
        this.SocketType = type;
        this.asyncIO = asyncIO;
    }

    /// <summary>
    /// Creates an instances owning an internal Socket
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="readTimeout"></param>
    public SocketConnector(string host, int port, int readTimeout, bool asyncIO = false)
    {
        this.Address = new SocketEndPoint(host, port);
        this.ReadTimeout = readTimeout;
        this.asyncIO = asyncIO;
    }

    /// <summary>
    /// Creates an instances with a given Socket
    /// </summary>
    /// <param name="client"></param>
    public SocketConnector(Socket client, bool asyncIO = false)
    {
        this.OwnDevice = false;
        this.socket = client;
        this.asyncIO = asyncIO;

        try
        {
            if (this.socket == null)
                return;

            this.socketType = client.SocketType;

            if (this.socketType == SocketType.Stream)
                this.socket.NoDelay = true;

            if (!this.socket.Connected)
                return;

            this.Address = new SocketEndPoint(this.socket.RemoteEndPoint as IPEndPoint);
        }
        catch (Exception ex)
        {
            Trace.Exception(ex);
        }
    }

    public SocketConnector(string? address, bool asyncIO = false)
    {
        if (!string.IsNullOrEmpty(address))
            this.Address = SocketEndPoint.ParseAddress(address);

        this.asyncIO = asyncIO;
    }

    public override bool Activate(object? listener = null)
    {
        if (this.OwnDevice)
            this.Deactivate();

        return base.Activate(listener);
    }

    protected override bool CreateReadThread()
    {
        var isok = this.asyncIO ? this.DoSetupAsyncIO() : base.CreateReadThread();

        if (!isok && this.Reconnect && this.asyncIO)
        {
            ThreadRunner.Spawn(this.DoAsyncIOReconnect);
            return true;
        }

        return isok;
    }

    private void DoAsyncIOReconnect(object obj)
    {
        while (!this.stop && !this.IsConnected)
        {
            Thread.Sleep(this.ReadTimeout);
            this.DoSetupAsyncIO();
        }
    }

    private void CreateSocket()
    {
        if (this.socket == null)
        {
            var prot = this.socketType == SocketType.Stream ? ProtocolType.Tcp : ProtocolType.Udp;
            this.socket = new Socket(AddressFamily.InterNetwork, this.socketType, prot);

            this.options.ForEach(i => this.socket.SetSocketOption(SocketOptionLevel.Socket, i.Key, i.Value));

            if (this.socketType == SocketType.Stream)
            {
                this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                this.socket.NoDelay = true;

                if (this.KeepAliveInterval > 0)
                {
                    this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    this.socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, this.KeepAliveInterval);
                    this.socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, this.KeepAliveInterval);
                    this.socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, this.KeepAliveInterval);
                }
            }
            else
                this.socket.DontFragment = true;
        }

        this.CheckBind();
        this.CheckBufferSize();

        if (this.WriteTimeout > 0)
            this.socket.SendTimeout = this.WriteTimeout;

        if (this.ReadTimeout > 0)
            this.socket.ReceiveTimeout = this.ReadTimeout;
    }

    private bool DoSetupAsyncIO()
    {
        if (!this.Connect())
        {
            return false;
        }

        var socket = this.Socket;
        var isok = false;

        if (this.packetBuffer == null || this.packetBuffer.Length != this.ReceiveBufferSize)
            this.packetBuffer = new byte[this.ReceiveBufferSize];

        try
        {
            if (!this.stop && socket != null && socket.Connected)
                socket.BeginReceive(this.receiveBuffer, 0, this.receiveBuffer.Length, SocketFlags.None, this.DataReadEnd, socket);

            isok = true;
        }
        catch (Exception ex)
        {
            Trace.Exception(ex);
        }

        return isok;
    }

    private void CheckBind()
    {
        if (string.IsNullOrEmpty(this.bindToAddress))
            return;

        var address = SocketEndPoint.ParseAddress(this.bindToAddress);
        var ep = address.IsEmpty ? IPAddress.Any : IPAddress.Parse(address.Host!);

        if (address.Port > 0)
            this.socket?.Bind(new IPEndPoint(ep, address.Port));
    }

    protected override void CheckBufferSize()
    {
        var client = this.socket;

        if (client == null)
            return;

        if (this.ReceiveBufferSize > 0)
            client.ReceiveBufferSize = this.ReceiveBufferSize;

        if (this.sendBufferSize > 0)
            client.SendBufferSize = this.sendBufferSize;
    }

    protected override void DeviceConnect()
    {
        try
        {
            if (this.IsConnected)
            {
                return;
            }

            this.DoClose(this.socket);
            this.CreateSocket();

            if (this.socketType == SocketType.Dgram)
            {
                return;
            }

            if (this.Address?.Host == null)
            {
                throw new Exception("address not assigned");
            }

            var asc = this.socket?.BeginConnect(this.Address.Host, this.Address.Port, null, null);

            if (asc?.AsyncWaitHandle.WaitOne(this.ReadTimeout == 0 ? 10000 : this.ReadTimeout) ?? false)
            {
                try
                {
                    this.socket?.EndConnect(asc);
                    return;
                }
                catch (Exception ex)
                {
                    Trace.Error($"address={this.Address} error={ex.Message}");
                }
            }

            this.DoClose(this.socket);
        }
        catch (ObjectDisposedException)
        {
            this.socket = null;
        }
        catch (Exception ex)
        {
            Trace.Exception(ex);
        }
    }

    private void DoClose(Socket? client)
    {
        if (client == null)
            return;

        if (this.socketType == SocketType.Stream)
        {
            try
            { client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false); }
            catch (Exception) { }
        }

        try
        { client.Close(0); }
        catch (Exception) { }
        this.socket = null;
    }

    protected override void DeviceDisconnect()
    {
        var client = this.socket;

        if (client != null && (client.Connected || this.socketType == SocketType.Dgram))
            this.DoClose(client);
    }

    protected override int DeviceRead(byte[] buffer, int offset, int count, int timeout)
    {
        if (this.asyncIO)
        {
            return -1;
        }

        if (this.socketType == SocketType.Dgram)
            return this.ReadFrom(buffer, offset, count, timeout);

        var client = this.socket;
        var readable = client != null && client.Poll(timeout, SelectMode.SelectRead) && client.Connected && client.Poll(0, SelectMode.SelectWrite);
        var received = 0;

        try
        { received = readable ? client!.Receive(buffer, offset, count, SocketFlags.None) : -1; }
        catch (Exception) { }

        return received;
    }

    protected override int DeviceWrite(byte[] buffer, int offset, int count)
    {
        if (this.socketType == SocketType.Dgram)
            return this.WriteTo(buffer, offset, count);

        var client = this.socket;

        if (client == null)
            return -1;

        if (this.asyncIO)
        {
            client.BeginSend(buffer, offset, count, SocketFlags.None, this.EndSendData, client);
            return count;
        }

        var sent = client != null ? client.Send(buffer, offset, count, SocketFlags.None) : 0;
        return sent;
    }

    private void DataReadEnd(IAsyncResult ar)
    {
        try
        {
            var socket = (Socket)ar.AsyncState!;
            var count = socket.EndReceive(ar);

            if (count > 0)
            {
                if (this.buffer != null)
                {
                    this.buffer.Push(this.receiveBuffer.AsSpan()[..count]);
                    var data = this.buffer.Pop();

                    while (!data.IsEmpty)
                    {
                        this.FireReceiveData(data);
                        data = this.buffer.Pop();
                    }
                }
                else
                {
                    this.FireReceiveData(this.receiveBuffer.AsSpan()[..count]);
                }

                this.DoSetupAsyncIO();
            }
            else
            {
                this.Disconnect(true);
            }
        }
        catch (Exception ex)
        {
            if (ex is not SocketException)
                Trace.Error(ex.Message);

            this.Disconnect(true);
        }
    }

    protected override void FireDisconnected(bool lost)
    {
        base.FireDisconnected(lost);

        if (lost && this.Reconnect && this.asyncIO)
        {
            ThreadRunner.Spawn(this.DoAsyncIOReconnect);
        }
    }

    private void EndSendData(IAsyncResult ar)
    {
        try
        {
            var socket = (Socket)ar.AsyncState!;
            socket.EndSend(ar);
        }
        catch (Exception e)
        {
            Trace.Exception(e);
        }
    }

    public int WriteTo(byte[] buffer, int offset, int count)
    {
        return this.WriteTo(buffer, offset, count, this.Context as SocketEndPoint);
    }

    public int WriteTo(byte[] buffer, int offset, int count, SocketEndPoint? ep)
    {
        var client = this.socket;
        var sent = client != null && ep != null ? client.SendTo(buffer, offset, count, SocketFlags.None, ep.IP!) : 0;
        return sent;
    }

    public int ReadFrom(byte[] buffer, int offset, int count, int timeout)
    {
        if (this.asyncIO)
        {
            return -1;
        }

        var client = this.socket;
        System.Net.EndPoint nep = new IPEndPoint(0, 0);
        var received = client != null ? client.ReceiveFrom(buffer, offset, count, SocketFlags.None, ref nep) : 0;
        this.Context = new SocketEndPoint(nep as IPEndPoint);
        return received;
    }

    protected override int GetReceiveBufferSize()
    {
        try
        { return this.socket?.ReceiveBufferSize ?? 0; }
        catch (Exception) { return 0; }
    }

    protected override int GetSendBufferSize()
    {
        try
        { return this.socket?.SendBufferSize ?? 0; }
        catch (Exception) { return 0; }
    }
}
