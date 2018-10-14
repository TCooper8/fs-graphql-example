module Tests

open System.Net.Mail

open Xunit
open Services
open Platform
open Common.Eventing
open Common.Users

type 'a GraphqlResp = {
  data: 'a
  errors: obj list
}

type NewUserResp = {
  newUser: string
}

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

[<Fact>]
let ``Test newUse mutation`` () = async {
  let userInfo =
    { id = UserId <| System.Guid.NewGuid()
      name = "bob"
      email = MailAddress "bob@gmail.com"
    }
  let query =
    """ mutation NewUser {
          newUser(name: "bob") {
            name
          }
        }
    """

  let! (data, errors) = Graphql.Resolvers.direct query userInfo
  let json = Encoding.Json.toString data
  let (Choice1Of2 res) = Encoding.Json.tryParse<NewUserResp GraphqlResp> json

  let (UserId id) = userInfo.id
  if res.data.newUser <> string id then
    failwithf "Expected id `%s` but got `%s`" (string id) res.data.newUser
}
