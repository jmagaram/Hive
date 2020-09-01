module Hive.Test.Logic.Map

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Logic

[<TestClass>]
type MapTest() = 
    [<TestMethod>]
    member this.Replace()=
        let map = [("a",1);("b",2)] |> Map.ofSeq
        Assert.AreEqual(map |> Map.replace "a" (fun i -> (i |> Option.get) + 1 |> Option.Some) |> Map.find "a", 2)
        Assert.AreEqual(map |> Map.replace "a" (fun i -> None) |> Map.tryFind "a", None)
        Assert.AreEqual(map |> Map.replace "c" (fun i -> 
            Assert.AreEqual(None, i)
            Some 3) |> Map.find "c", 3)
        Assert.AreEqual(map |> Map.replace "c" (fun i -> 
            Assert.AreEqual(None, i)
            None) |> Map.tryFind "c", None)