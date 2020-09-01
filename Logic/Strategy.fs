namespace Hive.Logic

module Strategy =
    let goalWhite = System.Int32.MaxValue // Bigger is better for White
    let goalBlack = System.Int32.MinValue // Smaller is better for Black

    // This is a fast evaluation function with two factors.
    //
    // The first factor measures how trapped the bee is. A bee is trapped if any of the following are true: 
    // it is covered by a Beetle, it is a cut vertice, or it is surrounded by enough tiles that it can't slide 
    // away. If a bee is trapped then the severity of the trapping is based on the number of tiles surrounding 
    // it. No weight is given to the color of those tiles or whether those tiles can be easily moved away like 
    // Beetles and Grasshoppers of the same color as the bee that are also not cut-vertices.
    //
    // The second factor attempts to measure how much flexibility each player has - the number of possible
    // moves. Because this is expensive to calculate I am simply calculating how many tiles are in play.
    // This encourages the player to move tiles out of their reserve.
    let champion (game:IGame) =
        match game |> Game.gameState with
        | NextTurnBy _ ->
            [White; Black]
            |> List.map (fun player ->
                let unplayedTiles = 
                    game.Reserve player 
                    |> Seq.sumBy (fun (bug, unplayed) -> unplayed)
                let beeTrappedBy =
                    game.Bee
                    |> Seq.pick (fun (color, status) -> if color=player then Some status else None)
                    |> function
                        | TilesPlaced _ -> 0
                        | InPlay beeHex ->
                            let isCutVertice = fun () -> game.Board.CutVertices |> List.exists (fun cut -> cut = beeHex)
                            let noFreedom = fun () -> Game.bee beeHex game.Board |> Seq.isEmpty
                            let isCoveredByBeetle = fun () -> game.Board |> Board.find beeHex |> List.length > 1
                            let isTrapped = noFreedom() || isCutVertice() || isCoveredByBeetle()
                            match isTrapped with
                            | true -> 1 + (beeHex.Neighbors |> Array.sumBy (fun n -> if game.Board.TryFind(n)<>None then 1 else 0))
                            | false -> 0
                -(unplayedTiles + beeTrappedBy*999)
                |> fun score -> if player = White then score else -score)
            |> List.sum
        | Victor Black -> goalBlack
        | Victor White -> goalWhite
        | Tie -> 0        

    // This is a sophisticated but slow evaluation function.
    let challenger (game:IGame) =
        match game |> Game.gameState with
        | NextTurnBy _ ->
            [White; Black]
            |> List.map (fun player ->
                let (beeMoves, otherMoves) =
                    game.LegalTurns player
                    |> Seq.choose (fun turn -> 
                        match turn with
                        | Move (source, target) -> Some (source, target)
                        | _ -> None)
                    |> Seq.groupPair
                    |> Seq.fold (fun (beeMoves, otherMoves) (source, targets) ->
                        let targets = targets |> Seq.length
                        match game.Board.TryFind(source).Value.Head.Bug with
                        | Bee -> (beeMoves + targets, otherMoves)
                        | _ -> (beeMoves, otherMoves + targets)) (0, 0)
                let unplayedTiles = game.Reserve player |> Seq.sumBy (fun (bug, unplayed) -> unplayed)
                let beeTrappedBy =
                    game.Bee
                    |> Seq.pick (fun (color, status) -> if color=player then Some status else None)
                    |> function
                        | TilesPlaced x -> 0 // not yet in play
                        | InPlay hex ->
                            match beeMoves with
                            | 0 -> // is in play but no valid moves for it - trapped!
                                (hex 
                                |> Hex.neighbors 
                                |> Array.sumBy (fun n -> if game.Board.TryFind(n) = None then 0 else 1)) + 1 // Add 1 for beetle
                            | _ -> 0 // is in play and has some valid moves so it is not trapped
                beeMoves*10 + otherMoves*3 - unplayedTiles*100 - beeTrappedBy*10000
                |> fun score -> if player = White then score else -score)
            |> List.sum
        | Victor Black -> goalBlack
        | Victor White -> goalWhite
        | Tie -> 0        

    let rec scoreTree evaluator game randomFirstTurnOrder =
        { new IScoreTree<_,_> with
            member this.Score = game |> evaluator
            member this.Children =
                match game |> Game.gameState with
                | NextTurnBy player -> 
                    let turns =
                        match randomFirstTurnOrder with
                        | false -> game.LegalTurns player
                        | true -> 
                            let random = new System.Random()
                            game.LegalTurns player |> Seq.sortBy (fun _ -> random.Next())
                    turns |> Seq.map (fun turn -> (turn, scoreTree evaluator (game |> Game.takeTurn turn) false))
                | Tie -> Seq.empty
                | Victor _ -> Seq.empty
        }

    let takeBestTurn game depth randomFirstTurnOrder evaluator timeoutSeconds =
        let findMax = 
            match game |> Game.gameState with
            | NextTurnBy White -> true
            | NextTurnBy Black -> false
            | _ -> failwith "Should never happen!"
        let root = scoreTree evaluator game randomFirstTurnOrder
        let (principalVariation, score) = Minimax.solveAlphaBetaDeepening findMax goalBlack goalWhite depth root timeoutSeconds
        match principalVariation with
        | bestTurn :: _ -> game |> Game.takeTurn bestTurn
        | _ -> failwith "Should never happen."

