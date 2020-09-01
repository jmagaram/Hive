namespace Hive.Logic

module Map =
    let replace key convert map =
        match map |> Map.tryFind key |> convert with
        | Some value -> map |> Map.add key value
        | None -> map |> Map.remove key
