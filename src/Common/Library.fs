namespace Common

type Id = System.Guid

type RelationType =
  | UserEmail

type TableType =
  | UserTable

module Eventing =
  type ActionEvent =
    | RowCreated of Id * TableType
    | RelationCreated of Id * RelationType

module Try =
  type 'a Try =
    | Success of 'a
    | Failure of exn

  module Async =
    let Try task = async {
      let! res = task |> Async.Catch
      return
        match res with
        | Choice1Of2 v -> Success v
        | Choice2Of2 e -> Failure e
    }

module Control =
  type TransactionBuilder() =
    member __.Bind (comp, binder) = async {
      let! x = comp
      return! binder x
    }

    member __.For (col:seq<_>, func) = async {
      for c in col do
        do! func c
    }

    member __.Zero () = async { return () }

  let transaction = TransactionBuilder()

module Users =
  open Control
  open System.Net.Mail

  type UserId = | UserId of Id
  type UserInfo = {
    id: UserId
    name: string
    email: MailAddress
  }

  type UserInput = {
    name: string
    email: MailAddress
  }

  [<Interface>]
  type 'ctx IUserService =
    abstract Get: 'ctx -> UserId -> UserInfo Async
    abstract Post: 'ctx -> UserInput -> UserId Async
    abstract Put: 'ctx -> UserId -> UserInput -> unit Async
    abstract Delete: 'ctx -> UserId -> unit Async
    abstract Patch: 'ctx -> UserId -> UserInput -> unit Async

module UserEmails =
  open System.Net.Mail
  open Users

  [<Interface>]
  type 'ctx IUserEmailService =
    abstract Put: 'ctx -> UserId -> MailAddress -> unit Async

module internal Example =
  open Control
  open Users
  open UserEmails

  type Ctx = {
    log: string -> unit
    createCmd: string -> (unit -> unit)
  }

  type Services = {
    users: Ctx IUserService
    userEmails: Ctx IUserEmailService
  }

  let newUser services userInput emailInputs =
    let ctx =
      { createCmd = fun id ->
          fun () ->
            printfn "Closing cmd %s" id
        log = fun msg -> printfn "%s" msg
      }

    transaction {
      let! userId = services.users.Post ctx userInput
      for emailInput in emailInputs do
        do! services.userEmails.Put ctx userId emailInput
    }