namespace Platform

open Common
open Common.Try
open Common.Users
open Common.UserEmails
open Common.Eventing
open Services

type Services = {
  users: DbCtx IUserService
  userEmails: DbCtx IUserEmailService
}

type Platform (services, connStr, events: ActionEvent Event) =
  let x = 5
  let transaction connStr task = async {
    use! ctx = TransactionDbCtx.Open connStr
    try
      let! res = task ctx
      do! ctx.Commit()
    with e ->
      do! ctx.Rollback()
      raise e
  }

  member __.Init () = async {
    use! conn = ConnDbCtx.Open connStr
    let ctx = conn :> DbCtx
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

  /// Will create a new user.
  /// Note: Creating the user's emails could be done within the UserService, but I have split these two in order to
  ///   demonstrate the simplicity of implementing this kind of pattern in F#.
  ///   Because it isn't tied to the UserService, the user emails storage could be pulled out into a different service,
  ///   but the integrity of the data on insert would still be maintained.
  member __.NewUser userInput emails =
    transaction connStr (fun ctx -> async {
      use! ctx = TransactionDbCtx.Open connStr
      // Create the user.
      let! (UserId userId) = services.users.Post ctx userInput
      do events.Trigger <| RowCreated (userId, UserTable)

      // This is part of the transaction just to keep the data strictly in-sync.
      // This call could be connected to a different service or database if needed.
      // Also, the transaction piece could be removed -- Allowing for better throughput, but poor data integrity.
      for email in emails do
        try
          do! services.userEmails.Put ctx (UserId userId) email
        with e ->
          // Sample for a retry within a transaction.
          do! services.userEmails.Put ctx (UserId userId) email

        // This will fail if any subscribers are unable to consume the event.
        // Due to the transaction call above, it will rollback the database if it cannot publish this event.
        do events.Trigger <| RelationCreated (userId, UserEmail)

      return UserId userId
    })