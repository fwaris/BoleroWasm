namespace BoleroWasm.Client

open Microsoft.AspNetCore.Components.WebAssembly.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open System
open System.Net.Http

module Program =
    [<EntryPoint>]
    let Main args =
        let builder = WebAssemblyHostBuilder.CreateDefault(args)
        builder.RootComponents.Add<Main.MyApp>("#main")
    
        builder.Services.AddScoped<HttpClient>(fun _ ->
            new HttpClient(BaseAddress = Uri builder.HostEnvironment.BaseAddress)
            ) |> ignore

        //http factory to create clients to call Microsoft graph api
        builder.Services.AddScoped<Graph.GraphAPIAuthorizationMessageHandler>() |> ignore
        builder.Services.AddHttpClient(
            Graph.Api.CLIENT_ID, 
            Action<HttpClient>(Graph.Api.configure)) //need type annotation to bind to the correct overload

            .AddHttpMessageHandler<Graph.GraphAPIAuthorizationMessageHandler>()
            |> ignore    

        //add authentication that internally uses the msal.js library
        builder.Services.AddMsalAuthentication(fun o -> 

                //read configuration to reference the AD-app
                builder.Configuration.Bind("AzureAd", o.ProviderOptions.Authentication)

                //Add any access token scopes needed by the WASM app here.
                //The requested scopes have to be first granted in the AD-app.
                //The token scopes will allow the WASM app to access protected APIs 
                o.ProviderOptions.DefaultAccessTokenScopes.Add("User.Read")
                ) |> ignore
        
        builder.Build().RunAsync() |> ignore
        0
