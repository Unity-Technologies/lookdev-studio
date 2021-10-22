import socket
import bpy
import time
import threading

def loopingForReceivingMsg():
    print('Looping')
    return 1.5

def socket_server(*args):

    HOST = '127.0.0.1'
    PORT = 6002

    BufferSize = 4096

    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM, socket.IPPROTO_TCP)
    s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, True)
    s.bind((HOST, PORT))
    s.listen()
    

    #bpy.app.timers.register(loopingForReceivingMsg)
    rcvCmd = '';

    while rcvCmd != 'end' :

        conn, addr = s.accept()

        if not conn :
            continue

        data = conn.recv(BufferSize)
        '''
        if not data:
            break
        '''
        rcvCmd = data.decode("ascii")

        print ('## Received :' + rcvCmd)

        if rcvCmd != '' :
            exec(data.decode())
        
        #conn.send(b'ok')
        time.sleep(1.0)

        conn.close()
    
    s.close()

    


try:
    t = threading.Thread(None, socket_server)
    t.start()


except Exception as e:
    print (e)
