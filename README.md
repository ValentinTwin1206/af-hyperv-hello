# AF_HYPERV Hello World

A minimal hello-world project over virtual socket connections between a **Windows host** and a **WSL2 VM**.

## About

Hyper-V sockets establish a direct channel between host and VM through shared memory on the hypervisor layer — no TCP/IP, no network adapters, no DNS. As a result, **Windows Firewall rules and network policies do not apply**, making the channel reliable even in locked-down or air-gapped environments.

The client is written in **C#** (`AF_HYPERV`, Windows) as the idiomatic choice for Windows/.NET system programming. The server is written in **Python** (`AF_VSOCK`, WSL2) because it ships pre-installed in Ubuntu with `AF_VSOCK` support built into the standard `socket` module.

**Port mapping:** VSOCK ports map to AF_HYPERV service GUIDs using the Microsoft template `{port_as_8_hex_digits}-facb-11e6-bd58-64006a7986d3`. For example, port `5000` (`0x1388`) → `00001388-facb-11e6-bd58-64006a7986d3`.

---

## Prerequisites

- **Windows 11** with WSL2 enabled
- **WSL2** distribution installed (e.g. Ubuntu)
- **.NET 8 SDK** on Windows
- **Python 3.6+** in WSL2 (usually pre-installed)

--- 

## Usage

### Register *AF_HYPERV Hello World*

According to the [Microsoft Hyper-V Integration Services documentation](https://learn.microsoft.com/en-us/windows-server/virtualization/hyper-v/make-integration-service), every `AF_HYPERV` socket service must be **registered in the Windows registry** before connections are permitted. Without this, `Connect()` fails with an access-denied error.

Run in an **elevated PowerShell** (the script self-elevates via UAC if needed):

```powershell
.\scripts\Register-HvService.ps1
```

You should see:

```
Registered: 00001388-facb-11e6-bd58-64006a7986d3 (port 5000)
```

Verify with:

```powershell
Get-Item 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Virtualization\GuestCommunicationServices\00001388-facb-11e6-bd58-64006a7986d3'
```

You should see:

```
Name                           Property
----                           --------
00001388-facb-11e6-bd58-640... ElementName : AF_HYPERV Hello World (port 5000)
```

### Create AF_HYPERV Connection

#### Get the WSL2 LUVM GUID

In an elevated PowerShell, run `hcsdiag list` and copy the GUID of the `WSL` entry:

```powershell
hcsdiag list
```

You should see:

```
b99e7037-a1a0-4c86-98e0-bbbb2f1256aa
    VM,                         Running, b99e7037-a1a0-4c86-98e0-bbbb2f1256aa, WSL
```

#### Run the Server in WSL2

Open a terminal inside your WSL distribution and run the `server.py`:

```bash
python3 /{path_to_project}/af-hyperv-hello/src/server/server.py
```

```
AF_VSOCK server listening on port 5000 (CID=ANY)
Waiting for connection from Windows host...
Press Ctrl+C to stop.
```

#### Run the Client on Windowa

From the repo root, passing the GUID from `hcsdiag list`:

```powershell
dotnet run --project src\client -- "<WSL2-LUVM-GUID>"
```

You should see:

###### Windows (client)

```
Connecting to WSL2 VM...
  VM ID:      b99e7037-a1a0-4c86-98e0-bbbb2f1256aa
  Service ID: 00001388-facb-11e6-bd58-64006a7986d3 (VSOCK port 5000)
Connected!
Message:
```

###### WSL2 (server)

```
Connection from CID=2, port=731991331
```

### Unregister *AF_HYPERV Hello World*

This is a **one-time setup** per machine. To remove it:

```powershell
.\scripts\Register-HvService.ps1 -Unregister
```

---

## License

The *AF_HYPERV Hello World* project is released under the [Apache 2.0](LICENSE.MD) license.

---

## Troubleshooting

### "Socket error: An attempt was made to access a socket in a way forbidden by its access permissions"

- **Register the service GUID first** (see step 0) in an elevated PowerShell.
- Try running the client **as Administrator**.
- Ensure the WSL2 VM GUID is correct (`hcsdiag list`).

### "Connection refused" or timeout

- Verify the Python server is running in WSL2 **before** starting the client.
- Confirm WSL2 is actually running: `wsl --list --running`.

### AF_VSOCK not available in WSL2

- Check your WSL2 kernel version: `uname -r` (needs 5.10+).
- Verify VSOCK support:

  ```bash
  zgrep -E "CONFIG_VSOCKET|CONFIG_HYPERV_VSOCKETS" /proc/config.gz
  ```

  You should see:

  ```bash
  CONFIG_VSOCKETS=y
  CONFIG_VSOCKETS_DIAG=m
  CONFIG_VSOCKETS_LOOPBACK=m
  CONFIG_HYPERV_VSOCKETS=y
  ```

### `hcsdiag` not found

- `hcsdiag` ships with Windows but may not be on `PATH`.
- Full path: `C:\Windows\System32\hcsdiag.exe`.
- Alternatively, use the Hyper-V PowerShell module: `Get-VM | Where-Object { $_.Name -match 'WSL' }`.
