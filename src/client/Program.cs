using System.Net.Sockets;
using System.Text;
using HyperVClient;

const int DefaultPort = 5000;

if (args.Length < 1)
{
    Console.WriteLine("AF_HYPERV Hello World Client");
    Console.WriteLine("============================");
    Console.WriteLine();
    Console.WriteLine("Usage: HyperVClient <WSL2-VM-GUID> [port]");
    Console.WriteLine();
    Console.WriteLine("  WSL2-VM-GUID  The GUID of your WSL2 VM.");
    Console.WriteLine("                Run 'hcsdiag list' to find it.");
    Console.WriteLine($"  port          VSOCK port (default: {DefaultPort})");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  HyperVClient b99e7037-a1a0-4c86-98e0-bbbb2f1256aa");
    return;
}

Guid vmId = Guid.Parse(args[0]);
int port = args.Length > 1 ? int.Parse(args[1]) : DefaultPort;

Guid serviceId = HvSocketEndPoint.VsockPortToServiceId(port);
var endpoint = new HvSocketEndPoint(vmId, serviceId);

Console.WriteLine($"Connecting to WSL2 VM...");
Console.WriteLine($"  VM ID:      {vmId}");
Console.WriteLine($"  Service ID: {serviceId} (VSOCK port {port})");

using var socket = new Socket(
    HvSocketEndPoint.HyperV,
    SocketType.Stream,
    (ProtocolType)HvSocketEndPoint.HV_PROTOCOL_RAW);

try
{
    socket.Connect(endpoint);
    Console.WriteLine("Connected!");

    // Prompt for message
    Console.Write("Message: ");
    string message = Console.ReadLine() ?? string.Empty;

    socket.Send(Encoding.UTF8.GetBytes(message));
    Console.WriteLine($"Sent:     {message}");

    // Receive response
    byte[] buffer = new byte[1024];
    int received = socket.Receive(buffer);
    string response = Encoding.UTF8.GetString(buffer, 0, received);
    Console.WriteLine($"Received: {response}");

    socket.Shutdown(SocketShutdown.Both);
    Console.WriteLine("Done.");
}
catch (SocketException ex)
{
    Console.Error.WriteLine($"Socket error: {ex.Message} (code: {ex.SocketErrorCode})");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Troubleshooting:");
    Console.Error.WriteLine("  1. Is the Python server running in WSL2?");
    Console.Error.WriteLine("  2. Is the VM GUID correct? Run: hcsdiag list");
    Console.Error.WriteLine("  3. Are you running this with sufficient privileges?");
    Environment.Exit(1);
}
