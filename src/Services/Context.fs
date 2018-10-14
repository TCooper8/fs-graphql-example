namespace Services

open System
open Npgsql
open Common.Eventing

[<Interface>]
type DbCtx =
  abstract Cmd: unit -> NpgsqlCommand Async

type ConnDbCtx (conn:NpgsqlConnection) =
  interface IDisposable with
    member __.Dispose () =
      use conn = conn
      ()

  interface DbCtx with
    member __.Cmd () = async {
      return conn.CreateCommand()
    }

  static member Open connStr = async {
    let conn = new NpgsqlConnection(connStr)
    do! conn.OpenAsync() |> Async.AwaitTask
    return new ConnDbCtx(conn)
  }

type TransactionDbCtx (conn:NpgsqlConnection, tx:NpgsqlTransaction) =
  let exn = ref None

  interface IDisposable with
    member __.Dispose () =
      use conn = conn
      use tx = tx
      match !exn with
      | None -> ()
      | Some e -> raise e

  interface DbCtx with
    member __.Cmd () = async {
      let cmd = conn.CreateCommand()
      cmd.Transaction <- tx
      return cmd
    }

  member __.Commit () = async {
    return! tx.CommitAsync() |> Async.AwaitTask
  }

  member __.Rollback () = async {
    return! tx.RollbackAsync() |> Async.AwaitTask
  }

  static member Open connStr = async {
    let conn = new NpgsqlConnection(connStr)
    do! conn.OpenAsync() |> Async.AwaitTask
    let tx = conn.BeginTransaction()
    return new TransactionDbCtx(conn, tx)
  }