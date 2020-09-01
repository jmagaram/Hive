namespace Hive.Play

open System.Collections
open System.Collections.Generic
open Hive

type Hex = 
    { 
        X: int; 
        Y: int 
    }
    static member op_Equality (a:Hex, b:Hex) = a=b
    static member op_Inequality (a:Hex, b:Hex) = a<>b

type Bug = | Beetle=0 | Grasshopper=1 | Bee=2 | Spider=3 | Ant=4
type Color = | Black=0 | White=1

type Tile = 
    { 
        Color: Color; 
        Bug: Bug 
    }

type TileStack = { Hex : Hex; Tiles : Tile seq; }

type State =
    | Tie = 1
    | NextTurnByWhite = 3
    | NextTurnByBlack = 4
    | WonByWhite = 5
    | WonByBlack = 6

type LegalPlace = {
    Bugs : Bug seq;
    Targets : Hex seq;
    }

type LegalMove = {
    Source : Hex;
    Targets : Hex seq;
    }

type IHiveGame =
    abstract member Stacks : TileStack seq
    abstract member Reserve : Color -> Tile seq
    abstract member State : State
    abstract member LegalMoves : LegalMove seq
    abstract member LegalPlace : LegalPlace
    abstract member Move : source:Hex -> target:Hex -> IHiveGame
    abstract member Place : Bug -> Hex -> IHiveGame
    abstract member SkipTurn : IHiveGame
    abstract member TakeBestTurn : depth:int -> timeoutSeconds:int -> IHiveGame

module Play =           
    let toHex (h:Logic.IHex) = { X=h.X; Y=h.Y } : Hex
    let fromHex (h:Hex) = Logic.Hex.create h.X h.Y
    let toBug (i:Logic.Bug) =
        match i with
        | Logic.Ant -> Bug.Ant
        | Logic.Bee -> Bug.Bee
        | Logic.Grasshopper -> Bug.Grasshopper
        | Logic.Spider -> Bug.Spider
        | Logic.Beetle -> Bug.Beetle
    let fromBug (i:Bug) =
        match i with
        | Bug.Ant -> Logic.Ant
        | Bug.Bee -> Logic.Bee
        | Bug.Grasshopper -> Logic.Grasshopper
        | Bug.Spider -> Logic.Spider
        | Bug.Beetle -> Logic.Beetle
        | _ -> invalidOp "Not supported"
    let toColor (c:Logic.Color) =
        match c with
        | Logic.Black -> Color.Black
        | Logic.White -> Color.White
    let fromColor (c:Color) =
        match c with
        | Color.Black -> Logic.Color.Black
        | _ -> Logic.Color.White
    let toTile (t:Logic.Tile) = { Bug = t.Bug |> toBug; Color = t.Color |> toColor }
    let toGameState (s:Logic.GameState) =
        match s with
        | Logic.NextTurnBy Logic.White -> State.NextTurnByWhite
        | Logic.NextTurnBy Logic.Black -> State.NextTurnByBlack
        | Logic.Tie -> State.Tie
        | Logic.Victor Logic.White -> State.WonByWhite
        | Logic.Victor Logic.Black -> State.WonByBlack
    let nextTurnBy (gameState:Logic.GameState) = 
        match gameState with
        | Logic.NextTurnBy color -> color
        | _ -> invalidOp "There is no current player for a completed game."
        
    let rec toGame (game:Logic.IGame) =
        { new IHiveGame with
            member this.Stacks =
                game
                |> Logic.Game.board
                |> Logic.Board.stacks
                |> Seq.map (fun (hex, stack) -> { Hex = hex |> toHex; Tiles = stack |> Seq.map toTile })
            member this.Reserve color =
                game
                |> Logic.Game.reserve (color |> fromColor)
                |> Seq.collect (fun (bug, count) -> Seq.init count (fun _ -> { Bug = bug |> toBug; Color = color }))
                |> Seq.sortBy (fun t -> t.Bug)
            member this.State = game.GameState |> toGameState
            member this.LegalMoves = 
                let currentPlayer = game.GameState |> nextTurnBy
                game.LegalTurns currentPlayer
                |> Seq.choose (fun i ->
                    match i with
                    | Logic.Move (source, target) -> Some (source |> toHex, target |> toHex)
                    | _ -> None)
                |> Logic.Seq.groupPair
                |> Seq.map (fun (source, targets) -> { Source = source; Targets = targets })
            member this.LegalPlace =
                let currentPlayer = game.GameState |> nextTurnBy
                let bugsAndTargets =
                    game.LegalTurns currentPlayer
                    |> Seq.choose (fun i -> 
                        match i with 
                        | Logic.Place (bug, target) -> Some (bug, target) 
                        | _ -> None)
                    |> Seq.cache
                let bugs = bugsAndTargets |> Seq.map (fun (bug, target) -> bug |> toBug) |> Seq.distinct
                let targets = bugsAndTargets |> Seq.map (fun (bug, target) -> target |> toHex) |> Seq.distinct
                { Bugs = bugs; Targets = targets }
            member this.Move source target = 
                let turn = Logic.Move ((source |> fromHex),(target |> fromHex))
                game.TakeTurn turn |> toGame
            member this.Place bug target = 
                let turn = Logic.Place ((bug |> fromBug), (target |> fromHex))
                game.TakeTurn turn |> toGame
            member this.SkipTurn = game.TakeTurn Logic.Skip |> toGame
            member this.TakeBestTurn depth timeoutSeconds = 
                let timeoutSeconds =
                    match timeoutSeconds with
                    | 0 | System.Int32.MaxValue | System.Int32.MinValue | -1 -> None
                    | _ -> Some timeoutSeconds
                Logic.Strategy.takeBestTurn game depth true Logic.Strategy.challenger timeoutSeconds |> toGame
        }

    let start firstPlayerColor = Logic.Game.start firstPlayerColor |> toGame

[<AbstractClass; Sealed>]
type HiveGame =
    static member New = Play.start Logic.White
