namespace Services

open Common
open Common.Users
open Npgsql

module UserService =
  type private Service () =
    interface DbCtx IUserService with
      member __.Get ctx id = async {
        use! cmd = ctx.Cmd()
        return failwithf "Not implemented"
      }

      member __.Post ctx input = async {
        use! cmd = ctx.Cmd()
        cmd.Parameters.AddWithValue ("name", input.name) |> ignore
        cmd.Parameters.AddWithValue ("email", input.email.Address) |> ignore

        cmd.CommandText <-
          """ insert into user_profiles (
                name,
                email
              )
              values (
                :name,
                :email
              )
              returning id;
          """

        let! id = cmd.ExecuteScalarAsync() |> Async.AwaitTask
        let id = id :?> System.Guid
        return UserId id
      }

      member __.Put ctx (UserId id) input = async {
        return failwithf "Not implemented"
      }

      member __.Delete ctx (UserId id) = async {
        return failwithf "Not implemented"
      }

      member __.Patch ctx (UserId id) input = async {
        return failwithf "Not implemented"
      }

  let init () =
    Service()
    :> DbCtx IUserService