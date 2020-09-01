namespace Hive.Logic

open System.Collections
open System.Collections.Generic

type IQueue<'T> = 
    inherit IEnumerable<'T>
    abstract member Peek : 'T option
    abstract member Enqueue : 'T -> IQueue<'T>
    abstract member Dequeue : IQueue<'T>

module Queue =
    let create<'T> items =
        let rec build backward forward =
            let (backward, forward) = // if there is anything in queue then forward will not be empty
                match forward with
                | [] -> ([], backward |> List.rev)
                | _ -> (backward, forward)
            let items = seq { yield! forward; yield! backward |> List.rev }
            { 
                new IQueue<'T> with
                    member q.Peek =
                        match forward with
                        | [] -> None
                        | head::tail -> Some head
                    member q.Enqueue x = build (x::backward) forward
                    member q.Dequeue =
                        match forward with
                        | [] -> invalidOp "Can't dequeue an empty queue"
                        | head::tail -> build backward tail
                interface IEnumerable<_> with member this.GetEnumerator() = items.GetEnumerator()
                interface IEnumerable with member this.GetEnumerator () = (items :> IEnumerable).GetEnumerator() 
            }
        build [] (items |> Seq.toList)
    let empty<'T> = create List.empty<'T>
    let peek (q:IQueue<'T>) = q.Peek
    let enqueue item (q:IQueue<'T>) = q.Enqueue item
    let dequeue (q:IQueue<'T>) = q.Dequeue
    let toSeq (q:IQueue<'T>) = q :> IEnumerable<'T>