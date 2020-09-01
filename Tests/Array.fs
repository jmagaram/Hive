module Hive.Test.Logic.Array

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Logic

[<TestClass>]
type ArrayTest() = 
    [<TestMethod>]
    member this.Bits()=
        Assert.AreEqual(6, Array.bits (fun i -> i) [| true; true; false |])
        Assert.AreEqual(1, Array.bits (fun i -> i) [| false; false; true |])