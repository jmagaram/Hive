namespace Hive.Logic

type IBoard<'Piece> =
    abstract member Move : source:IHex * target:IHex -> IBoard<'Piece>
    abstract member Put : IHex * 'Piece -> IBoard<'Piece>
    abstract member Stacks : (IHex * 'Piece list) seq
    abstract member TryFind : IHex -> 'Piece list option
    abstract member CutVertices : IHex list
    abstract member PieceCount : int

type PieceInPlay<'Piece> = {
        Hex: IHex;
        Piece: 'Piece;
        Z: int; }

type Range = 
    { 
        MinX : int;
        MaxX : int; 
        MinY : int; 
        MaxY : int 
    }
    member this.Expand x y = { 
        MinX = min x this.MinX; 
        MaxX = max x this.MaxX; 
        MinY = min y this.MinY; 
        MaxY = max y this.MaxY }
    member this.IsInRange x y = x>=this.MinX && x<=this.MaxX && y>=this.MinY && y<=this.MaxY
    member this.Width = this.MaxX-this.MinX+1
    member this.Height = this.MaxY-this.MinY+1
    static member CreateFromPoint x y = {
        MinX = x
        MinY = y
        MaxX = x
        MaxY = y }

module Board =
    let rec build stacks range pieceCount =
        let push piece stack =
            match stack with
                | None -> []
                | Some stack -> stack
            |> fun list -> piece :: list
            |> Option.Some
        let pop stack =
            let stack = stack |> Option.get
            match stack with
            | [singleton] -> None
            | head :: tail -> Some tail
            | [] -> failwith "Can't pop an empty stack."
        let tryFind = 
            let grid = Array2D.zeroCreateBased range.MinX range.MinY range.Width range.Height
            do stacks |> Map.iter (fun (h:IHex) item -> grid.[h.X, h.Y] <- Some item)
            fun x y ->
                match range.IsInRange x y with
                | true -> grid.[x, y]
                | false -> None
        let cutVertices =
            lazy
                let root =
                    stacks
                    |> Map.toSeq
                    |> Seq.head
                    |> fun (hex, _) -> hex
                let neighbors source = 
                    source
                    |> Hex.neighbors
                    |> Seq.filter (fun neighbor -> tryFind neighbor.X neighbor.Y <> None)
                Graph.cutVertices root neighbors
        { new IBoard<'Piece> with
            member this.Put (hex, piece) =
                let stacks = stacks |> Map.replace hex (push piece)
                let range = range.Expand hex.X hex.Y
                build stacks range (pieceCount+1)
            member this.Move (source, target) = 
                let piece = 
                    stacks 
                    |> Map.find source
                    |> List.head
                let stacks =
                    stacks
                    |> Map.replace source pop
                    |> Map.replace target (push piece)
                let range = range.Expand target.X target.Y
                build stacks range pieceCount
            member this.Stacks = stacks |> Map.toSeq
            member this.TryFind hex = tryFind hex.X hex.Y
            member this.CutVertices = cutVertices |> Lazy.force
            member this.PieceCount = pieceCount
        }

    let empty = 
        { new IBoard<'Piece> with
            member this.Put (hex, piece) =
                let stacks = Map.empty |> Map.add hex [piece]
                let range = Range.CreateFromPoint hex.X hex.Y
                build stacks range 1
            member this.Move (source, target) = failwith "There are no pieces to move."
            member this.Stacks = Seq.empty
            member this.CutVertices = List.empty
            member this.PieceCount = 0
            member this.TryFind hex = None }

    let put hex piece (board:IBoard<_>) = board.Put (hex, piece)
    let move source target (board:IBoard<_>) = board.Move (source, target)
    let stacks (board:IBoard<_>) = board.Stacks
    let tops (board:IBoard<_>) = board.Stacks |> Seq.map (fun (hex, stack) -> (hex, stack.Head))
    let pieces (board:IBoard<_>) = board.Stacks |> Seq.collect (fun (hex, stack) -> stack |> Seq.map (fun piece -> (hex, piece)))
    let piecesZ (board:IBoard<_>) = board.Stacks |> Seq.collect (fun (hex, stack) -> stack |> List.rev |> List.mapi (fun z piece -> { Piece=piece; Hex=hex; Z=z }))
    let pieceCount (board:IBoard<_>) = board.PieceCount
    let tryFind hex (board:IBoard<_>) = board.TryFind hex
    let find hex (board:IBoard<_>) = board.TryFind hex |> Option.get
    let isEmpty hex (board:IBoard<_>) = (board.TryFind hex).IsNone
    let isOccupied hex (board:IBoard<_>) = (board.TryFind hex).IsSome
    let cutVertices (board:IBoard<_>) = board.CutVertices
    let isSurrounded hex (board:IBoard<_>) =
        hex
        |> Hex.neighbors
        |> Seq.forall (fun neighbor -> board |> isOccupied neighbor)

    let height hex (board:IBoard<_>) = 
        match board |> tryFind hex with
        | Some stack -> stack |> List.length
        | None -> 0

    // Helps calculate the freedom-to-move rule for a piece. The index into the array is the "question"
    // where the high-order bit indicates whether there is a neighbor to the North, and subsequent
    // bits answer this question for the other directions running clockwise. The "answer" is the list
    // of movement directions, with 0 being North, 1 as NorthEast, etc. Use in conjunction with
    // the Array.bits function.
    let freedom =
        let escape border =
            [0..5]
            |> Seq.choose (fun y ->
                let x = if y=0 then 5 else y-1
                let z = if y=5 then 0 else y+1
                let yIsBlocked = border &&& (1 <<< (5-y)) <> 0
                let xIsBlocked = border &&& (1 <<< (5-x)) <> 0
                let zIsBlocked = border &&& (1 <<< (5-z)) <> 0
                let canEscapeY =
                    match (xIsBlocked, yIsBlocked, zIsBlocked) with
                    | (true, false, false) -> true
                    | (false, false, true) -> true
                    | _ -> false
                match canEscapeY with
                | true -> Some y
                | false -> None)
        { 0..63 }
        |> Seq.map (fun questionBits -> escape questionBits |> List.ofSeq)
        |> Array.ofSeq