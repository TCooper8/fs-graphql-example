open System

open System.Net.Mail
open Common.Users

[<EntryPoint>]
let main argv =
  let userInfo =
    { id = UserId <| System.Guid.NewGuid()
      name = "bob"
      email = MailAddress "bob@gmail.com"
    }
  printf "Query >>> "
  let line = Console.ReadLine()
  let reply = Graphql.Resolvers.exec.AsyncExecute(queryOrMutation = line, data = userInfo) |> Async.RunSynchronously
  printfn "%A" reply
  0
