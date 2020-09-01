namespace Hive.Logic

open System
open System.Threading

type IScoreTree<'Score,'Branch> =
    abstract member Score : 'Score
    abstract member Children : ('Branch * IScoreTree<'Score,'Branch>) seq

module Minimax =
    let score (tree:IScoreTree<_,_>) = tree.Score
    let children (tree:IScoreTree<_,_>) = tree.Children

    let random64 seed =
        let generator =
            match seed with
            | None -> new System.Random()
            | Some seed -> new System.Random(seed)
        fun () ->
            let bytes = Array.zeroCreate<byte> 8
            generator.NextBytes(bytes)
            System.BitConverter.ToInt64(bytes, 0)

    let randomHash64<'T when 'T : equality> seed =
        let rand = random64 seed
        let sigs = new System.Collections.Generic.Dictionary<'T,_>()
        fun i ->
            match sigs.TryGetValue(i) with
            | (true, signature) -> signature
            | (false, _) ->
                let signature = rand()
                sigs.Add(i, signature)
                signature

    let rec solve findMax depth tree =
        let children = tree |> children |> Seq.cache
        let isLeaf = (depth = 0) || (children |> Seq.isEmpty)
        match isLeaf with
        | true -> (None, tree |> score)
        | false -> 
            children
            |> Seq.map (fun (branch, child) -> (Some branch, solve (not findMax) (depth-1) child |> snd))
            |> Seq.reduce (fun best candidate -> 
                let isBetter =
                    match findMax with
                    | true -> (candidate |> snd) > (best |> snd)
                    | false -> (candidate |> snd) < (best |> snd)
                match isBetter with
                    | true -> candidate
                    | false -> best)

    let rec solveAlphaBeta findMax a b depth tree (cancel:CancellationToken option) =
        let children = tree |> children |> Seq.cache
        let isLeaf = (depth = 0) || (children |> Seq.isEmpty)
        match isLeaf with
        | true -> ([], tree |> score)
        | false -> 
            children
            |> Seq.scan (fun ((a, b, prune, variation) as best) (branch, child) ->
                if cancel.IsSome then cancel.Value.ThrowIfCancellationRequested()
                let (childVariation, childScore) = solveAlphaBeta (not findMax) a b (depth-1) child cancel
                let isBetter = variation=[] || (findMax && childScore>a) || (not findMax && childScore<b)
                match isBetter with
                | false -> best
                | true ->
                    let (a, b) = 
                        match findMax with 
                        | true -> (max childScore a, b) 
                        | false -> (a, min childScore b)
                    (a, b, a>=b, branch :: childVariation)) (a, b, false, [])
            |> Seq.tryFindOrLast (fun (_, _, prune, _) -> prune)
            |> Option.get
            |> fun (a, b, _, variation) -> (variation, if findMax then a else b)

    let solveAlphaBetaDeepening findMax a b depth tree timeoutSeconds =
        let rec organize tree variation =
            match variation with
            | [] -> tree
            | head :: tail -> 
                { new IScoreTree<_,_> with
                    member this.Score = tree |> score
                    member this.Children = 
                        seq {
                            let children = tree |> children |> Seq.cache
                            yield children |> Seq.pick (fun (branch, child) -> if branch=head then Some (branch, organize child tail) else None)
                            yield! children |> Seq.filter (fun (branch, child) -> branch<>head)
                        }
                }
        let deepestSolution = ref None
        use tokenSource = 
            match timeoutSeconds with
            | None -> new CancellationTokenSource()
            | Some timeoutSeconds -> new CancellationTokenSource(timeoutSeconds*1000)
        let cancelToken = tokenSource.Token
        try
            [0..depth]
            |> List.iter (fun depth -> 
                let tree =
                    match !deepestSolution with
                    | None -> tree
                    | Some (variation, _) -> organize tree variation
                deepestSolution := Some (solveAlphaBeta findMax a b depth tree (Some cancelToken)))       
        with
            | :? System.OperationCanceledException -> ()
        !deepestSolution |> Option.get