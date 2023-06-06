# Example Tcp Client Server

### Server available commands
```
help - list all commands
broadcast <message> - send message to all connected clients
clients - list all connected clients
close - close socket (safe way to close all connections) 
exit - close program
```
  
### Client available commands
```
help - list all commands
send <message> - send message to server
send gettime - get current timestamp from server
send ping - if connected, server response "pong"
close - close socket (safe way to close connection) 
exit - close program
```
