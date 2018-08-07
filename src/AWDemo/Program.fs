module AWDemo.App

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open AWDemo.HttpHandlers
open FSharp.Data.Dapper
open AWDemo.Models

// ---------------------------------
// Web app
// ---------------------------------

let notFound = setStatusCode 404 >=> text "Not Found" 

let chooseAtLeastOne handlers = choose <| List.append handlers [ notFound ]

let webApp =
    chooseAtLeastOne [
        subRoute "/api"
            (choose [
                GET >=> chooseAtLeastOne [
                    route "/hello" >=> handleGetHello
                    route "/products" >=> paginate listProducts
                    route "/customers" >=> paginate listCustomers
                ]
                POST >=> chooseAtLeastOne [
                    route "/products" >=> bindJson<Product> createProduct
                ]
                PATCH >=> chooseAtLeastOne [
                    routef "/orders/%i" (fun id -> bindJson<PatchOrderRequest> (updateOrder id))
                ]
                setStatusCode 405 >=> text "Not supported"
            ])
        ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseCors(configureCors)
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore
    services.AddScoped<Npgsql.NpgsqlConnection> (fun _ ->
        new Npgsql.NpgsqlConnection "Host=localhost;User Id=postgres;Database=AdventureWorks;"
    ) |> ignore
    services.AddTransient<Database> () |> ignore

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    OptionHandler.RegisterTypes()
    WebHostBuilder()
        .UseKestrel()
        .UseUrls("http://localhost:5050")
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Build()
        .Run()
    0