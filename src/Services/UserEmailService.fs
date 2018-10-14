namespace Services

open Common.Users
open Common.UserEmails

module UserEmailService =
  type private Service () =
    interface DbCtx IUserEmailService with
      member __.Put ctx (UserId id) email = async {
        use! cmd = ctx.Cmd()
        cmd.CommandText <-
          """ insert into user_emails (
                user_id,
                email
              )
              values (
                :user_id,
                :email
              );
          """
        cmd.Parameters.AddWithValue("user_id", id) |> ignore
        cmd.Parameters.AddWithValue("email", email.Address) |> ignore

        do! cmd.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
      }

  let init () =
    Service()
    :> DbCtx IUserEmailService