namespace AWDemo.Models

open System.Threading.Tasks
[<CLIMutable>]
type Message =
    {
        Text : string
    }

[<CLIMutable>]
type Product = 
    {
        ProductId : int
        Name : string
        ProductNumber : string
        Color : string option 
    }

 [<CLIMutable>]
 type Customer =
    {
        CustomerId : int
        FirstName : string
        LastName : string
    }


type OrderStatus = Pending | Processing | Completed | Cancelled

[<CLIMutable>]
type Order =
    {
        CustomerId : int
        ProductId : int
        Quantity : int
        Status : OrderStatus
        DateCreated : System.DateTime
        DateCompleted : System.DateTime option
    }

[<CLIMutable>]
type PatchOrderRequest = 
    {
        UpdateQuantity : int
    }

type UserRole = Admin | Customer of int

type User =
    {
        Email : string
        Role : UserRole
    }