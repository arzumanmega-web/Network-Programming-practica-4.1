using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Client
{
    public class FakeHttpResponse
    {
        public string raw { get; set; }
    }
    public class FakeHttpClient
    {
        private readonly string _host;
        private readonly int _port;

        public FakeHttpClient(string host, int port)
        {
            _host = host;
            _port = port;
        }
        public async Task<FakeHttpResponse> SendAsync(string method, string path, object body)
        {
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(_host), _port);
            await using var stream = client.GetStream();
            var requestPath = $"{method} {path}";
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                requestPath += "\n" + json;

            }
            var bytes = Encoding.UTF8.GetBytes(requestPath);
            await stream.WriteAsync(bytes);
            Console.WriteLine("\n______________Raw Tcp Request____________");
            Console.WriteLine(requestPath);
            var buffer = new byte[8192];
            var count = await stream.ReadAsync(buffer);
            var responseText = Encoding.UTF8.GetString(buffer, 0, count);
            return new FakeHttpResponse { raw = responseText };



        }
    }

    public class Program
    {
        static FakeHttpClient client = new FakeHttpClient("192.168.1.82", 27001);
        static async Task Main(string[] args)
        {

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("MINI Fake Postman Client");
                Console.WriteLine("1. GET /products");
                Console.WriteLine("2. GET /products/{id}");
                Console.WriteLine("3. POST /products");
                Console.WriteLine("4. PUT /products/{id}");
                Console.WriteLine("5. DELETE /products/{id}");
                Console.WriteLine("0. Exit");

                Console.WriteLine("Choose: ");

                var choice = Console.ReadLine();
                if (choice == "0")
                {
                    break;
                }

                try
                {
                    switch (choice)
                    {
                        case "1":
                            {
                                await SendAsync("GET", "/products");
                                break;
                            }
                        case "2":
                            {
                                Console.WriteLine("Product ID : ");
                                var getId = Console.ReadLine();
                                await SendAsync("GET", $"/products/{getId}");
                                break;
                            }
                        case "3":
                            {
                                Console.WriteLine("Enter name : ");
                                string name = Console.ReadLine();
                                Console.WriteLine("Enter price : ");
                                decimal price = decimal.Parse(Console.ReadLine()!);

                                await SendAsync("POST", "/products", new { name, price });
                                break;
                            }
                        case "4":
                            {

                                
                                Console.WriteLine("Product ID : ");
                                var UpdateId = Console.ReadLine()!;
                                Console.WriteLine("Update price: ");
                                var UpdatePrice =Console.ReadLine();
                                await SendAsync("PUT",$"/products/{UpdateId}/{UpdatePrice}");

                                break;
                            }
                        case "5":
                            {
                                Console.WriteLine("Product ID : ");
                                var DeleteId = Console.ReadLine()!;
                                await SendAsync("DELETE", $"/products/{DeleteId}");
                                break;
                            }
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static async Task SendAsync(string method, string path, object? body = null)
        {
            var response = await client.SendAsync(method, path, body);

            Console.WriteLine();
            Console.WriteLine("------ RESPONSE RAW -------");
            Console.WriteLine(response.raw);
        }
    }
}