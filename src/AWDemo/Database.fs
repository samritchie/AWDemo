namespace AWDemo

open AWDemo.Models
open System.Threading.Tasks
open Npgsql
open FSharp.Data.Dapper

type Database (connection : NpgsqlConnection) =
  let connectionF () = Connection.SqlServerConnection connection
  let pageSize = 50

  member private __.GetPaginatedList<'T> (sql : string) (page: int) =
    (querySeqAsync<'T> connectionF) {
      parameters (dict 
        [ 
          ("limit", box pageSize)
          ("offset", box <| (page - 1) * pageSize)
        ])
      script (sql + " limit @limit offset @offset")
    } |> Async.StartAsTask

  member this.GetProducts (page : int) =
    this.GetPaginatedList<Product> "select * from production.product order by productid" page

  member this.GetCustomers (page : int) =
    this.GetPaginatedList<Customer> "select *, BusinessEntityId as CustomerId from person.person p inner join sales.customer c on c.personId = p.businessEntityId order by BusinessEntityId" page

  member this.GetOrder (orderId : int) : Task<Order option> = Task.FromResult None