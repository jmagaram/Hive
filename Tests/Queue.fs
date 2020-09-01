module Hive.Test.Logic.Queue

open System.Collections
open System.Collections.Generic
open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Logic.Queue

[<TestClass>]
type QueueTest() = 
    let dequeueAll queue =
        let extract queue =
            match queue |> peek with
            | None -> None
            | Some item -> Some (item, queue |> dequeue)
        Seq.unfold extract queue |> List.ofSeq

    [<TestMethod>]
    member this.PeekEnqueueDequeue()=
        let q = empty (* [] *) in Assert.AreEqual(None, q.Peek)
        let q = q.Enqueue 1 (* [1] *) in Assert.AreEqual(1, q.Peek.Value)
        let q = q.Enqueue 2 (* [12] *) in Assert.AreEqual(1, q.Peek.Value)
        let q = q.Enqueue 3 (* [123] *) in Assert.AreEqual(1, q.Peek.Value)
        let q = q.Dequeue (* [23] *) in Assert.AreEqual(2, q.Peek.Value)
        let q = q.Dequeue (* [3] *) in Assert.AreEqual(3, q.Peek.Value)
        let q = q.Enqueue 4 (* [34] *) in Assert.AreEqual(3, q.Peek.Value)
        let q = q.Dequeue (* [4] *) in Assert.AreEqual(4, q.Peek.Value)
        let q = q.Dequeue (* [] *) in Assert.AreEqual(None, q.Peek)

    [<TestMethod>]
    member this.Dequeue_IsEmpty_Throw()=
        let isSuccess = 
            try
                let q = empty
                q.Dequeue |> ignore
                false
            with
            | _ as ex -> true
        Assert.IsTrue(isSuccess)

    [<TestMethod>]
    member this.Constructor_Empty()=
        let q = empty 
        Assert.IsTrue(q.Peek.IsNone)
        let q = empty<int>
        Assert.IsTrue(q.Peek.IsNone)
        let q = create<int> []
        Assert.IsTrue(q.Peek.IsNone)

    [<TestMethod>]
    member this.Constructor_OfList()=
        let queue = create [1;2;3;4]
        Assert.IsTrue(queue |> dequeueAll = [1;2;3;4])

    [<TestMethod>]
    member this.Constructor_OfSeq()=
        let queue = create (seq { 1..4 })
        Assert.IsTrue(queue |> dequeueAll = [1;2;3;4])

    [<TestMethod>]
    member this.IEnumerable()=
        // Correct values
        let q = empty (* [] *) |> enqueue 1 |> enqueue 2 |> enqueue 3
        Assert.IsTrue(q :> IEnumerable<int> |> List.ofSeq = [1;2;3])
        let q = q |> dequeue
        Assert.IsTrue(q :> IEnumerable<int> |> List.ofSeq = [2;3])
        let q = q |> dequeue |> dequeue
        Assert.IsTrue(q :> IEnumerable<int> |> List.ofSeq = [])

        // Same instance
        let q = empty (* [] *) |> enqueue 1 |> enqueue 2 |> enqueue 3
        let a = q :> IEnumerable<int> :> obj
        let b = q :> IEnumerable<int> :> obj
        Assert.AreEqual(a,b)
        Assert.AreSame(a,b)
