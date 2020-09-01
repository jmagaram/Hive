namespace Hive.Logic

module Seq =
    let groupPair items = 
        items
        |> Seq.groupBy (fun (key, result) -> key)
        |> Seq.map (fun (key, results) -> (key, results |> Seq.map snd))

    // See http://bit.ly/10JDmQ7
    let tryFindOrLast predicate (source:seq<'T>) =
        let rec scan (r: System.Collections.Generic.IEnumerator<'T>) current =
            match r.MoveNext() with
            | false -> current
            | true -> 
                let current = r.Current
                match predicate current with
                | true -> Some current
                | false -> scan r (Some current)
        use r = source.GetEnumerator()
        scan r None

    let existDuplicates items =
        let set = new System.Collections.Generic.HashSet<_>()
        items
        |> Seq.map (fun i -> set.Add(i))
        |> Seq.exists (fun i -> i=false)