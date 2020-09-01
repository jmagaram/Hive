namespace Hive.Logic

open System.Collections
open System.Collections.Generic

type Visit<'Key> = {
    Node : 'Key
    Ancestors : 'Key list
    Depth : int }

module Graph =
    let cutVertices root neighbors =
        let edges = new HashSet<_>()
        let found = new Dictionary<_,int>()
        let cuts = new List<_>(30)
        let rec visit current depth =
            found.Add(current, depth)
            let (minDepth, maxDepth, children) =
                current
                |> neighbors
                |> Seq.filter (fun neighbor -> neighbor<>current)
                |> Seq.fold (fun (minDepth, maxDepth, children) neighbor ->
                    let edge = if current <= neighbor then (current, neighbor) else (neighbor, current)
                    match edges.Add edge with
                    | false -> (minDepth, maxDepth, children)
                    | true -> 
                        let neighborMin = 
                            match found.TryGetValue(neighbor) with
                            | (false, _) -> visit neighbor (depth+1)
                            | (true, ancestor) -> ancestor
                        (min neighborMin minDepth, max neighborMin maxDepth, children+1)) (depth, -1, 0)
            let cuts = if (depth>1 && maxDepth>=depth) || (depth=1 && children<>1) then cuts.Add(current)
            minDepth
        visit root 1 |> ignore
        cuts :> IEnumerable<_> |> List.ofSeq

    let depthFirst node neighbors =
        let visited = new HashSet<_>()
        let rec traverse investigate =
            match investigate with
            | [] -> None
            | head :: tail -> 
                match visited.Add head.Node with
                | false -> traverse tail
                | true ->
                    let investigate =
                        head.Node
                        |> neighbors
                        |> Seq.fold (fun investigate neighbor -> { Node=neighbor; Depth=head.Depth+1; Ancestors=head.Node :: head.Ancestors } :: investigate) tail 
                    Some (head, investigate)
        let investigate = [{ Node=node; Depth=1; Ancestors=[] }]
        Seq.unfold traverse investigate

    let breadthFirst node neighbors =
        let visited = new HashSet<_>()
        let rec traverse investigate =
            match investigate |> Queue.peek with
            | None -> None
            | Some head -> 
                let tail = investigate |> Queue.dequeue
                match visited.Add head.Node with
                | false -> traverse tail
                | true ->
                    let investigate =
                        head.Node
                        |> neighbors
                        |> Seq.fold (fun investigate neighbor -> investigate |> Queue.enqueue { Node=neighbor; Depth=head.Depth+1; Ancestors=head.Node :: head.Ancestors }) tail 
                    Some (head, investigate)
        let investigate = Queue.create [{ Node=node; Depth=1; Ancestors=[] }]
        Seq.unfold traverse investigate