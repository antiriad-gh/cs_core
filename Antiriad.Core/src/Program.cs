using System.Text;
using Antiriad.Core.Config;
using Antiriad.Core.IO;
using Antiriad.Core.Log;
using Antiriad.Core.Serialization;
using Antiriad.Core.Threading;

namespace test_dn02;

class Program
{
  static readonly List<Connector> clients = new();
  static readonly ClientListener clientListener = new();

  public static int GetSize(ReadOnlySpan<byte> buffer)
  {
    for (var i = 0; i < buffer.Length; i++)
      if (buffer[i] == '.') return i + 1;
    return -1;
  }

  internal class ClientListener : IConnectorDataEvents
  {
    public void ConnectionClosed(Connector sender, bool lost)
    {
    }

    public void ConnectionEstablished(Connector sender)
    {
    }

    public void DataReceived(Connector sender, ReadOnlySpan<byte> buffer)
    {
      var s1 = Encoding.ASCII.GetString(buffer);
      Trace.Debug($"size={buffer.Length} data={s1}");
    }
  }

  internal class Listener : IConnectorListenerEvents
  {
    public void ClientConnected(Connector client)
    {
      Program.clients.Add(client);
      client.Sizer = Program.GetSize;
      client.Activate(clientListener);
    }
  }

  static readonly ManualResetEvent stop = new(false);

  public class Serial1
  {
    public int Id { get; set; }
    public string Name { get; set; }
  }

  static void Main(string[] args)
  {
    Span<byte> s = stackalloc byte[10];

    SpanWriter w = new(s);
    w.WriteByte(1);
    w.WriteUInt16(ushort.MaxValue);
    SpanReader p = new(s);
    var v1 = p.ReadByte();
    var v2 = p.ReadUInt16();

    var now = DateTime.Now;
    Trace.Debug($"test={1} stamp={now:HH:mm:ss.fff}");
    Thread.Sleep(50);
    Trace.Debug($"test={2} stamp={now:HH:mm:ss.fff}");

    var o1 = new Serial1 { Id = 123, Name = "jon doe" };
    var e1 = NaibSerializer.Encode(o1);
    var o2 = NaibDeserializer.Decode<object>(e1);
    e1 = null;

    var config = DynamicConfiguration.GetSection("settings");
    var director = config.Get("director", false);
    var test1 = config.Get("inner/value", "null");
    var test2 = config.Get("subtag/inner/value", "null");

    var server = new SocketServer("0:1234");
    server.Activate(new Listener());

    var client = new SocketConnector("localhost:1234");
    client.Sizer = Program.GetSize;
    client.Activate();
    var buf = Encoding.ASCII.GetBytes("12345.");
    client.Write(buf);

    ThreadRunner.Spawn(o => DoRun(o));
    Thread.Sleep(10000);

    stop.Set();
    client.Deactivate();
    clients.ForEach(i => i.Deactivate());
    server.Deactivate();

    /*Slice slice = new();
    JsonRpcPacketCompleter jpc = new();
    ConnectorBuffer buf = new(jpc.GetSize);

    var p1 = Encoding.ASCII.GetBytes("{test_1}{test__2}{test___3");
    buf.Push(p1, 0, p1.Length);
    
    if (buf.Pop(slice))
    {
      var s1 = Encoding.ASCII.GetString(slice.Buffer, slice.Offset, slice.Count);
      Console.WriteLine($"packet size={slice.Count} data={s1}");
    }
    
    if (buf.Pop(slice))
    {
      var s1 = Encoding.ASCII.GetString(slice.Buffer, slice.Offset, slice.Count);
      Console.WriteLine($"packet size={slice.Count} data={s1}");
    }
    
    if (buf.Pop(slice))
    {
      var s1 = Encoding.ASCII.GetString(slice.Buffer, slice.Offset, slice.Count);
      Console.WriteLine($"packet size={slice.Count} data={s1}");
    }

    var p2 = Encoding.ASCII.GetBytes("}");
    buf.Push(p2, 0, p2.Length);
    
    if (buf.Pop(slice))
    {
      var s1 = Encoding.ASCII.GetString(slice.Buffer, slice.Offset, slice.Count);
      Console.WriteLine($"packet size={slice.Count} data={s1}");
    }*/
  }

  static void DoRun(object o)
  {
    var counter = 0;

    while (!stop.WaitOne(500))
    {
      Trace.Debug($"counter={counter++}");
    }
  }
}
