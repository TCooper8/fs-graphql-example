module Tests

open System.Net.Mail

open Xunit
open Services
open Platform
open Common.Eventing

let services:Platform.Services =
  { users = UserService.init()
    userEmails = UserEmailService.init()
  }
let events = new Event<ActionEvent>()
let platform = Platform(services, "Host=localhost; Port=5432; Database=postgres; Username=postgres; Password=postgres", events)

[<Fact>]
let ``Test new user`` () = async {
  use subA =
    events.Publish
    |> Observable.subscribe (function
      | RowCreated (_, UsersTable) -> ()
      | RelationCreated (_, UserEmail) -> ()
      | any -> failwithf "Event %A is unexpected" any
    )
  
  do! platform.Init()
  let! userId =
    platform.NewUser
      { name = "bob"
        email = MailAddress "bob@gmail.com"
      }
      [ MailAddress "bob2@gmail.com"
        MailAddress "bob3@gmail.com"
      ]
  printfn "Added user %A" userId
}