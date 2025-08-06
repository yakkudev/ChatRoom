using System.Net.Sockets;

namespace ChatRoom;

public class ChatClient {

    private string host;
    private int port;

    private TcpClient client;

    public ChatClient(string host, int port) {
        this.host = host;
        this.port = port;

        Connect();
    }

    private void Connect() {
        Console.WriteLine($"connecting to {host}, port {port}");
        var retries = 10;
        while (client == null) {
            try {
                client = new TcpClient(host, port);
            }
            catch (Exception e){
                if (e is SocketException && retries-- > 0) {
                    Console.WriteLine("retrying connection...");
                    Thread.Sleep(1000);
                    continue;
                }

                Console.WriteLine("an error occured.");
                return;
            }
        }

        Console.WriteLine("###### CONNECTED ######"); 
        var message = "PING!";
    }
}