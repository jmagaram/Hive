module Hive.Test.Logic.Graph

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Test.Utilities
open Hive.Logic
open Hive.Logic.Graph
open Hive.Test

[<TestClass>]
type GraphTest() = 
    [<TestMethod>]
    member this.CutVertices()=
        let results = 
            Database.queryAll
            |> List.filter (fun testBoard -> testBoard.Title.Contains("cutVertices"))
            |> List.mapi (fun testId testBoard ->
                let board = Database.createBoard testBoard (fun hex -> hex.Color<>"empty") (fun hex -> hex.Color)
                let expected =
                    board
                    |> Board.stacks
                    |> Seq.filter (fun (hex, stack) -> stack |> Seq.head = "white")
                    |> Seq.map (fun (hex, stack) -> hex)
                    |> Set.ofSeq
                let roots =
                    board
                    |> Board.stacks
                    |> Seq.map (fun (hex, stack) -> hex)
                let actuals =
                    roots
                    |> Seq.map (fun root -> 
                        let neighbors source = 
                            source
                            |> Hex.neighbors
                            |> Seq.filter (fun target -> board |> Board.isOccupied target)
                        let actualCuts = cutVertices root neighbors |> Set.ofSeq
                        let isCorrect = (actualCuts = expected)
                        (isCorrect, root, actualCuts))
                    |> List.ofSeq
                let isCorrect = actuals |> List.forall (fun (isCorrect, _, _) -> isCorrect)
                {
                    TestId = testId;
                    Expected = expected;
                    Actual = actuals;
                    IsCorrect = isCorrect;
                })
        Assert.IsTrue(results |> List.forall (fun r -> r.IsCorrect))

    [<TestMethod>]
    member this.BreadthFirst()=
        let results = 
            Database.queryAll
            |> List.filter (fun testBoard -> testBoard.Title.Contains("bfs"))
            |> List.mapi (fun testId testBoard ->
                let board = Database.createBoard testBoard (fun hex -> hex.Color<>"empty") (fun hex -> hex.Tag |> int)
                let root = 
                    board
                    |> Board.stacks
                    |> Seq.find (fun (hex, stack) -> stack |> Seq.head = 1)
                    |> fun (hex, stack) -> hex
                let neighbors source =
                    source
                    |> Hex.neighbors
                    |> Seq.filter (fun neighbor -> board |> Board.isOccupied neighbor)
                let bfs = breadthFirst root neighbors |> List.ofSeq
                let actualOrder =
                    bfs
                    |> List.map (fun v -> (v.Depth, v.Node))
                    |> List.sort
                let expectedOrder =
                    board
                    |> Board.stacks
                    |> Seq.map (fun (hex, stack) -> (stack |> Seq.head, hex))
                    |> Seq.sort
                    |> List.ofSeq
                let isCorrectOrder = (actualOrder = expectedOrder)
                let isAncestorsCorrect =
                    bfs
                    |> Seq.forall (fun v -> 
                        (v.Node :: v.Ancestors)
                        |> List.map (fun h -> board |> Board.find h |> List.head)
                        |> List.rev
                        |> (=) [1..v.Depth])
                {
                    TestId = testId;
                    Expected = expectedOrder;
                    Actual = bfs;
                    IsCorrect = isCorrectOrder && isAncestorsCorrect
                })
            |> List.ofSeq
        Assert.IsTrue(results |> List.forall (fun r -> r.IsCorrect))

    [<TestMethod>]
    member this.DepthFirst()=
        let results = 
            Database.queryAll
            |> List.filter (fun testBoard -> testBoard.Title.Contains("dfs"))
            |> List.mapi (fun testId testBoard ->
                let board = Database.createBoard testBoard (fun hex -> hex.Color<>"empty") (fun hex -> if System.String.IsNullOrWhiteSpace(hex.Tag) then 0 else hex.Tag |> int32)
                let root =
                    board
                    |> Board.stacks
                    |> Seq.pick (fun (hex, stack) -> if stack |> Seq.head = 1 then Some hex else None)
                let neighbors source =
                    source
                    |> Hex.neighbors
                    |> Seq.filter (fun neighbor -> board |> Board.isOccupied neighbor)
                let dfs = depthFirst root neighbors |> List.ofSeq
                let actualOrder =
                    dfs
                    |> List.mapi (fun index v -> (index+1, v.Node))
                    |> List.sort
                let expectedOrder =
                    board
                    |> Board.stacks
                    |> Seq.map (fun (hex, stack) -> (stack |> Seq.head, hex))
                    |> Seq.filter (fun (order, hex) -> order <> 0)
                    |> Seq.sort
                    |> List.ofSeq
                let isCorrectOrder = (actualOrder = expectedOrder)
                {
                    TestId = testId;
                    Expected = expectedOrder;
                    Actual = actualOrder;
                    IsCorrect = isCorrectOrder
                })
            |> List.ofSeq
        Assert.IsTrue(results |> List.forall (fun r -> r.IsCorrect))