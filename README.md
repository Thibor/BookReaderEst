# BookReaderTnt
BoookReaderTnt can be used as normal UCI chess engine in chess GUI like Arena.
This program can read and add moves to chess openig book with mem extension.
To use this program you need install  <a href="https://dotnet.microsoft.com/download/dotnet-framework/net48">.NET Framework 4.8</a>

## Parameters

**-bf** mem opening Book File<br/>
**-ef** chess Engine File<br/>
**-tf** chess Teacher File<br/>
**-ea** chess Engine Arguments<br/>
**-w** Write new moves to the book<br/>
**-u** update moves in the book<br/>
**-lr** Limit maximum ply depth when Read from book (default 8) 0 means no limit<br/>
**-lw** Limit maximum ply depth when Write to book (default 8) 0 means no limit<br/>
**-add** Limit ADDed moves per game (default 8) 0 means no limit<br/>
**-rnd** RaNDomization (default 50) 0 - means choose best move 100 - mean choose random move 200 - mean choose worst move<br/>
**-log** Create LOG file<br/>
**-info** show additional INFOrmation<br/>

## Console commands

**help** - displays basic commands<br/>
**book load** [filename].[est|uci|pgn|txt] - clear and add<br/>
**book save** [filename].[est|uci|pgn] - save book to the file<br/>
**book addfile** [filename].[mem|pgn|uci|fen] - adds moves from another book<br/>
**book delete** [number x] - delete x moves from the book<br/>
**book clear** - clear all moves from the book<br/>
**book adduci** [uci] - adds a sequence of moves in uci format<br/>
**book addfen** [fen] - add position in fen format<br/>
**book moves** [uci] - make sequence of moves in uci format and shows possible continuations<br/>
**book info* - show extra informations of current book<br/>
**book getoption** - show options<br/>
**book setoption name [option name] value [option value]** - set option<br/>
**quit** quit the program as soon as possible

### Examples

BookReaderEst.exe **-bf bigtnt.tnt -ef stockfish.exe**<br/>
BookReaderEst.exe **bigtnt -ef stockfish.exe**

The program will first try to find move in chess opening book named bigtnt.tnt, and if it doesn't find any move in it, it will run a chess engine named stockfish.exe

BookReaderEst.exe **-bf bigtnt.tnt -w 100K -ef stockfish.exe**<br/>
BookReaderEst.exe **bigtnt -ef stockfish.exe -w 100K**

The program will first try to find move in chess opening book named bigtnt.tnt, and if it doesn't find any move in it, it will run a chess engine named stockfish.exe. If engine loose game, winner moves will be added to the book. If the book does not exist, the program will create it. 100k means that the book will grow to 100 000 moves, after exceeding this number, the less significant moves will be removed to make room for new ones.