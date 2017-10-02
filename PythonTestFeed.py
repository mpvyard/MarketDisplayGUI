import socket
import time






# Establish socket connection 
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

HOST = '127.0.0.1'
PORT = 1337

s.connect((HOST, PORT))

# Loop, producing data
while True:
    s.sendall('PRICE:IBM:ADD:BID:100:5\n')
    s.sendall('PRICE:IBM:ADD:ASK:101:10\n')
    #s.sendall('Test 123'.encode('latin_1'))
    
    
    # delay one second to send additional updates
    time.sleep(1)




#time.sleep(5)

#s.close()


