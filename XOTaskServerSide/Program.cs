using System.ComponentModel.Design;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

var ep = new IPEndPoint(IPAddress.Parse("192.168.1.82"), 27001);

var server = new TcpListener(ep);

server.Start();

Console.WriteLine("Server started...");

var clients = new List<TcpClient>();

char[,] board = new char[3, 3];
char currentPlayer = 'X';
bool gameActive = true;

void InitBoard()
{
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            board[i, j] = '-';
        }
    }
}

string DisplayBoard()
{
    var sb = new StringBuilder();
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            sb.Append(board[i, j] + " ");
        }
        sb.AppendLine();
    }
    return sb.ToString();
}

bool CheckWin()
{
    for (int i = 0; i < 3; i++)
    {
        if (board[i, 0] == currentPlayer && board[i, 1] == currentPlayer && board[i, 2] == currentPlayer) return true;
        if (board[0, i] == currentPlayer && board[1, i] == currentPlayer && board[2, i] == currentPlayer) return true;
    }
    if (board[0, 0] == currentPlayer && board[1, 1] == currentPlayer && board[2, 2] == currentPlayer) return true;
    if (board[0, 2] == currentPlayer && board[1, 1] == currentPlayer && board[2, 0] == currentPlayer) return true;
    return false;
}

bool CheckDraw()
{
    foreach (var cell in board)
    {
        if (cell == '-') return false;
    }
    return true;
}

async Task BroadcastGameState()
{
    string boardState = DisplayBoard();
    var bytes = Encoding.UTF8.GetBytes(boardState);

    foreach (var client in clients)
    {
        var stream = client.GetStream();
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }
}

InitBoard();

while (clients.Count < 2)
{
    var client = await server.AcceptTcpClientAsync();
    clients.Add(client);
    Console.WriteLine("Player connected.");
}



while (gameActive)
{
    var currentPlayerIndex = currentPlayer == 'X' ? 0 : 1;
    var client = clients[currentPlayerIndex];
    var stream = client.GetStream();

    // Send game board to the current player
    await BroadcastGameState();

    // Receive player's move (row and column)
    var buffer = new byte[1024];
    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
    var move = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

    if (int.TryParse(move[0].ToString(), out int row) && int.TryParse(move[1].ToString(), out int col))
    {
        if (board[row, col] == '-')
        {
            board[row, col] = currentPlayer;

            if (CheckWin())
            {
                await BroadcastGameState();
                await BroadcastMessage($"{currentPlayer} wins!");
                gameActive = false;
            }
            else if (CheckDraw())
            {
                await BroadcastGameState();
                await BroadcastMessage("The game is a draw!");
                gameActive = false;
            }
            else
            {
                currentPlayer = currentPlayer == 'X' ? 'O' : 'X'; // Switch turns
            }
        }
        else
        {
            await BroadcastMessage("Invalid move! Try again.");
        }
    }
}

async Task BroadcastMessage(string message)
{
    var bytes = Encoding.UTF8.GetBytes(message);
    foreach (var client in clients)
    {
        var stream = client.GetStream();
        await stream.WriteAsync(bytes, 0, bytes.Length);
    }
}