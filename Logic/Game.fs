namespace Hive.Logic

type Bug = | Beetle | Grasshopper | Bee | Spider | Ant

type Color = | Black | White

type Tile = { 
    Color: Color; 
    Bug: Bug }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Tile =
    let color (tile:Tile) = tile.Color
    let bug (tile:Tile) = tile.Bug

type GameState =
    | NextTurnBy of Color
    | Victor of Color
    | Tie

type BeeStatus = 
    | TilesPlaced of int
    | InPlay of IHex

type Turn =
    | Place of Bug * IHex
    | Move of IHex * IHex
    | Skip

type GameHash =
    | Tile of PieceInPlay<Tile>
    | State of GameState

type IGame = 
    abstract member GameState : GameState
    abstract member Board : IBoard<Tile>
    abstract member Reserve : Color -> (Bug * int) seq
    abstract member TakeTurn : Turn -> IGame
    abstract member LegalTurns : Color -> Turn seq
    abstract member Bee : (Color * BeeStatus) seq
    abstract member Hash64 : int64

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Color =
    let other color = 
        match color with
        | Black -> White
        | White -> Black

module Game =
    let gameState (game:IGame) = game.GameState
    let board (game:IGame) = game.Board
    let reserve color (game:IGame) = game.Reserve color
    let takeTurn turn (game:IGame) = game.TakeTurn turn
    let turns color (game:IGame) = game.LegalTurns color
    let beeStatus (game:IGame) = game.Bee
    let hash64 (game:IGame) = game.Hash64

    let coreBugs = [ 
        (Bug.Spider, 2); 
        (Bug.Bee, 1); 
        (Bug.Grasshopper, 3); 
        (Bug.Ant, 3); 
        (Bug.Beetle, 2) 
        ]

    let ant source board =
        let neighbors node =
            node
            |> Hex.neighbors
            |> Array.bits (fun n -> board |> Board.isOccupied n && n<>source)
            |> fun bits -> Board.freedom.[bits]
            |> List.map (fun direction -> node.Neighbors.[direction])
        Graph.depthFirst source neighbors
        |> Seq.skip 1
        |> Seq.map (fun visit -> visit.Node)

    // http://www.boardgamegeek.com/thread/461630/is-this-a-legal-move
    // http://boardgamegeek.com/thread/863355/an-illustrated-question-about-ant-movement-happene
    let spider source board =
        let neighbors node =
            node
            |> Hex.neighbors
            |> Array.bits (fun n -> board |> Board.isOccupied n && n<>source)
            |> fun bits -> Board.freedom.[bits]
            |> List.map (fun direction -> node.Neighbors.[direction])
        Graph.breadthFirst source neighbors
        |> Seq.takeWhile (fun v -> v.Depth <= 4)
        |> Seq.choose (fun v ->
            match v.Depth with
            | 4 -> Some v.Node
            | _ -> None)

    let bee hex board =
        hex
        |> Hex.neighbors
        |> Array.bits (fun n -> board |> Board.isOccupied n)
        |> fun bits -> Board.freedom.[bits]
        |> List.map (fun direction -> hex.Neighbors.[direction])
        |> Seq.ofList

    // The beetle is subject to the freedom-to-move rule; see http://boardgamegeek.com/wiki/page/Hive_FAQ#toc10
    let beetle hex board =
        let startHeight = board |> Board.height hex
        let neighbors =
            hex
            |> Hex.neighbors
            |> Array.mapi (fun index neighbor -> (index, neighbor, board |> Board.height neighbor))
        neighbors
        |> Seq.choose (fun (goIndex, go, goHeight) -> 
            let (_, _, left) = neighbors.[if goIndex=0 then 5 else goIndex-1]
            let (_, _, right) = neighbors.[if goIndex=5 then 0 else goIndex+1]
            let minGateHeight = min left right
            let maxMoveHeight = max (startHeight-1) goHeight
            let canMove = (maxMoveHeight >= minGateHeight) && (((goHeight>0) || (startHeight>1)) || (not (maxMoveHeight=0 && left=0 && right=0)))
            match canMove with
            | true -> Some go
            | false -> None)

    let hopper hex board =
        hex
        |> Hex.neighborsVia
        |> Seq.choose (fun (direction, neighbor) ->
            match board |> Board.isEmpty neighbor with
            | true -> None
            | false ->
                Hex.vector neighbor direction
                |> Seq.find (fun h -> board |> Board.tryFind h = None)
                |> Option.Some)

    let stateAfterTurn lastTurnBy board bees =
        let isSurrounded player = 
            match bees |> Map.find player with
            | InPlay hex -> board |> Board.isSurrounded hex
            | _ -> false
        let isBlackSurrounded = isSurrounded Black
        let isWhiteSurrounded = isSurrounded White
        match isBlackSurrounded, isWhiteSurrounded with
        | false, false -> NextTurnBy (lastTurnBy |> Color.other)
        | true, false -> Victor White
        | false, true -> Victor Black
        | true, true -> Tie

    let placeTargets player board =
        match board |> Board.pieceCount with
        | 0 -> Hex.origin |> Seq.singleton // First tile must go in "center", not touching anything
        | 1 -> Hex.origin |> Hex.north |> Seq.singleton  
            // Second tile can (must) touch opposite color. Although it is legal to move anywhere that 
            // surrounds the initial tile, here I artificially limit the choices to the north of the origin 
            // so that when the computer considers possible moves it can think ahead farther.
        | _ ->
            board
            |> Board.tops
            |> Seq.filter (fun (hex, tile) -> tile.Color=player)
            |> Seq.collect (fun (hex, _) -> hex |> Hex.neighbors)
            |> Seq.distinct
            |> Seq.filter (fun empty -> 
                match board |> Board.tryFind empty with
                | None ->
                    empty
                    |> Hex.neighbors
                    |> Seq.forall (fun neighbor -> 
                        match board |> Board.tryFind neighbor with
                        | None -> true
                        | Some stack -> (stack |> List.head).Color=player)
                | Some _ -> false)

    let legalMoves player beeStatus board =
        match beeStatus with
        | TilesPlaced _ -> Seq.empty
        | InPlay _ ->
            let breakers = 
                board 
                |> Board.cutVertices
                |> List.choose (fun hex ->
                    let stack = board |> Board.find hex
                    match stack.Head.Color=player && stack.Length=1 with
                    | true -> Some hex
                    | false -> None)
                |> Set.ofList
            board
            |> Board.tops
            |> Seq.choose (fun (hex, tile) ->
                match tile.Color = player && not (breakers.Contains hex) with
                | true ->
                    match tile.Bug with
                        | Bug.Ant -> board |> ant hex
                        | Bug.Bee -> board |> bee hex
                        | Bug.Spider -> board |> spider hex
                        | Bug.Beetle -> board |> beetle hex
                        | Bug.Grasshopper -> board |> hopper hex
                    |> Seq.map (fun target -> Move (hex, target))
                    |> Option.Some
                | false -> None)
            |> Seq.concat

    let legalPlaces player beeStatus reserve board =
        let bugs =
            match beeStatus with
                | InPlay _ -> reserve
                | TilesPlaced x -> if x<3 then reserve else Bug.Bee |> Seq.singleton
        let targets = 
            match bugs |> Seq.isEmpty with
                | true -> Seq.empty
                | false -> placeTargets player board |> Seq.cache
        seq {
            for bug in bugs do
                for target in targets do
                    yield Place (bug, target)
            }
                            
    let legalTurns player reserve board bee =
        seq {
            yield! legalPlaces player bee reserve board
            yield! legalMoves player bee board
        }

    let hashesRandomSeed = 33
    let hashes = Minimax.randomHash64<GameHash> (Some hashesRandomSeed)

    let start firstPlayer =
        let rec build board state reserve beeStatus zobrist =
            let legal =
                [Color.Black; Color.White]
                |> List.map (fun player ->
                    let turns =
                        lazy
                            reserve 
                            |> Map.find player
                            |> Map.toSeq
                            |> Seq.map (fun (bug, count) -> bug)
                            |> fun reserve -> legalTurns player reserve board (beeStatus |> Map.find player)
                            |> Seq.cache
                    (player, turns))
                |> Map.ofSeq
            { new IGame with
                member this.Hash64 = zobrist
                member this.GameState = state
                member this.Board = board
                member this.Reserve color = reserve |> Map.find color |> Map.toSeq
                member this.LegalTurns player = legal |> Map.find player |> Lazy.force
                member this.Bee = beeStatus |> Map.toSeq
                member this.TakeTurn turn =
                    let player = 
                        match state with
                        | NextTurnBy color -> color
                        | _ -> invalidOp "The game is over."
                    match turn with
                        | Place (bug, hex) ->
                            assert
                                legal
                                |> Map.find player
                                |> Lazy.force
                                |> Seq.exists (fun i -> i = turn)
                            let tile = { Bug = bug; Color = player }
                            let board = board |> Board.put hex tile
                            let targetZ = (board |> Board.find hex |> List.length) - 1
                            let reserve =
                                reserve
                                |> Map.find player
                                |> Map.replace bug (fun count -> 
                                    match count.Value with
                                    | 1 -> None
                                    | count -> (count-1) |> Option.Some)
                                |> fun bugs -> reserve |> Map.add player bugs
                            let beeStatus =
                                let bee = beeStatus |> Map.find player
                                match bug, bee with
                                | _, InPlay _ -> beeStatus
                                | Bee, _ -> beeStatus |> Map.add player (InPlay hex)
                                | _, TilesPlaced x -> beeStatus |> Map.add player (TilesPlaced (x+1))
                            let state' = stateAfterTurn player board beeStatus
                            let zobrist = 
                                zobrist 
                                ^^^ hashes (state |> State)
                                ^^^ hashes (state' |> State)
                                ^^^ hashes ({ Hex=hex; Piece=tile; Z=targetZ } |> Tile)
                            build board state' reserve beeStatus zobrist
                        | Move (source, target) ->
                            assert
                                legal
                                |> Map.find player
                                |> Lazy.force
                                |> Seq.exists (fun i -> i=turn)
                            let (piece, sourceZ) = 
                                let stack = board |> Board.find source 
                                (stack |> List.head, (stack |> List.length) - 1)
                            let board = board |> Board.move source target
                            let targetZ = (board |> Board.find target |> List.length) - 1
                            let beeStatus =
                                match piece.Bug with
                                | Bee -> beeStatus |> Map.add player (InPlay target)
                                | _ -> beeStatus
                            let state' = stateAfterTurn player board beeStatus
                            let zobrist = 
                                zobrist 
                                ^^^ hashes (state |> State)
                                ^^^ hashes (state' |> State)
                                ^^^ hashes ({ Hex=source; Piece=piece; Z=sourceZ } |> Tile)
                                ^^^ hashes ({ Hex=target; Piece=piece; Z=targetZ } |> Tile)
                            build board state' reserve beeStatus zobrist
                        | Skip ->
                            assert 
                                legal 
                                |> Map.find player
                                |> Lazy.force
                                |> Seq.isEmpty
                            let state' = stateAfterTurn player board beeStatus
                            let zobrist = 
                                zobrist 
                                ^^^ hashes (state |> State) 
                                ^^^ hashes (state' |> State)
                            build board state' reserve beeStatus zobrist
            }
        let state = NextTurnBy firstPlayer
        let board = Board.empty
        let reserve = 
            coreBugs
            |> Map.ofSeq
            |> fun bugs -> 
                Map.empty
                |> Map.add Color.White bugs
                |> Map.add Color.Black bugs
        let beeStatus = [(White, TilesPlaced 0);(Black, TilesPlaced 0)] |> Map.ofList
        let zobrist = hashes (state |> GameHash.State)
        build board state reserve beeStatus zobrist