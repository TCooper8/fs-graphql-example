namespace Graphql

open System
open System.Net.Mail
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Data.GraphQL.Relay
open Common.Users

module Resolvers =
  let Id = FSharp.Data.GraphQL.Types.SchemaDefinitions.ID<Guid>

  let UserDef =
    Define.Object<UserInfo>(
      name = "User",
      fields =
        [ Define.AutoField("name", String)
        ]
    )

  let Query: UserInfo ObjectDef =
    Define.Object(
      "Query",
      [ Define.Field(
          "user",
          UserDef,
          "Looks up a user",
          [
          ],
          fun ctx info ->
            info
        )
      ]
    )

  let Mutation: UserInfo ObjectDef =
    Define.Object(
      "Mutation",
      [ Define.Field(
          "newUser",
          Id,
          "Creates a new user",
          [ Define.Input("name", String)
          ],
          fun ctx info ->
            System.Guid.NewGuid()
        )
      ]
    )

  let schema =
    Schema(
      query = Query,
      mutation = Mutation
    )
  let exec = Executor schema

