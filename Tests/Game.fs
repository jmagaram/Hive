module Hive.Test.Logic.Game

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Test.Utilities
open Hive.Test
open Hive.Logic

// Assumption is that in the test data the single white piece moves, and valid destinations 
// are marked as empty.
let testValidMoves title moveCalculator = 
    Database.queryAll
    |> List.filter (fun testBoard -> testBoard.Title = title)
    |> List.mapi (fun testId testBoard ->
        let occupied = Database.createBoard testBoard (fun h -> h.Color <> "empty") (fun h -> h.Color)
        let bugLocation = 
            occupied
            |> Board.stacks
            |> Seq.find (fun (hex, stack) -> stack |> Seq.head = "white")
            |> fun (hex, stack) -> hex
        let expected = 
            Database.createBoard testBoard (fun h -> h.Color = "empty") (fun h -> true)
            |> Board.stacks
            |> Seq.map (fun (hex, stack) -> hex)
            |> Set.ofSeq
        let actual = moveCalculator bugLocation occupied |> List.ofSeq
        let isCorrectNumberOfResults = actual.Length = expected.Count
        let isCorrectResults = (actual |> Set.ofList) = expected
        let isCorrect = isCorrectNumberOfResults && isCorrectResults
        { 
            IsCorrect = isCorrect; 
            TestId = testId; 
            Expected = expected;
            Actual = actual;
        })

[<TestClass>]
type GameTest() = 
    [<TestMethod>]
    [<TestCategory("Slow")>]
    member this.Hash64_IterativeHashIsSameAsCalcFromScratch() =
        let hash64 game =
            game
            |> Game.board
            |> Board.piecesZ
            |> Seq.fold (fun hash piece -> hash ^^^ (Game.hashes (GameHash.Tile piece))) 0L
            |> fun tilesHash -> tilesHash ^^^ (Game.hashes (GameHash.State game.GameState))
        let seed = 33
        let random = new System.Random(seed)
        let newGame = Game.start White
        let maxTurnsPerGame = 100
        let generator (turnsLeft, game) =
            match turnsLeft with
            | 0 -> Some (newGame, (maxTurnsPerGame, newGame))
            | _ ->
                match game |> Game.gameState with
                | Tie | Victor _ -> Some (newGame, (maxTurnsPerGame, newGame))
                | NextTurnBy player ->
                    let legal = game |> Game.turns player |> Array.ofSeq
                    let turn =
                        match legal.Length with
                        | 0 -> Skip
                        | legalCount -> legal.[random.Next(0, legalCount)]
                    let game = game |> Game.takeTurn turn
                    Some (game, (turnsLeft-1, game))
        let iterations = 50000
        Assert.IsTrue(
            Seq.unfold generator (maxTurnsPerGame, newGame) 
            |> Seq.take iterations
            |> Seq.forall (fun game -> 
                let iterative = game.Hash64
                let calculateFromScratch = game |> hash64
                iterative = calculateFromScratch))

    [<TestMethod>]
    member this.Hashes_AreUnique()=
        let hashes = Minimax.randomHash64<int> (Some Game.hashesRandomSeed)
        let colors = 2
        let bugs = 5
        let boardWidth = 100
        let boardHeight = 100
        let zRange = 4
        let permutations = colors * bugs * boardWidth * boardHeight * zRange
        Assert.IsTrue(
            { 1..permutations }
            |> Seq.map (fun i -> hashes i)
            |> Seq.existDuplicates
            |> not)

    [<TestMethod>]
    member this.CoreBugs()=
        let map = Game.coreBugs |> Map.ofSeq
        Assert.IsTrue(map |> Map.find Bug.Bee = 1)
        Assert.IsTrue(map |> Map.find Bug.Spider = 2)
        Assert.IsTrue(map |> Map.find Bug.Ant = 3)
        Assert.IsTrue(map |> Map.find Bug.Grasshopper = 3)
        Assert.IsTrue(map |> Map.find Bug.Beetle = 2)

    [<TestMethod>]
    member this.Ant()=
        let results = testValidMoves "ant" Game.ant
        Assert.IsTrue(results |> Seq.forall (fun result -> result.IsCorrect))

    [<TestMethod>]
    member this.Spider()=
        let results = testValidMoves "spider" Game.spider
        Assert.IsTrue(results |> Seq.forall (fun result -> result.IsCorrect))

    [<TestMethod>]
    member this.Hopper()=
        let results = testValidMoves "grasshopper" Game.hopper
        Assert.IsTrue(results |> Seq.forall (fun result -> result.IsCorrect))

    [<TestMethod>]
    member this.Bee()=
        let results = testValidMoves "bee" Game.bee
        Assert.IsTrue(results |> Seq.forall (fun result -> result.IsCorrect))

    [<TestMethod>]
    member this.ValidPlaceTargets()=
        let results = 
            Database.queryAll
            |> List.filter (fun testBoard -> testBoard.Title.Contains("placement"))
            |> List.mapi (fun testId testBoard ->
                let occupied = Database.createBoard testBoard (fun h -> h.Color <> "empty") (fun h -> { Bug=Bug.Bee; Color=if h.Color="black" then Color.Black else Color.White } : Tile)
                let expected = 
                    Database.createBoard testBoard (fun h -> h.Color = "empty") (fun h -> true)
                    |> Board.stacks
                    |> Seq.map (fun (hex, stack) -> hex)
                    |> Seq.sort
                    |> List.ofSeq
                let actual = Game.placeTargets Color.White occupied |> Seq.sort |> List.ofSeq
                let isCorrect = expected = actual
                {
                    IsCorrect = isCorrect;
                    TestId = testId;
                    Expected = expected;
                    Actual = actual
                })
        Assert.IsTrue(results |> List.forall (fun i -> i.IsCorrect))

    [<TestMethod>]
    member this.BeetlePaths()=
        // White piece is beetle
        // Tag format is {height} {x}, both of which are optional
        // If {height} is missing the assumed height is 1
        // Valid destinations are marked with {x}
        let parseTag (h:Database.Hex) =
            let tag = if (h.Tag = null) then "" else h.Tag.Trim().Replace("  "," ")
            match tag.Split(' ') with
            | [| "" |] -> (1, false)
            | [| height; "x" |] -> ((height |> int32), true)
            | [| height |] when height<>"x" -> ((height |> int32), false)
            | [| "x" |] -> (1, true)
            | _ -> failwith "Not implemented or error in test."
        let height h = let (height, isDestination) = parseTag h in height
        let isDestination h = let (height, isDestination) = parseTag h in isDestination
        let piece (h:Database.Hex) = h.Color
        let results =
            Database.queryAll
            |> List.filter (fun testBoard -> testBoard.Title="beetle")
            |> List.mapi (fun testId testBoard ->
                let occupied = Database.createRaisedBoard testBoard (fun h -> h.Color <> "empty") piece height
                let bugLocation = 
                    occupied
                    |> Board.stacks
                    |> Seq.find (fun (hex, stack) -> stack |> Seq.head = "white")
                    |> fun (hex, stack) -> hex
                let expected = 
                    Database.createBoard testBoard (fun h -> h |> isDestination) (fun h -> true)
                    |> Board.stacks
                    |> Seq.map (fun (hex, stack) -> hex)
                    |> Set.ofSeq
                let actual = Game.beetle bugLocation occupied |> List.ofSeq
                let isCorrectNumberOfResults = actual.Length = expected.Count
                let isCorrectResults = (actual |> Set.ofList) = expected
                let isCorrect = isCorrectNumberOfResults && isCorrectResults
                { 
                    IsCorrect = isCorrect; 
                    TestId = testId; 
                    Expected = expected;
                    Actual = actual;
                })
        Assert.IsTrue(results |> List.forall (fun i -> i.IsCorrect))
