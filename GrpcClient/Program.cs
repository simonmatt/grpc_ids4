using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GrpcClient
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();

            var disco = await httpClient.GetDiscoveryDocumentAsync("http://localhost:5100");

            if (disco.IsError)
            {
                Console.WriteLine($"Error: {disco.Error}");
            }

            var discoJson = disco.Json;

            Console.WriteLine($"\r\n Discovery Response: {discoJson}");

            var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine($"Error: {tokenResponse.Error}");
            }

            var token = tokenResponse.AccessToken;

            var headers = new Metadata();
            headers.Add("Authorization", "Bearer " + token);

            var callOptions = new CallOptions(headers);

            var channel = GrpcChannel.ForAddress(new Uri("https://localhost:5101"));

            var client = new GrpcService.Greeter.GreeterClient(channel);

            var response = client.SayHello(new GrpcService.HelloRequest { Name = "World" }, callOptions);

            Console.WriteLine(response.Message);

            Console.Read();
        }
    }
}