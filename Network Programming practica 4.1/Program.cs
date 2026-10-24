using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Network_Programming_practica_4._1.DataAccess;
using Network_Programming_practica_4._1.Dtos;
using Network_Programming_practica_4._1.Entities;
using Network_Programming_practica_4._1.Repository;

namespace Network_Programming_practica_4._1
{

    public class HttpRequest
    {
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? Body { get; set; }
        public static HttpRequest Parse(string raw)
        {

            var lines = raw.Split("\n", StringSplitOptions.RemoveEmptyEntries);

            var firstLine = lines[0].Trim();
            var parts = firstLine.Split(' ');

            var body = lines.Length > 1 ? string.Join('\n', lines.Skip(1)) : "";

            return new HttpRequest
            {
                Method = parts[0].Trim().ToUpper(),
                Path = parts[1].Trim(),
                Body = body.Trim(),
            };
        }
    }


    public static class HttpResponse
    {
        public static string Json(object data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        }
    }

    public static class Router
    {
        public static async Task<string> HandleAsync(HttpRequest request)
        {

            var repo = new ProductRepo();
            if (request.Method == "GET" && request.Path == "/products")
            {
                var products = await repo.GetProducts();
                return HttpResponse.Json(products);
            }
            else if (request.Method == "GET" && request.Path!.StartsWith("/products/"))
            {
                var idText = request.Path.Replace("/products/", " ");
                if (!int.TryParse(idText, out var id))
                {
                    return HttpResponse.Json(new { message = "Invalid product id" });
                }
                var products = await repo.GetProductById(id);
                if (products is null)
                {
                    return HttpResponse.Json(new { message = "IProduct not found" });
                }
                return HttpResponse.Json(products);
            }
            else if (request.Method == "POST" && request.Path == "/products")
            {
                var dto = JsonSerializer.Deserialize<ProductDtos>(request.Body!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto is null || string.IsNullOrWhiteSpace(dto.Name))
                {
                    return HttpResponse.Json(new { message = "Invalid product data.." });
                }
                var product = new Product
                {
                    Name = dto.Name,
                    Price = dto.Price
                };
                repo?.AddProduct(product);
                return HttpResponse.Json(product);
            }
            else if (request.Method == "PUT" && request.Path!.StartsWith("/products/"))
            {
                var Text = request.Path.Replace("/products/", " ");
                var Id_Price = Text.Split("/");
                if (!int.TryParse(Id_Price[0], out var id))
                {
                    return HttpResponse.Json(new { message = "Invalid product id" });
                }
                if (!decimal.TryParse(Id_Price[1], out var price))
                {
                    return HttpResponse.Json(new { message = "Invalid product price" });
                }
                
                var productRep=repo.GetProductById(id);
                var productUp = productRep.Result;
                if (productUp is null)
                {
                    return HttpResponse.Json(new { message = "Invalid product id.." });
                }
                productUp.Price = price;
                repo.UpdateProduct(productUp);

                return HttpResponse.Json(productUp);
            }
            else if (request.Method == "DELETE" && request.Path!.StartsWith("/products/"))
            {
                var idText = request.Path.Replace("/products/", " ");
                if (!int.TryParse(idText, out var id))
                {
                    return HttpResponse.Json(new { message = "Invalid product id" });
                }
                var products = await repo.GetProductById(id);
                if (products is null)
                {
                    return HttpResponse.Json(new { message = "IProduct not found" });
                }
                repo.RemoveProduct(products);
                return HttpResponse.Json(new {message="Product is removed.."} );
            }
            return HttpResponse.Json(null!);
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            ProductRepo repo = new ProductRepo();
            var ip = IPAddress.Parse("192.168.1.82");
            var port = 27001;
            var listener = new TcpListener(ip,port);
            listener.Start();
            Console.WriteLine("Fake API Server Started...");
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();

                _ = Task.Factory.StartNew(() =>
                {

                    HandleClientAsync(client);


                },TaskCreationOptions.LongRunning);
            }

        }

        static async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                await using (var stream = client.GetStream())
                {
                    var buffer=new byte[client.ReceiveBufferSize];
                    var count = await stream.ReadAsync(buffer);
                    var rawRequest=Encoding.UTF8.GetString(buffer,0,count);
                    Console.WriteLine("--------------Request-------------");
                    Console.WriteLine(rawRequest);

                    var request=HttpRequest.Parse(rawRequest);
                    var response=await Router.HandleAsync(request);

                    var bytes=Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(bytes);

                }
            }
        }
    }
}
