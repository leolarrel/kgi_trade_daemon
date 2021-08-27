import sys
import socket
import time

def main() :
    if len(sys.argv) != 2 :
        return -1

    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.connect(("127.0.0.1", 44444))

    while True :
        s.send(sys.argv[1].encode('ascii'))
        back_buffer = s.recv(1024)
        back = back_buffer.decode('ascii')
        print(f"back -> {back}");
        if sys.argv[1] != back :
            return -1
        time.sleep(2)



if __name__ == '__main__':
    sys.exit(main())

