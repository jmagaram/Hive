module Hive.Test.Logic.Seq

open System.Collections
open System.Collections.Generic
open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Logic
open Hive.Logic.Seq

[<TestClass>]
type SeqTest() = 
    [<TestMethod>]
    member this.GroupPair()=
        let test = [ ("a",1);("a",2);("b",3);("b",4);("c",5)] |> groupPair |> Map.ofSeq
        Assert.IsTrue(test |> Map.find "a" |> List.ofSeq = [1;2])
        Assert.IsTrue(test |> Map.find "b" |> List.ofSeq = [3;4])
        Assert.IsTrue(test |> Map.find "c" |> List.ofSeq = [5])

    [<TestMethod>]
    member this.ExistDuplicates()=
        Assert.IsFalse([] |> existDuplicates)
        Assert.IsFalse([1] |> existDuplicates)
        Assert.IsFalse([1;2] |> existDuplicates)
        Assert.IsFalse([1;2;3] |> existDuplicates)
        Assert.IsTrue([1;1] |> existDuplicates)
        Assert.IsTrue([2;1;2] |> existDuplicates)
        Assert.IsTrue([1;2;3;4;5;6;7;8;1] |> existDuplicates)
        Assert.IsTrue([1;2;3;4;5;1;2;3;4;5] |> existDuplicates)
       
    [<TestMethod>]
    member this.TryFindOrLast()=
        Assert.IsTrue([1;2;3] |> tryFindOrLast (fun i -> i = 1) |> Option.get = 1)
        Assert.IsTrue([1;2;3] |> tryFindOrLast (fun i -> i = 2) |> Option.get = 2)
        Assert.IsTrue([1;2;3] |> tryFindOrLast (fun i -> i = 3) |> Option.get = 3)
        Assert.IsTrue([1;2;3] |> tryFindOrLast (fun i -> i = 3) |> Option.get = 3)
        Assert.IsTrue([] |> tryFindOrLast (fun i -> i = 3) = None)
        let trap = 
            seq {
                yield 1
                yield 2
                invalidOp "Gone too far!"
                yield 3
            }
        Assert.IsTrue(trap |> tryFindOrLast (fun i -> i >= 2) |> Option.get = 2)
