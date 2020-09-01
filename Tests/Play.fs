module Hive.Test.Play

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Test.Utilities
open Hive.Test.Database
open Hive.Play

[<TestClass>]
type PlayTest() = 
    [<TestMethod>]
    member this.GameStuff()=
        let results = (HiveGame.New.Place Bug.Bee ({ X=0; Y=0 })).Place Bug.Spider ({ X=0; Y=1 })
        let a = results.Stacks |> Seq.length
        Assert.IsTrue(a>0)

    [<TestMethod>]
    member this.HexEquality()=
        let a1 = { X=0; Y=1 }
        let a2 = { X=0; Y=1 }
        let b = { X=0; Y=2 }
        Assert.AreEqual(a1,a2)
        Assert.AreNotEqual(a1,b)

