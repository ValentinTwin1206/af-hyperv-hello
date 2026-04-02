using System.Net;
using System.Net.Sockets;

namespace HyperVClient;

/// <summary>
/// Custom EndPoint for Hyper-V sockets (AF_HYPERV / address family 34).
/// Maps a VM ID + Service ID (GUID pair) to a SOCKADDR_HV structure.
/// </summary>
public class HvSocketEndPoint : EndPoint
{
    // AF_HYPERV = 34
    public const int AF_HYPERV_VALUE = 34;
    public static readonly AddressFamily HyperV = (AddressFamily)AF_HYPERV_VALUE;

    // HV_PROTOCOL_RAW = 1
    public const int HV_PROTOCOL_RAW = 1;

    // SOCKADDR_HV: Family(2) + Reserved(2) + VmId(16) + ServiceId(16) = 36 bytes
    private const int SocketAddressSize = 36;

    public Guid VmId { get; }
    public Guid ServiceId { get; }

    public HvSocketEndPoint(Guid vmId, Guid serviceId)
    {
        VmId = vmId;
        ServiceId = serviceId;
    }

    public override AddressFamily AddressFamily => HyperV;

    public override SocketAddress Serialize()
    {
        var sa = new SocketAddress(HyperV, SocketAddressSize);

        // Bytes [0]-[1]: Address family (set by SocketAddress ctor)
        // Bytes [2]-[3]: Reserved = 0
        sa[2] = 0;
        sa[3] = 0;

        // Bytes [4]-[19]: VM ID (16 bytes, little-endian GUID)
        byte[] vmIdBytes = VmId.ToByteArray();
        for (int i = 0; i < 16; i++)
            sa[4 + i] = vmIdBytes[i];

        // Bytes [20]-[35]: Service ID (16 bytes, little-endian GUID)
        byte[] serviceIdBytes = ServiceId.ToByteArray();
        for (int i = 0; i < 16; i++)
            sa[20 + i] = serviceIdBytes[i];

        return sa;
    }

    public override EndPoint Create(SocketAddress socketAddress)
    {
        byte[] vmIdBytes = new byte[16];
        byte[] serviceIdBytes = new byte[16];

        for (int i = 0; i < 16; i++)
        {
            vmIdBytes[i] = socketAddress[4 + i];
            serviceIdBytes[i] = socketAddress[20 + i];
        }

        return new HvSocketEndPoint(new Guid(vmIdBytes), new Guid(serviceIdBytes));
    }

    /// <summary>
    /// Converts a VSOCK port number to a Hyper-V service GUID.
    /// Format: {port_hex_8chars}-facb-11e6-bd58-64006a7986d3
    /// </summary>
    public static Guid VsockPortToServiceId(int port)
    {
        string hex = port.ToString("x8");
        return Guid.Parse($"{hex}-facb-11e6-bd58-64006a7986d3");
    }

    public override string ToString() => $"HvSocket(VM={VmId}, Service={ServiceId})";
}
