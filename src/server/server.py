#!/usr/bin/env python3
"""
AF_VSOCK server for WSL2.
Listens for incoming Hyper-V socket connections from the Windows host.
"""

import socket
import sys

AF_VSOCK = 40                    # Address family for VM sockets
VMADDR_CID_ANY = 0xFFFFFFFF     # Accept connections from any CID (like INADDR_ANY)
DEFAULT_PORT = 5000


def main():
    port = int(sys.argv[1]) if len(sys.argv) > 1 else DEFAULT_PORT

    sock = socket.socket(AF_VSOCK, socket.SOCK_STREAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind((VMADDR_CID_ANY, port))
    sock.listen(1)

    print(f"AF_VSOCK server listening on port {port} (CID=ANY)")
    print("Waiting for connection from Windows host...")
    print("Press Ctrl+C to stop.\n")

    try:
        while True:
            conn, addr = sock.accept()
            cid, remote_port = addr
            print(f"Connection from CID={cid}, port={remote_port}")

            data = conn.recv(1024)
            if data:
                message = data.decode("utf-8")
                print(f"Received: {message}")

                response = f"Hello From WSL2. Received '{message}' via AF_VSOCK / AF_HYPERV!"
                conn.sendall(response.encode("utf-8"))
                print(f"Sent:     {response}")

            conn.close()
            print("Connection closed.\n")
            print("Waiting for next connection...")
    except KeyboardInterrupt:
        print("\nShutting down.")
    finally:
        sock.close()


if __name__ == "__main__":
    main()
