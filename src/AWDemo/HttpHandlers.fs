namespace AWDemo

open Microsoft.AspNetCore.Http
module Int32 =
    open System
    let tryParse (str: string) =
        match Int32.TryParse str with
        | (true, v) -> Some v
        | (false, _) -> None



module HttpHandlers =

    open Microsoft.AspNetCore.Http
    open Giraffe
    open AWDemo.Models

    type HttpContext with
        member this.PageQueryParam =
            this.TryGetQueryStringValue "page" 
                    |> Option.bind Int32.tryParse 
                    |> Option.defaultValue 1

        member __.GetUser: User option = None

    let paginate (handler : int -> HttpHandler) (next : HttpFunc) (ctx : HttpContext) =
        task {
            let page = ctx.PageQueryParam
            return! handler page next ctx
        }

    let handleGetHello (next : HttpFunc) (ctx : HttpContext) =
        task {
            let response = {
                Text = "Hello world, from Giraffe!"
            }
            return! json response next ctx
        }

    let listProducts (page : int) (next : HttpFunc) (ctx : HttpContext) =
        task {
            let db = ctx.GetService<Database> ()
            let! products = db.GetProducts page
            return! json products next ctx
        }

    let listCustomers (page : int) (next : HttpFunc) (ctx : HttpContext) =
        task {
            let db = ctx.GetService<Database> ()
            let! customers = db.GetCustomers page
            return! json customers next ctx
        }

    let createProduct (product : Product) : HttpHandler =
        setStatusCode 201 >=> text "Created" 

    let updateOrder (id : int) (req : PatchOrderRequest) (next : HttpFunc) (ctx : HttpContext) =
        task {
            let db = ctx.GetService<Database> ()
            let user = ctx.GetUser
            let! order = db.GetOrder id
            match (req, user, order) with 
            | (_, _, None) -> 
                return! setStatusCode 404 next ctx
            | (_, None, _) -> 
                return! setStatusCode 401 next ctx
            | (_, Some { Role = Customer i }, Some o) when i <> o.CustomerId ->
                return! setStatusCode 403 next ctx
            | (_, _, Some { Status = Cancelled }) | (_, _, Some { Status = Completed }) ->
                return! setStatusCode 400 next ctx
            | ({ UpdateQuantity = q}, _, Some o) when q < o.Quantity -> 
                return! setStatusCode 400 next ctx 
            | ({ UpdateQuantity = q}, _, Some o) ->
                // Do DB update
                return! (setStatusCode 200 >=> json { o with Quantity = q }) next ctx
        }