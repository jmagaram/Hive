module Hive.Test.Database

open Hive.Logic
open MBrace.FsPickler
open MBrace.FsPickler.Json
open Microsoft.VisualStudio.TestTools.UnitTesting
open System

type Hex = 
    { X : int
      Y : int
      Color : string 
      Tag : string }

type Board =
    { Title : string
      CreatedOn : DateTime
      Note : string
      Hexes : Hex list }

let queryAll =
    let json = FsPickler.CreateJsonSerializer(indent = false)
    json.OmitHeader <- true
    use reader = System.IO.File.OpenText("..\..\..\TestData.json")
    json.Deserialize<Board list>(reader)

let printHex (hex:Hex) = printfn "     HEX (%i,%i) %s %s" hex.X hex.Y hex.Color hex.Tag

let printBoard (board:Board) = 
    printfn "BOARD %s" board.Title
    board.Hexes |> Seq.iter printHex
    printfn ""

let hexes (board:Board) = board.Hexes

let createRaisedBoard (board:Board) hexFilter pieceConverter heightConverter =
    board.Hexes
    |> Seq.filter hexFilter
    |> Seq.fold (fun board hex -> 
        let height = hex |> heightConverter
        let piece = hex |> pieceConverter
        let hex = Hex.create hex.X hex.Y
        [1..height] |> List.fold (fun board _ -> board |> Board.put hex piece) board) Board.empty

let createBoard board hexFilter pieceConverter = 
    createRaisedBoard board hexFilter pieceConverter (fun _ -> 1)


