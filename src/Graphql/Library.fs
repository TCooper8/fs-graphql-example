namespace Graphql

open System
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.Data.GraphQL.Relay

module Resolvers =
  let Id = FSharp.Data.GraphQL.Types.SchemaDefinitions.ID<Guid>

  type Person = {
    id: Guid
    name: string
  }

  