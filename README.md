## .NET Core gRPC with Identity Server 4

一个简单的基于gRPC的认证授权demo。

|程序|类型|说明|
|---|---|---|
Ids4.Server|Web API|基于OpenID Connect + OAuth2.0认证授权服务
GrpcService|gRPC服务程序|gRPC服务端
GrpcClient|控制台程序|gRPC客户端

#### Ids4.Server

1. 创建一个 ASP.NET Core Web API程序，命名为`Ids4.Server`
2. 引入`IdentityServer4`程序包

```shell
> Install-Package IdentityServer4
```
3. `IdentityServer`配置，添加新类`Config.cs`并增加以下测试代码
   
```csharp
using IdentityServer4.Models;
using System.Collections.Generic;

namespace Ids4.Server
{
    public class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email()
            };
        }

        public static IEnumerable<ApiResource> GetApis()
        {
            return new List<ApiResource>
            {
                new ApiResource("api","demo api")
                {
                    ApiSecrets={new Secret("secret".Sha256()) }
                }
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    ClientId="client",
                    ClientSecrets={new Secret("secret".Sha256())},
                    AllowedGrantTypes=GrantTypes.ClientCredentials,
                    AllowedScopes={"api"}
                }
            };
        }
    }
}
```
4. 修改`Startup.cs`，注入相关依赖和配置中间件

```csharp
# ConfigureServices
ervices.AddIdentityServer()
                .AddDeveloperSigningCredential(persistKey: false)
                .AddInMemoryApiResources(Config.GetApis())
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryClients(Config.GetClients());

# Configure method
app.UseRouting();

            app.UseIdentityServer();
            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });        
```

5. 启动并测试，这里使用`Postman`

```http
# Request
POST /connect/token HTTP/1.1
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=client&client_secret=secret

# Response
{
    "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IklOU0ZNcHJnay0yQXIxbkpwNGJfNHciLCJ0eXAiOiJhdCtqd3QifQ.eyJuYmYiOjE1OTE1OTQzMTIsImV4cCI6MTU5MTU5NzkxMiwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MTAwIiwiYXVkIjoiYXBpIiwiY2xpZW50X2lkIjoiY2xpZW50Iiwic2NvcGUiOlsiYXBpIl19.dCcfcx8KScLNwstd_Jn5xWM_O5DgFVwmPohdftTE9cOnH-Ny39dg30qI_RF32Lz9vLtMzBgY_sHdwtPQtqOek1Tu_35IcfPJg6wGxaqtwy0r0Jr0up9JhVptxA-5cTbjDOawmfCxorYQJSlHptACsN9xTx5lua14ObxqnrStM_rkkNBJwLmrhLCY2S-CcugLTXnjVKfr4An4NE4XDL-rpPDJKGp04lXXta5ESmVUagI2cFop30zCFVzqLvL1rXAJP3ocGeAW1lLNbHdtxOlzzAzW5XdKTBaFF1tPEhUkVBp7nVO6WYD6fRruVdSA5EWsM06MDF9IADddChTuu20N2g",
    "expires_in": 3600,
    "token_type": "Bearer",
    "scope": "api"
}
```

#### GrpcService

使用VS2019创建gRPC服务，使用默认设置即可

#### GrpcClient

1. 创建一个.NET Core控制台程序，命名为`GrpcClient`
2. 引入相关程序包

```shell
> Install-Package Google.Protobuf
> Install-Package Grpc.Net.Client
> Install-Package Grpc.Tools
```
或者直接编辑`GrpcClient.csproj`文件，添加以下配置也可

```xml
# GrpcClient.csproj
# Grpc.Tools包的作用是将.proto文件转成C#文件
<PackageReference Include="google.protobuf" Version="3.12.3" />
<PackageReference Include="grpc.net.client" Version="2.29.0" />
<PackageReference Include="Grpc.Tools" Version="2.29.0">
<PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```
3. 在`GrpcClient`程序根目录下创建新文件夹`Protos`，复制`GrpcService`项目中`Protos\greet.proto`到该目录中
4. 右键单击`GrpcClient`中的`greet.proto`文件，设置其属性`gRPC Stub Classes`为`Client only`或者直接编辑`GrpcClient.csproj`，加入以下配置也可

```xml
# GrpcClient.csproj
	<ItemGroup>
		<Protobuf Include="Protos\greet.proto" GrpcServices="Client" />
	</ItemGroup>
```

5. 在`Program.cs`中增加以下代码访问服务端

```csharp
# Program.cs

var channel = GrpcChannel.ForAddress(new Uri("https://localhost:5101"));

var client = new GrpcService.Greeter.GreeterClient(channel);

var response = client.SayHello(new GrpcService.HelloRequest { Name = "World" });

Console.WriteLine(response.Message);

Console.Read();
```
6. 运行服务端`GrpcService`和客户端`GrpcClient`

```shell
# GrpcService 
$ dotnet run

# GrpcClient
$ dotnet run
```

#### 在GrpcService中集成IdentityServer4

1. 引入NuGet程序包

```shell
> Install-Package IdentityServer4.AccessTokenValidation
```

2. 修改`Startup.cs`

```csharp
public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options => options.EnableDetailedErrors = true);
            services.AddAuthorization();
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = "http://localhost:5100";
                    options.RequireHttpsMetadata = false;
                });
        }
```
3. 配置中间件

```csharp
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // 增加下面两行
            app.UseAuthentication(); 
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GreeterService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
```

4. 配置需要进行授权的服务或方法

```csharp
    [Authorize]
    public class GreeterService : Greeter.GreeterBase
    {
        ...
    }
```

5. 重新启动`GrpcService`和`GrpcClient`，这时发现返回`401`，表示`Unauthorized`，需要在请求时携带`accesstoken`
6. 改造`GrpcClient`，在`Program.cs`中增加以下代码

```csharp
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
```

7. 依次运行`Ids4.Server`，`GrpcService`，`GrpcClient`