using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var ep = new IPEndPoint(IPAddress.Parse("192.168.1.82"), 27001);
var client = new TcpClient();

client.Connect(ep);

if (client.Connected)
{
    var stream = client.GetStream();

    while (true)
    {
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        var boardState = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.WriteLine(boardState);

        // Prompt the player to make a move
        Console.WriteLine("Enter your move (row and column, e.g., '01' for row 0, column 1):");
        var move = Console.ReadLine();

        if (!string.IsNullOrEmpty(move))
        {
            // Send move to the server
            var moveBytes = Encoding.UTF8.GetBytes(move);
            await stream.WriteAsync(moveBytes, 0, moveBytes.Length);
        }
    }
}
