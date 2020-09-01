namespace Hive.Logic

open System
open System.Collections.Generic

type IHex =
    abstract X : int;
    abstract Y : int;
    abstract Neighbors : IHex array
    inherit IComparable

module Hex =
    [<CustomEquality>]
    [<CustomComparison>]
    type private Point =
        {
            X : int;
            Y : int;
            Key : int;
            Neighbors : Lazy<IHex array>
        }

        override this.GetHashCode() = this.Key

        override this.Equals obj =
            let other = obj :?> Point
            this.Key=other.Key

        override this.ToString() = sprintf "(%i,%i)" this.X this.Y

        interface IComparable with
            member this.CompareTo obj =
                match obj with
                | :? Point as other -> compare this.Key other.Key
                | _ -> invalidArg "obj" "Not a Point"

        interface IHex with
            member this.X = this.X
            member this.Y = this.Y
            member this.Neighbors = this.Neighbors.Value

    // Code assumes x and y are never bigger than will fit in int16
    let rec create = 
        let hexes = new Dictionary<int,IHex>()
        fun x y ->
            let key = ((x |> uint16 |> int) <<< 16) ||| (y |> uint16 |> int)
            lock hexes (fun () ->
                match hexes.TryGetValue(key) with
                | (true, hex) -> hex
                | (false, _) ->
                    let hex =
                        { 
                            X = x
                            Y = y
                            Key = key
                            Neighbors = 
                                lazy 
                                [| 
                                    create x (y+1)             
                                    create (x+1) y             
                                    create (x+1) (y-1)             
                                    create x (y-1)
                                    create (x-1) y
                                    create (x-1) (y+1) |]
                        }
                        :> IHex
                    hexes.Add(key, hex)
                    hex)

    let north (h:IHex) = h.Neighbors.[0]
    let northEast (h:IHex) = h.Neighbors.[1]
    let southEast (h:IHex) = h.Neighbors.[2]
    let south (h:IHex) = h.Neighbors.[3]
    let southWest (h:IHex) = h.Neighbors.[4]
    let northWest (h:IHex) = h.Neighbors.[5]

    let compass = [| north; northEast; southEast; south; southWest; northWest |] : (IHex->IHex) array

    let origin = create 0 0

    let neighbors (hex:IHex) = hex.Neighbors
    let neighborsVia (hex:IHex) = Array.zip compass hex.Neighbors

    let touches (h1:IHex) (h2:IHex) =
        let xDiff = h2.X-h1.X
        match xDiff with
        | 0 -> 
            let yDiff = h2.Y-h1.Y
            yDiff=1 || yDiff=(-1)
        | 1 -> 
            let yDiff = h2.Y-h1.Y 
            yDiff=0 || yDiff=(-1)
        | -1 -> 
            let yDiff = h2.Y-h1.Y 
            yDiff = 0 || yDiff=1
        | _ -> false

    let vector (start:IHex) offset = Seq.unfold (fun start -> Some (start, offset start)) start

