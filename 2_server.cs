using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MultiThreadedServer
{
    class Program
    {
        static Dictionary<int, string> fileDictionary = new Dictionary<int, string>();
        static int fileIdCounter = 1;
        static readonly string dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "server", "data");

        static void Main(string[] args)
        {
            Console.WriteLine("Server started!");
            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);

            LoadFileDictionary();

            StartServer();
        }

        static void StartServer()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                clientThread.Start(client);
            }
        }

        static void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");

            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            try
            {
                while (true)
                {
                    string request = reader.ReadLine();
                    if (request == "exit" || request == null)
                        break;

                    string[] requestData = request.Split(' ');
                    string response = ProcessRequest(requestData, reader, writer);
                    writer.WriteLine(response);
                    writer.Flush();

                    Console.WriteLine($"Client {client.Client.RemoteEndPoint} requested: {request}. Response: {response}");
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Client {client.Client.RemoteEndPoint} disconnected: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }


        static string ProcessRequest(string[] requestData, StreamReader reader, StreamWriter writer)
        {
            string action = requestData[0];
            string response = "";

            if (action == "PUT")
            {
                string fileName = requestData[1];
                int fileId = fileIdCounter++;
                fileDictionary.Add(fileId, fileName);

                string filePath = Path.Combine(dataDirectory, fileId.ToString());

                using (FileStream fileStream = File.Create(filePath))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = reader.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }
                }

                SaveFileDictionary();

                response = $"200 {fileId}";
            }
            else if (action == "GET")
            {
                if (requestData[1] == "BY_ID")
                {
                    int fileId = int.Parse(requestData[2]);
                    if (fileDictionary.ContainsKey(fileId))
                    {
                        string fileName = fileDictionary[fileId];
                        string filePath = Path.Combine(dataDirectory, fileId.ToString());
                        if (File.Exists(filePath))
                        {
                            byte[] fileBytes = File.ReadAllBytes(filePath);
                            writer.WriteLine($"200 {Convert.ToBase64String(fileBytes)} ");
                            writer.Flush();
                        }
                        else
                        {
                            response = $"404 {fileId} {filePath}";
                        }
                    }
                    else
                    {
                        response = "404";
                    }
                }
                else if (requestData[1] == "BY_NAME")
                {
                    string fileName = requestData[2];
                    if (fileDictionary.ContainsValue(fileName))
                    {
                        int fileId = fileDictionary.FirstOrDefault(x => x.Value == fileName).Key;
                        string filePath = Path.Combine(dataDirectory, fileId.ToString());
                        if (File.Exists(filePath))
                        {
                            byte[] fileBytes = File.ReadAllBytes(filePath);
                            writer.WriteLine($"200 {Convert.ToBase64String(fileBytes)} ");
                            writer.Flush();
                        }
                        else
                        {
                            response = $"404 {fileName} {filePath}";
                        }
                    }
                    else
                    {
                        response = "404";
                    }
                }
            }
            else if (action == "DELETE")
            {
                if (requestData[1] == "BY_ID")
                {
                    int fileId = int.Parse(requestData[2]);
                    if (fileDictionary.ContainsKey(fileId))
                    {
                        string filePath = Path.Combine(dataDirectory, fileId.ToString());
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            fileDictionary.Remove(fileId);
                            SaveFileDictionary();
                            response = "200";
                        }
                        else
                        {
                            response = "404";
                        }
                    }
                    else
                    {
                        response = "404";
                    }
                }
                else if (requestData[1] == "BY_NAME")
                {
                    string fileName = requestData[2];
                    if (fileDictionary.ContainsValue(fileName))
                    {
                        int fileId = fileDictionary.FirstOrDefault(x => x.Value == fileName).Key;
                        string filePath = Path.Combine(dataDirectory, fileId.ToString());
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            fileDictionary.Remove(fileId);
                            SaveFileDictionary();
                            response = "200";
                        }
                        else
                        {
                            response = "404";
                        }
                    }
                    else
                    {
                        response = "404";
                    }
                }
            }
            return response;
        }

        static void LoadFileDictionary()
        {
            string fileDictionaryFile = Path.Combine(dataDirectory, "fileDictionary.txt");
            if (File.Exists(fileDictionaryFile))
            {
                string[] lines = File.ReadAllLines(fileDictionaryFile);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int fileId))
                    {
                        fileDictionary.Add(fileId, parts[1]);
                    }
                }

                if (fileDictionary.Any())
                {
                    fileIdCounter = fileDictionary.Keys.Max() + 1;
                }
            }
        }

        static void SaveFileDictionary()
        {
            string fileDictionaryFile = Path.Combine(dataDirectory, "fileDictionary.txt");
            List<string> lines = new List<string>();
            foreach (var kvp in fileDictionary)
            {
                lines.Add($"{kvp.Key},{kvp.Value}");
            }
            File.WriteAllLines(fileDictionaryFile, lines);
        }
    }
}
