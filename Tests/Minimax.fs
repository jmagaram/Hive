module Hive.Test.Logic.Minimax

open Microsoft.VisualStudio.TestTools.UnitTesting
open Hive.Test.Utilities
open Hive.Logic

type Score = int

type Tree =
    | Node of Score * Tree list
    | Leaf of Score

type MinimaxEvent =
    | QueryScore
    | Posted of AsyncReplyChannel<MinimaxEvent list>

let randomTree nodes minScore maxScore seed =
    let map = // Map<NodeNum, Score * ParentNodeNum option>
        let random = new System.Random(seed)
        let randomScore = fun () -> random.Next(minScore, maxScore+1)
        let root = Map.empty |> Map.add 0 (randomScore(), None)
        [1..nodes]
        |> Seq.fold (fun map nodeNum -> 
            let randomParentNodeNum = random.Next(0,nodeNum)
            map |> Map.add nodeNum (randomScore(), Some randomParentNodeNum)) root
    let rec tree node =
        let (score, _) = map |> Map.find node
        let children =
            map
            |> Map.toSeq
            |> Seq.choose (fun (child, (score, parent)) ->
                match parent = Some node with
                | true -> Some (tree child)
                | false -> None)
            |> List.ofSeq
        match children with
        | [] -> Leaf score
        | head :: tail -> Node (score, children)
    tree 0

let scoreTree tree = 
    let logger = Hive.Test.Utilities.logger<MinimaxEvent>
    let rec build tree =
        match tree with
        | Node (score, children) ->
            { new IScoreTree<_,_> with 
                member this.Score = 
                    logger.Post QueryScore
                    score
                member this.Children = 
                    children
                    |> List.mapi (fun index child -> (index, child |> build)) // Unique branch IDs leading to each child, starting with 0
                    |> List.toSeq
            }
        | Leaf score -> 
            { new IScoreTree<_,_> with 
                member this.Score =
                    logger.Post QueryScore 
                    score
                member this.Children = Seq.empty
            }
    (build tree, logger)

[<TestClass>]
type MinimaxTest() = 
    [<TestMethod>]
    [<TestCategory("Slow")>]
    member this.Random64_NoDuplicatesWithVariousSeeds()=
        let seeds = { 1..50 }
        let numbersPerSeed = 199999
        Assert.IsTrue(
            seeds
            |> Seq.forall (fun seed -> 
                let rand = Minimax.random64 (Some seed)
                { 1..numbersPerSeed }
                |> Seq.map (fun _ -> rand())
                |> Seq.existDuplicates
                |> not))

    [<TestMethod>]
    member this.Random64_SequenceRepeatableWithSameSeed()=
        let seeds = { 1..10 }
        let numbersPerSeed = 1000
        Assert.IsTrue(
            seeds
            |> Seq.forall(fun seed ->
                let rand1 = Minimax.random64 (Some seed)
                let l1 = List.init numbersPerSeed (fun _ -> rand1())
                let rand2 = Minimax.random64 (Some seed)
                let l2 = List.init numbersPerSeed (fun _ -> rand2())
                l1=l2))

    [<TestMethod>]
    member this.Random64_SequenceUniquePerSeed()=
        let seeds = { 1..100 }
        let numbersPerSeed = 10
        Assert.IsTrue(
            seeds
            |> Seq.map(fun seed ->
                let rand = Minimax.random64 (Some seed)
                List.init numbersPerSeed (fun _ -> rand()))
            |> Seq.existDuplicates
            |> not)

    [<TestMethod>]
    member this.Random64_SequenceUniquePerTimeBasedSeed()=
        let seeds = { 1..100 }
        let numbersPerSeed = 10
        Assert.IsTrue(
            seeds
            |> Seq.map(fun seed ->
                System.Threading.Thread.Sleep(20) // required delay                
                let rand = Minimax.random64 None // time-based seed
                List.init numbersPerSeed (fun _ -> rand()))
            |> Seq.existDuplicates
            |> not)

    [<TestMethod>]
    member this.RandomHash64()=
        let seeds = { 1..50 }
        Assert.IsTrue(
            seeds
            |> Seq.forall(fun seed ->
                let hash = Minimax.randomHash64 (Some seed)
                let a1 = hash 'a'
                let b1 = hash 'b'
                let c1 = hash 'c'
                let a2 = hash 'a'
                let b2 = hash 'b'
                let c2 = hash 'c'
                a1=a2 && b1=b2 && c1=c2 && a1<>b1 && b1<>c1))
        // Hashes correspond to same number in random sequence
        let hash = Minimax.randomHash64 (Some 100)
        let rand = Minimax.random64 (Some 100)
        let hashList = [1..100] |> List.map (fun i -> hash i)
        let randList = [1..100] |> List.map (fun _ -> rand())
        Assert.AreEqual(hashList, randList)

    [<TestMethod>]
    [<TestCategory("Slow")>]
    member this.SolveRandom()=
        let seed = 23
        let minScore = -50
        let maxScore = 50
        let minNodes = 10
        let maxNodes = 1000
        let testCount = 300
        let random = new System.Random(seed)
        let results =
            [1..testCount]
            |> List.map (fun i -> 
                let nodes = random.Next(minNodes,maxNodes)
                let (tree, logger) = randomTree nodes minScore maxScore seed |> scoreTree
                let findMax = random.Next() % 2 = 1
                let depth = random.Next(3,100)

                logger.Clear
                let result = Minimax.solve findMax depth tree
                let scores = logger.Messages |> List.filter (fun i -> i = QueryScore) |> List.length

                logger.Clear
                let resultAb = 
                    match Minimax.solveAlphaBeta findMax (minScore-1) (maxScore+1) depth tree None with
                    | [], score -> (None, score)
                    | head::tail, score -> (Some head, score)
                let scoresAb = logger.Messages |> List.filter (fun i -> i = QueryScore) |> List.length

                logger.Clear
                let resultAbDeepening = 
                    match Minimax.solveAlphaBetaDeepening findMax (minScore-1) (maxScore+1) depth tree None with
                    | [], score -> (None, score)
                    | head::tail, score -> (Some head, score)

                let isCorrect = result=resultAb && (result |> snd)=(resultAbDeepening |> snd) && scoresAb<scores
                (isCorrect, i, scores, scoresAb))
        Assert.IsTrue(results |> List.forall(fun (isCorrect, _, _, _) -> isCorrect))

    [<TestMethod>]
    member this.Solve()=
        let sample =
            Node (8, [ 
                    Node (1, [
                            Node (2, [
                                    Node (1, [Leaf 2;Leaf 6]);
                                    Node (11, [Leaf 7;Leaf 4;Leaf 5])
                                ]);
                            Node (6, [
                                    Node (7, [Leaf 3])
                                ]);
                        ]);
                    Node (9, [
                            Node (4, [
                                    Node (8, [Leaf 6]);
                                    Node (3, [Leaf 4; Leaf 9]);
                                ]);
                            Node (8, [
                                    Node (2, [Leaf 1]);
                                ]);
                        ]);
                    Node (4, [
                            Node (5, [
                                    Node (4, [Leaf 5]);
                                ]);
                            Node (2, [
                                    Node (5, [Leaf 9;Leaf 8]);
                                    Node (2, [Leaf 6]);
                                ]);
                        ])
                ])
        let results =
            [
                (0, true, None, 8); // No searching; just return score of root with no branch to get there
                (0, false, None, 8); // No searching; just return score of root with no branch to get there
                (1, true, Some 1, 9);
                (1, false, Some 0, 1);
                (2, true, Some 1, 4);
                (2, false, Some 2, 5);
                (3, true, Some 0, 7); 
                (3, false, Some 1, 3);
                (4, true, Some 2, 5);
                (4, false, Some 0, 6);
                (99, true, Some 2, 5); // deeper than tree goes
                (99, false, Some 0, 6); // deeper than tree goes
            ]
            |> Seq.mapi (fun index (depth, findMax, expectedBranch, expectedScore) -> 
                let (tree, logger) = sample |> scoreTree

                logger.Clear
                let actual = Minimax.solve findMax depth tree
                let leafQueries = logger.Messages |> Seq.filter (fun i -> i = MinimaxEvent.QueryScore) |> Seq.length           
                
                logger.Clear
                let actualAb = 
                    match Minimax.solveAlphaBeta findMax -999 999 depth tree None with
                    | [], score -> (None, score)
                    | head::tail, score -> (Some head, score)
                let leafQueriesAb = logger.Messages |> Seq.filter (fun i -> i = MinimaxEvent.QueryScore) |> Seq.length

                logger.Clear
                let actualAbIterativeDeepening = 
                    match Minimax.solveAlphaBetaDeepening findMax -999 999 depth tree None with
                    | [], score -> (None, score)
                    | head::tail, score -> (Some head, score)

                System.Diagnostics.Debug.WriteLine(sprintf "Leaves: %i, LeavesAb: %i" leafQueries leafQueriesAb)

                let expected = (expectedBranch, expectedScore)

                let isCorrect = (actual=expected) && (actualAb=expected) && ((actualAbIterativeDeepening |> snd) = (expected |> snd))
                {
                    IsCorrect = isCorrect;
                    TestId = index;
                    Expected = expected
                    Actual = (actual, actualAb, actualAbIterativeDeepening)
                })
            |> List.ofSeq
        Assert.IsTrue(results |> Seq.forall (fun i -> i.IsCorrect))