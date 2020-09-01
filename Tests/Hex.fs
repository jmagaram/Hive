module Hive.Test.Logic.Hex

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Logic
open Hive.Logic.Hex

[<TestClass>]
type HexTest() = 
    let a1 = create 6 7
    let a2 = create 6 7
    let b = create 6 8
    
    [<TestMethod>]
    member this.Equality()=
        Assert.AreEqual(a1,a2)
        Assert.AreNotEqual(a1,b)
        Assert.IsTrue((a1=a2))
        Assert.IsFalse((a1=b))

    [<TestMethod>]
    member this.GetHashCode()=
        Assert.AreEqual(a1.GetHashCode(),a2.GetHashCode())
        Assert.AreNotEqual(a1.GetHashCode(),b.GetHashCode())

    [<TestMethod>]
    member this.IComparable()=
        let a = create 0 0
        let b = create 0 1
        let c = create 1 0
        let d = create 1 1
        Assert.IsTrue(a < b)
        Assert.IsTrue(b < c)
        Assert.IsTrue(c < d)
        Assert.IsTrue(d > c)
        Assert.IsTrue(c > b)
        Assert.IsTrue(b > a)
        Assert.IsTrue(a.CompareTo(a)=0)
        Assert.IsTrue(b.CompareTo(b)=0)
        Assert.IsTrue(a.CompareTo(b)=(-1))
        Assert.IsTrue(b.CompareTo(a)=1)
        let set = [ a; a; b; c; d; d ] |> Set.ofList
        Assert.IsTrue(set.Count=4)
        Assert.IsTrue(set |> List.ofSeq |> List.sort = [a; b; c; d ])

    [<TestMethod>]
    member this.OffsetDirection()=
        let origin = create 0 0
        Assert.AreEqual(north origin, create 0 1)
        Assert.AreEqual(south origin, create 0 (-1))
        Assert.AreEqual(northEast origin, create 1 0)
        Assert.AreEqual(northWest origin, create (-1) 1)
        Assert.AreEqual(southEast origin, create 1 (-1))
        Assert.AreEqual(southWest origin, create (-1) 0)
        Assert.AreEqual(origin, origin |> north |> south)
        Assert.AreEqual(origin, origin |> northEast |> southWest)
        Assert.AreEqual(origin, origin |> northWest |> southEast)

    [<TestMethod>]
    member this.Compass_IsSixInCircularOrder()=
        // In circular order starting north and going 
        let actual = compass |> Seq.map (fun offset -> offset origin) |> List.ofSeq
        let expected =
            [
                north origin;
                northEast origin;
                southEast origin;
                south origin;
                southWest origin;
                northWest origin;
            ]
        Assert.AreEqual(expected,actual)

    [<TestMethod>]
    member this.Origin_IsZeroZero()=
        Assert.IsTrue(origin.X = 0)
        Assert.IsTrue(origin.Y = 0)

    [<TestMethod>]
    member this.Create_AssignsProperCoordinates()=
        let target = create 2 3
        Assert.IsTrue(target.X=2 && target.Y=3)
        let target = create 9 (-4)
        Assert.IsTrue(target.X=9 && target.Y=(-4))

    [<TestMethod>]
    member this.Vector()=
        let actual = vector origin north |> Seq.take 3 |> List.ofSeq
        let expected = [ create 0 0; create 0 1; create 0 2 ]
        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member this.Touches()=
        let h1 = Hex.create 43 21
        Assert.IsTrue(h1.Neighbors |> Array.forall (fun n -> n |> Hex.touches h1 && h1 |> Hex.touches n))
        Assert.IsFalse(h1 |> Hex.touches h1) // doesn't touch if exactly same
        Assert.IsFalse(h1 |> Hex.touches (Hex.create 43 23)) // x same, y diff of 2
        Assert.IsFalse(h1 |> Hex.touches (Hex.create 43 19)) // x same, y diff of 2
        Assert.IsFalse(h1 |> Hex.touches (Hex.create 44 22)) // x bigger, y bigger
        Assert.IsFalse(h1 |> Hex.touches (Hex.create 42 20)) // x smaller, y smaller
        Assert.IsFalse(h1 |> Hex.touches (Hex.create 26 1)) // totally different



