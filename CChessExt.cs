using System;
using System.Collections.Generic;
using NSChess;

namespace NSProgram
{
	class CChessExt : CChess
	{

		public bool Is2ToEnd(out string myMov, out string enMov)
		{
			myMov = "";
			enMov = "";
			List<int> mu1 = GenerateValidMoves(out _);//my last move
			foreach (int myMove in mu1)
			{
				bool myEscape = true;
				MakeMove(myMove);
				List<int> mu2 = GenerateValidMoves(out _);//enemy mat move
				foreach (int enMove in mu2)
				{
					bool enAttack = false;
					MakeMove(enMove);
					List<int> mu3 = GenerateValidMoves(out bool mate);//my illegal move
					if (mate)
					{
						myEscape = false;
						enAttack = true;
						myMov = EmoToUmo(myMove);
						enMov = EmoToUmo(enMove);
					}
					UnmakeMove(enMove);
					if (enAttack)
						continue;
				}
				UnmakeMove(myMove);
				if (myEscape)
					return false;
			}
			return true;
		}

		string FlipVBoaS(string boaS)
		{
			string result = string.Empty;
			for (int y = 7; y >= 0; y--)
				for (int x = 0; x < 8; x++)
				{
					char c = boaS[y * 8 + x];
					result += char.IsUpper(c) ? char.ToLower(c) : char.ToUpper(c);
				}
			return result;
		}

		public string GetBoaS()
		{
			string result = string.Empty;
			for (int row = 0; row < 8; row++)
				for (int col = 0; col < 8; col++)
				{
					int i = (row << 3) | col;
					int piece = board[i];
					if (piece == colorEmpty)
						result += "-";
					else
					{
						char[] pieceArr = { ' ', 'p', 'n', 'b', 'r', 'q', 'k', ' ' };
						char pieceChar = pieceArr[piece & 0x7];
						result += ((piece & colorWhite) != 0) ? char.ToUpper(pieceChar) : pieceChar;
					}
				}
			char[] chars = result.ToCharArray();
			if ((castleRights & 1) != 0)
				chars[63] = 'T';
			if ((castleRights & 2) != 0)
				chars[56] = 'T';
			if ((castleRights & 4) != 0)
				chars[7] = 't';
			if ((castleRights & 8) != 0)
				chars[0] = 't';
			if (passing >= 0)
			{
				int x = passing & 7;
				int y = passing >> 3;
				if (WhiteTurn)
					y++;
				else
					y--;
				int i = y * 8 + x;
				if (WhiteTurn)
				{
					if ((x > 0) && (chars[i - 1] == 'P'))
						chars[i] = 'a';
					if ((x < 7) && (chars[i + 1] == 'P'))
						chars[i] = 'a';

				}
				else
				{
					if ((x > 0) && (chars[i - 1] == 'p'))
						chars[i] = 'A';
					if ((x < 7) && (chars[i + 1] == 'p'))
						chars[i] = 'A';
				}
			}

			return new string(chars);
		}

		public string GetTnt()
		{
			string boaS = GetBoaS();
			if (!WhiteTurn)
				boaS = FlipVBoaS(boaS);
			return boaS;
		}

		public void SetTnt(string tnt)
		{
			halfMove = 0;
			castleRights = 0;
			lastCastle = 0;
			passing = 0;
			for (int n = 0; n < tnt.Length; n++)
			{
				char c = tnt[n];
				int piece = char.IsUpper(c) ? colorWhite : colorBlack;
				switch (char.ToLower(c))
				{
					case 'a':
						piece |= piecePawn;
						passing = n - 8;
						break;
					case 'p':
						piece |= piecePawn;
						break;
					case 'n':
						piece |= pieceKnight;
						break;
					case 'b':
						piece |= pieceBishop;
						break;
					case 't':
						piece |= pieceRook;
						switch (n)
						{
							case 63:
								castleRights |= 1;
								break;
							case 56:
								castleRights |= 2;
								break;
							case 7:
								castleRights |= 4;
								break;
							case 0:
								castleRights |= 8;
								break;
						}
						break;
					case 'r':
						piece |= pieceRook;
						break;
					case 'q':
						piece |= pieceQueen;
						break;
					case 'k':
						piece |= pieceKing;
						break;
					default:
						piece = colorEmpty;
						break;
				}
				board[n] = piece;
			}
		}

		int ScorePos(int x,int y)
		{
			int[] ax = new int[8] { 0, 2, 4, 6, 7, 5, 3, 1 };
			int[] ay = new int[8] { 7, 6, 5, 4, 3, 2, 1, 0 };
			return (ay[y] << 3) | ax[x];
		}

		public int MoveScorePos(int emo)
		{
			int fr = MoveFrom(emo);
			int to = MoveTo(emo);
			int fx = SquareX(fr);
			int tx = SquareX(to);
			int fy = SquareY(fr);
			int ty = SquareY(to);
			if (MoveWhite(emo))
				return (ScorePos(tx, ty) - ScorePos(fx, fy));
			return (ScorePos(tx, 7 - ty) - ScorePos(fx, 7 - fy));
		}

		public bool MoveIsBack(int emo)
		{
            if (MoveIsCapture(emo))
                return false;
            if (MoveIsCastling(emo))
				return false;
            if (MoveIsPawn(emo))
                return false;
            return MoveScorePos(emo) <= 0;
		}

		public bool MoveIsForth(int emo)
		{
            if (MoveIsCapture(emo))
                return true;
            if (MoveIsCastling(emo))
				return true;
            if (MoveIsPawn(emo))
                return true;
            return MoveScorePos(emo) > 0;
		}


	}

}
