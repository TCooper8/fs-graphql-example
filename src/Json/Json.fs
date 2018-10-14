namespace Encoding

open Newtonsoft.Json

module Json =

  type OptionConverter() =
    inherit JsonConverter()
    override x.CanConvert(t) =
       t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>
    override x.WriteJson(writer, value, serializer) =
       let value =
          if value = null then null
          else
            let _,fields = Microsoft.FSharp.Reflection.FSharpValue.GetUnionFields(value, value.GetType())
            fields.[0]
       serializer.Serialize(writer, value)
    override x.ReadJson(reader, t, existingValue, serializer) = failwith "Not supported"

  let settings = JsonSerializerSettings()
  settings.Converters <- [| OptionConverter() :> JsonConverter |]
  settings.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
  let json o = JsonConvert.SerializeObject(o, settings)

  let tryParse<'a> (input:string) =
    try JsonConvert.DeserializeObject<'a> input |> Choice1Of2
    with e -> Choice2Of2 e

  let toString value =
    JsonConvert.SerializeObject value
