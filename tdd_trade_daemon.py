import sys
import socket
import time

def main() :
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.connect(("127.0.0.1", 44444))

    send_buf1 = bytearray(3);

    send_buf1[0] = 0 #Buy
#   send_buf1[0] = 1 #Sell

#   send_buf1[1] = 0 #TX
    send_buf1[1] = 1 #MTX

    send_buf1[2] = 1 #order amount

#   send_buf2 = (17000).to_bytes(4, "little") #limit order
    send_buf2 = (0xffffffff).to_bytes(4, "little") #market order

    send_buf = send_buf1 + send_buf2;
    while True :
        s.send(send_buf)
        recv_buf = s.recv(1)
        print(f"back -> {recv_buf}");
        time.sleep(2)



if __name__ == '__main__':
    sys.exit(main())

