namespace Platform

open Common
open Common.Try
open Common.Users
open Common.UserEmails
open Common.Eventing
open Services

type Services = {
  users: Ctx IUserService
  userEmails: Ctx IUserEmailService
}

type Platform (services, connStr, events: ActionEvent Event) =
  let x = 5
  let transaction connStr task = async {
    use! ctx = TransactionCtx.Open connStr
    try
      let! res = task ctx
      do! ctx.Commit()
    with e ->
      do! ctx.Rollback()
      raise e
  }

  member __.Init () = async {
    use! conn = ConnCtx.Open connStr
    let ctx = conn :> Ctx
    use! cmd = ctx.Cmd()
    cmd.CommandText <-
      """ create extension if not exists pgcrypto;
          create table if not exists user_profiles (
            id uuid primary key default gen_random_uuid()
          );
          alter table user_profiles
            add column if not exists name text not null unique;
          alter table user_profiles
            add column if not exists email text not null unique;

          create table if not exists user_emails (
            user_id uuid not null references user_profiles(id) on delete cascade,
            email text not null
          );
      """

    do cmd.ExecuteNonQuery() |> ignore
  }

  member __.NewUser userInput emails =
    transaction connStr (fun ctx -> async {
      use! ctx = TransactionCtx.Open connStr
      let! (UserId userId) = services.users.Post ctx userInput
      events.Trigger <| RowCreated (userId, UserTable)
      for email in emails do
        try
          do! services.userEmails.Put ctx (UserId userId) email
          events.Trigger <| RelationCreated (userId, UserEmail)
        with e ->
          return failwithf "Unable to insert %A: %A" email e

      return UserId userId
    })