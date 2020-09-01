# Play Hive (coded in 2013)

[Hive](https://www.boardgamegeek.com/boardgame/2655/hive) is a great abstract strategy game, kind of like chess but with some bug-themed pieces. I wanted to learn about functional programming in F# and decided to create a Windows program that would allow me to play against a computer opponent.

The most interesting challenge was computing the legal moves for each piece. The pieces are placed on a 3D hexagonal board and each "bug" has very specific movement rules. No piece can move if picking it up would disconnect the "hive"; all pieces on the board must always be in contact with every other piece. This relates to "cut vertices" in graph theory. Grasshoppers can jump in a straight line. Beetles can move on top of over pieces. Ants can move anywhere they can slide to. Spiders creep along the edge of the other pieces in movements of 3.

The second most interesting challenge was figuring out how to get the computer to play a smart game. I wasn't very successful here. It uses minimax and tree pruning to look ahead several moves and figure out the best option. But because there are so many legal moves for each piece - a single ant might be able to move to 15 different spots - and the player often has lots of pieces in play - the look-ahead tree gets very big very fast. I tried using exclusively immutable data structures but performance was poor. I used profiling to determine where the most time was spent and tweaked some things to make it run faster. If the game engine were written in Rust or C++ it probably would have gone faster. But given the exponential growth of the tree this probably wouldn't have solved the main problem. I also used a fairly simple board evaluation function to determine who was winning. This was based on how many possible moves your "bee" has (if your bee is trapped you lose) and a few other metrics. It would be interesting to try an AI approach rather than minimax.

The solution has several components:

*Logic* - The game engine that enforces valid moves, tracks the pieces on the board, minimax, etc.

*PlayHiveGame* - The WPF (Windows) client for playing the game against another player or the computer.

*Tests* - Some unit tests

*TestsEditor* - A UI tool for "drawing" tests. This was invaluable for ensuring that each piece moves correctly. I could draw a potential board, indicate where the "ant" was, and specify all the legal places it can go. 
