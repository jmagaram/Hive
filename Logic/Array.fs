namespace Hive.Logic

module Array =
    let bits isTrue items =
        items
        |> Array.fold (fun bits n -> 
            match n |> isTrue with
            | false -> bits <<< 1
            | true -> (bits <<< 1) ||| 1) 0

