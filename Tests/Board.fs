module Hive.Test.Logic.Board

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Test.Utilities
open Hive.Logic
open Hive.Logic.Hex
open Hive.Logic.Board

[<TestClass>]
type BoardTest() = 
    let hex1 = create 1 1
    let hex2 = create 2 2
    let hex3 = create 3 3
    let top hex board = board |> tryFind hex |> Option.get |> Seq.head 

    [<TestMethod>]
    member this.Constructor_Empty()=
        Assert.IsTrue(empty.Stacks |> Seq.isEmpty)

    [<TestMethod>]
    member this.Put()=
        Assert.IsTrue(empty |> put hex1 'a' |> top hex1 = 'a') // Put in totally empty
        Assert.IsTrue(empty |> put hex1 'a' |> put hex2 'b' |> top hex2 = 'b') // Put in empty position
        Assert.IsTrue(empty |> put hex1 'a' |> put hex1 'b' |> put hex1 'c' |> top hex1 = 'c') // Put on top of filled position

    [<TestMethod>]
    member this.PieceCount()=
        Assert.AreEqual(0, empty |> pieceCount)
        Assert.AreEqual(1, empty |> put hex1 'a' |> pieceCount)
        Assert.AreEqual(2, empty |> put hex1 'a' |> put hex2 'b' |> pieceCount)
        Assert.AreEqual(2, empty |> put hex1 'a' |> put hex2 'b' |> move hex1 hex2 |> pieceCount)
  
    [<TestMethod>]
    member this.Move()=
        // From position with 1 to empty position
        let b = empty |> put hex1 'a' |> move hex1 hex2
        Assert.IsTrue(b |> tryFind hex1 |> Option.isNone)
        Assert.IsTrue(b |> top hex2 = 'a')

        // From position with 2 to empty position
        let b = empty |> put hex1 'a' |> put hex1 'b' |> move hex1 hex2
        Assert.IsTrue(b |> top hex1 = 'a')
        Assert.IsTrue(b |> top hex2 = 'b')

        // From position with 2 to position with 1
        let b = empty |> put hex1 'a' |> put hex1 'b' |> put hex2 'c' |> move hex1 hex2
        Assert.IsTrue(b |> top hex1 = 'a')
        Assert.IsTrue(b |> top hex2 = 'b')

    [<TestMethod>]
    member this.Move_FromEmpty_Throws()=
        let result = throwsAny (fun () -> empty |> put hex1 'a' |> move hex2 hex3 |> ignore)
        Assert.IsTrue(result)

    [<TestMethod>]
    member this.Stacks()=
        let s = empty
        Assert.IsTrue(s.Stacks |> Seq.isEmpty)

        let s = 
            empty
            |> put hex1 'a' // 1a
            |> put hex2 'b' // 1a 2b
            |> put hex3 'c' // 1a 2b 3c
            |> move hex3 hex1 // 1ca 2b
            |> move hex2 hex3 // 1ca 3b
        
        let actual = s.Stacks |> Seq.map (fun (position, stack) -> (position,stack |> List.ofSeq)) |> Set.ofSeq
        let expected = [(hex1,['c';'a']);(hex3,['b'])] |> Set.ofSeq
        Assert.AreEqual(expected, actual)

    [<TestMethod>]
    member this.TryFind()=
        let board = empty |> put hex1 'a' |> put hex2 'b'
        Assert.IsTrue(board |> tryFind hex1 |> Option.get |> Seq.head = 'a')
        Assert.IsTrue(board |> tryFind hex2 |> Option.get |> Seq.head = 'b')
        Assert.IsTrue(board |> tryFind hex3 |> Option.isNone)

    [<TestMethod>]
    member this.Pieces()=
        let board = 
            empty
            |> put hex1 'a'
            |> put hex1 'b'
            |> put hex2 'c'
        let result = board |> pieces |> List.ofSeq |> List.sort
        let expected = [(hex1,'a'); (hex1,'b'); (hex2,'c')] |> List.sort
        Assert.AreEqual(expected, result)

    [<TestMethod>]
    member this.PiecesZ()=
        Assert.IsTrue(empty |> piecesZ |> Seq.isEmpty)

        let actual = empty |> put hex1 'a' |> put hex1 'b' |> piecesZ |> List.ofSeq
        let expected = [
            { Hex = hex1; Piece='a'; Z=0 }; // Bottom Z is 0
            { Hex = hex1; Piece='b'; Z=1 }; ]
        Assert.IsTrue((actual=expected))

    [<TestMethod>]
    member this.Freedom()=
        let occupied s = s |> Seq.map (fun c -> c='x') |> Array.ofSeq
        let escapable s = 
            s 
            |> Seq.mapi (fun i c -> (i, c=' ')) 
            |> Seq.choose (fun (i, isEscapable) -> if isEscapable then Some i else None) 
            |> Array.ofSeq
        let results = 
            [ "  xxxx"; "......"; "xxxxxx"; "x.x.x."; "x . x."; "xxx . "; "xxxx  " ]
            |> List.mapi (fun testIndex test ->
                assert (test.Length=6)
                let actualResult = 
                    test 
                    |> occupied
                    |> Array.bits (fun i -> i)
                    |> fun bits -> Board.freedom.[bits]
                    |> Set.ofList
                let expectedResult = 
                    test 
                    |> escapable
                    |> Set.ofArray
                let isCorrect = actualResult = expectedResult
                { 
                    IsCorrect = isCorrect;
                    TestId = testIndex;
                    Expected = expectedResult;
                    Actual = actualResult
                })
        Assert.IsTrue(results |> List.forall (fun i -> i.IsCorrect))