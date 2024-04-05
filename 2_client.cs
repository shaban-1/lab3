
using System;
using System.IO;
using System.Net.Sockets;

namespace MultiThreadedClient
{
    class Program
    {
        static readonly string dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "client", "data");

        static void Main(string[] args)
        {
            if (!Directory.Exists(dataDirectory))
                Directory.CreateDirectory(dataDirectory);

            Console.WriteLine("Enter action (1 - get a file, 2 - save a file, 3 - delete a file, exit - to exit):");
            string action = Console.ReadLine();

            using (TcpClient client = new TcpClient("127.0.0.1", 8888))
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                if (action == "1") // GET
                {
                    Console.WriteLine("Do you want to get the file by name or by id (1 - name, 2 - id):");
                    string getBy = Console.ReadLine();

                    if (getBy == "1") // By Name
                    {
                        Console.WriteLine("Enter name of the file:");
                        string fileName = Console.ReadLine();

                        // Отправляем серверу команду GET BY_NAME и имя файла
                        writer.WriteLine($"GET BY_NAME {fileName}");
                        writer.Flush();

                        // Получаем ответ от сервера
                        string response = reader.ReadLine();
                        if (response == "404")
                            Console.WriteLine("File not found on the server!");
                        else
                        {
                            Console.WriteLine("The file was downloaded! Specify a name for it:");
                            string saveFileName = Console.ReadLine();
                            File.WriteAllBytes(Path.Combine(dataDirectory, saveFileName), Convert.FromBase64String(response.Split(' ')[1]));
                            Console.WriteLine("File saved on the hard drive!");
                        }
                    }
                    else if (getBy == "2") // By ID
                    {
                        Console.WriteLine("Enter id:");
                        string fileId = Console.ReadLine();

                        // Отправляем серверу команду GET BY_ID и идентификатор файла
                        writer.WriteLine($"GET BY_ID {fileId}");
                        writer.Flush();

                        // Получаем ответ от сервера
                        string response = reader.ReadLine();
                        if (response == "404")
                            Console.WriteLine("File not found on the server!");
                        else
                        {
                            Console.WriteLine("The file was downloaded! Specify a name for it:");
                            string saveFileName = Console.ReadLine();
                            File.WriteAllBytes(Path.Combine(dataDirectory, saveFileName), Convert.FromBase64String(response.Split(' ')[1]));
                            Console.WriteLine("File saved on the hard drive!");
                        }
                    }
                }
                else if (action == "3") // DELETE
                {
                    Console.WriteLine("Do you want to delete the file by name or by id (1 - name, 2 - id):");
                    string deleteBy = Console.ReadLine();

                    if (deleteBy == "1") // By Name
                    {
                        Console.WriteLine("Enter name of the file:");
                        string fileName = Console.ReadLine();

                        // Отправляем серверу команду DELETE BY_NAME и имя файла
                        writer.WriteLine($"DELETE BY_NAME {fileName}");
                        writer.Flush();

                        // Получаем ответ от сервера
                        string response = reader.ReadLine();
                        if (response == "404")
                            Console.WriteLine("File not found on the server!");
                        else
                            Console.WriteLine("The response says that this file was deleted successfully!");
                    }
                    else if (deleteBy == "2") // By ID
                    {
                        Console.WriteLine("Enter id:");
                        string fileId = Console.ReadLine();

                        // Отправляем серверу команду DELETE BY_ID и идентификатор файла
                        writer.WriteLine($"DELETE BY_ID {fileId}");
                        writer.Flush();

                        // Получаем ответ от сервера
                        string response = reader.ReadLine();
                        if (response == "404")
                            Console.WriteLine("File not found on the server!");
                        else
                            Console.WriteLine("The response says that this file was deleted successfully!");
                    }
                }
                else if (action == "2") // PUT
                {
                    Console.WriteLine("Enter name of the file:");
                    string fileName = Console.ReadLine();

                    Console.WriteLine("Enter name of the file to be saved on server:");
                    string saveFileName = Console.ReadLine();


                    // Отправляем серверу команду PUT и имя файла
                    writer.WriteLine($"PUT {saveFileName}");
                    writer.Flush();

                    // Открываем файл и отправляем его содержимое серверу
                    using (FileStream fileStream = File.OpenRead(Path.Combine(dataDirectory, fileName)))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, bytesRead);
                        }
                    }

                    // Явно сбрасываем буфер и дожидаемся завершения передачи данных
                    stream.Flush();
                    client.Client.Shutdown(SocketShutdown.Send);

                    // Получаем ответ от сервера
                    string response = reader.ReadLine();
                    Console.WriteLine($"The request was sent.\nResponse says that file is saved! ID = {response.Split(' ')[1]}");
                }

                else if (action == "exit")
                {
                    // Отправляем серверу команду exit и закрываем соединение
                    writer.WriteLine("exit");
                    writer.Flush();
                }
                else
                {
                    Console.WriteLine("Invalid action");
                }
            }
        }
    }
}
