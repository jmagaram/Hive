open Hive.Logic
open System.IO
open System

type GameOptions = {
    FirstPlayer : Color
    MaxDepth : int
    MaxTurns : int
    MaxSecondsToThink : int option
    RandomizeFirstMove : bool
    BlackEvaluator : IGame -> int
    WhiteEvaluator : IGame -> int
}

[<EntryPoint>]
let main argv =
    let enableFileLogging = 
        fun () ->
            let logPath = sprintf "log %i.txt" System.DateTime.UtcNow.Ticks
            use file = File.CreateText logPath
            file.AutoFlush <- true
            Console.SetOut file
    let takeRandomTurns count seed =
        let random = new System.Random(seed)
        let newGame = Game.start Color.White
        let maxTurns = 50
        [1..count]
        |> Seq.fold (fun (game, turnNumber) _ ->
            match turnNumber with
            | 0 -> (newGame, maxTurns)
            | _ ->
                match game |> Game.gameState with
                | Victor _ | GameState.Tie -> (newGame, maxTurns)
                | NextTurnBy player ->
                    let legal = game.LegalTurns player |> Array.ofSeq
                    match legal.Length with
                    | 0 -> (game.TakeTurn Skip, turnNumber-1)
                    | _ ->
                        let turn = legal.[random.Next(0, legal |> Array.length)]
                        let game = game |> Game.takeTurn turn
                        (game, turnNumber-1)) (newGame, maxTurns)                        
    let playGameAgainstSelf opt =
        [1..opt.MaxTurns]
        |> List.fold (fun game turn ->
            printfn " Turn %i" turn
            match game |> Game.gameState with
            | Victor _ | GameState.Tie -> Game.start White
            | NextTurnBy White -> Strategy.takeBestTurn game opt.MaxDepth opt.RandomizeFirstMove opt.WhiteEvaluator opt.MaxSecondsToThink
            | NextTurnBy Black -> Strategy.takeBestTurn game opt.MaxDepth opt.RandomizeFirstMove opt.BlackEvaluator opt.MaxSecondsToThink) (Game.start opt.FirstPlayer)
    let simplePerformanceTest =
        fun () ->
            let options = {
                FirstPlayer = White
                MaxDepth = 4
                MaxSecondsToThink = None
                RandomizeFirstMove = false
                BlackEvaluator = Strategy.champion
                WhiteEvaluator = Strategy.champion
                MaxTurns = 15        
                }
            playGameAgainstSelf options |> ignore
    let tournament whiteStrategy blackStrategy iterations secondsToThink =
        let options = {
                FirstPlayer = White
                MaxDepth = 99
                MaxSecondsToThink = Some secondsToThink
                RandomizeFirstMove = true
                BlackEvaluator = blackStrategy
                WhiteEvaluator = whiteStrategy
                MaxTurns = 50            
            }
        let stopWatch = System.Diagnostics.Stopwatch.StartNew()
        let results =
            [1..iterations*2]
            |> List.fold (fun results gameNumber ->
                printfn "Game %i of %i" gameNumber (iterations*2)
                let options = { options with FirstPlayer = if (gameNumber%2=0) then White else Black }
                let finalGameState = playGameAgainstSelf options |> Game.gameState
                finalGameState :: results) []
        let summary = 
            results
            |> List.toSeq
            |> Seq.countBy (fun i -> i)
            |> List.ofSeq
        printfn "Results: %A" summary
        printfn "Elapsed minutes: %A" stopWatch.Elapsed.TotalMinutes
        Console.ReadLine() |> ignore

    simplePerformanceTest()
    0