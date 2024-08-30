using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.Scripts.Engine
{
	internal class Position
	{
		private (int RankStep, int FileStep) NORTH = (1, 0);
		private (int RankStep, int FileStep) NORTH_EAST = (1, 1);
		private (int RankStep, int FileStep) EAST = (0, 1);
		private (int RankStep, int FileStep) SOUTH_EAST = (-1, 1);
		private (int RankStep, int FileStep) SOUTH = (-1, 0);
		private (int RankStep, int FileStep) SOUTH_WEST = (-1, -1);
		private (int RankStep, int FileStep) WEST = (0, -1);
		private (int RankStep, int FileStep) NORTH_WEST = (1, -1);

		private string _activeColor;
		private (int Rank, int File) _blackKing = (0, 0);
		private string _castlingRights = string.Empty;
		private char _from;
		private string _possibleEnPassantTarget = string.Empty;
		private char[,] _squares = new char[8, 8];
		private (int Rank, int File) _whiteKing = (0, 0);
		private char _to;

		public Position(string forsythEdwardsNotation)
		{
			string[] forsythEdwardsNotationParts = forsythEdwardsNotation.Split(" ");

			// piece placement
			string piecePlacement = forsythEdwardsNotationParts[0];
			int piecePlacementIndex = 0;

			for (int rank = 7; rank >= 0; rank--)
			{
				for (int file = 0; file < 8; file++)
				{
					char piece = piecePlacement[piecePlacementIndex];

					_squares[rank, file] = piece;

					if (piece == 'K')
					{
						_whiteKing = (rank, file);
					}
					else if (piece == 'k')
					{
						_blackKing = (rank, file);
					}

					piecePlacementIndex++;
				}

				piecePlacementIndex++; // skip /
			}

			// active color
			_activeColor = forsythEdwardsNotationParts[1];

			// castling rights
			_castlingRights = forsythEdwardsNotationParts[2];

			// possible en passant target
			_possibleEnPassantTarget = forsythEdwardsNotationParts[3];
		}

		public int Evaluate()
		{
			int result = 0;
			int white = 0;
			int black = 0;

			for (int rank = 0; rank < 8; rank++)
			{
				for (int file = 0; file < 8; file++)
				{
					char square = _squares[rank, file];

					if ("KQRNBP".IndexOf(square) >= 0)
					{
						white += GetPieceValue(square);
						white += PieceSquareTables.Scores[square][rank, file];
					}
					else if ("kqrnbp".IndexOf(square) >= 0)
					{
						black += GetPieceValue(square);
						black += PieceSquareTables.Scores[square][rank, file];
					}
				}
			}
			
			result = black - white;

			return result;
		}

		public string GetMove()
		{
			int bestValue = int.MinValue;
			string bestMove = string.Empty;
			char bestPiece = ' ';
			Dictionary<string, List<string>> moves = GetMoves();
			string fen = GetFEN();

			foreach (string from in moves.Keys)
			{
				foreach (string to in moves[from])
				{
					Position position = new Position(fen);

					position.MakeMove(from, to);

					int value = Minimax(position, 2, int.MinValue, int.MaxValue, false);

					if (value >= bestValue)
					{
						bestValue = value;
						bestMove = $"{from}{to}";

						(int rank, int file) = GetRankAndFile(from);

						bestPiece = _squares[rank, file];
					}
				}
			}

			if (bestPiece == 'P'
				&& bestMove.EndsWith("8"))
			{
				bestMove += "Q";
			}
			else if (bestPiece == 'p'
				&& bestMove.EndsWith("1"))
			{
				bestMove += "q";
			}

			return bestMove;
		}

		public Dictionary<string, List<string>> GetMoves()
		{
			Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

			for (int rank = 0; rank < 8; rank++)
			{
				for (int file = 0; file < 8; file++)
				{
					if (string.Compare(_activeColor, "w") == 0)
					{
						switch (_squares[rank, file])
						{
							case 'K':
								GetMovesForKing(rank, file, "Q", "K", result);
								break;

							case 'Q':
								GetMovesForQueen(rank, file, result);
								break;

							case 'R':
								GetMovesForRook(rank, file, result);
								break;

							case 'N':
								GetMovesForKnight(rank, file, result);
								break;

							case 'B':
								GetMovesForBishop(rank, file, result);
								break;

							case 'P':
								GetMovesForPawn(rank, file, NORTH, NORTH_EAST, NORTH_WEST, -1, result);
								break;
						}
					}
					else if (string.Compare(_activeColor, "b") == 0)
					{
						switch (_squares[rank, file])
						{
							case 'k':
								GetMovesForKing(rank, file, "q", "k", result);
								break;

							case 'q':
								GetMovesForQueen(rank, file, result);
								break;

							case 'r':
								GetMovesForRook(rank, file, result);
								break;

							case 'n':
								GetMovesForKnight(rank, file, result);
								break;

							case 'b':
								GetMovesForBishop(rank, file, result);
								break;

							case 'p':
								GetMovesForPawn(rank, file, SOUTH, SOUTH_EAST, SOUTH_WEST, 1, result);
								break;
						}
					}
				}
			}

			return result;
		}

		// helpers
		private void AddMove(int fromRank, int fromFile, int toRank, int toFile, Dictionary<string, List<string>> result)
		{
			string from = GetAlgebraicNotation(fromRank, fromFile);
			string to = GetAlgebraicNotation(toRank, toFile);

			if (!result.ContainsKey(from))
			{
				result.Add(from, new List<string>());
			}

			result[from].Add(to);
		}

		private bool CanMoveTo(char from, char to, out bool stop, bool vertical = false, bool diagonal = false)
		{
			stop = false;

			if (to == '1')
			{
				if ((from == 'P' || from == 'p')
					&& diagonal)
				{
					return false;
				}

				return true;
			}

			if (to == 'K'
				|| to == 'k')
			{
				stop = true;

				return false;
			}

			char fromColor = from == 'K' || from == 'Q' || from == 'R' || from == 'N' || from == 'B' || from == 'P' ? 'w' : 'b';
			char toColor = to == 'K' || to == 'Q' || to == 'R' || to == 'N' || to == 'B' || to == 'P' ? 'w' : 'b';

			if (fromColor == toColor)
			{
				stop = true;

				return false;
			}

			if ((from == 'P' || from == 'p')
				&& vertical)
			{
				stop = true;

				return false;
			}

			stop = true;

			return true;
		}

		private string GetAlgebraicNotation(int rank, int file)
		{
			string result = $"{"abcdefgh".Substring(file, 1)}{(rank + 1)}";

			return result;
		}

		private string GetFEN()
		{
			StringBuilder result = new StringBuilder();

			// piece placement
			for (int rank = 7; rank >= 0; rank--)
			{
				for (int file = 0; file < 8; file++)
				{
					result.Append(_squares[rank, file]);
				}

				if (rank != 0)
				{
					result.Append("/");
				}
			}

			// active color
			result.Append($" {_activeColor}");

			// castling rights
			if (string.IsNullOrEmpty(_castlingRights))
			{
				result.Append(" -");
			}
			else
			{
				result.Append($" {_castlingRights}");
			}

			// possible en passant target
			result.Append($" {_possibleEnPassantTarget}");

			return result.ToString();
		}

		private int GetPieceValue(char piece)
		{
			switch (piece.ToString().ToUpper())
			{
				case "K":
					return 20000;

				case "Q":
					return 900;

				case "R":
					return 500;

				case "B":
					return 330;

				case "N":
					return 320;
				
				case "P":
					return 100;

				default:
					return 0;
			}
		}

		private (int rank, int file) GetRankAndFile(string algebraicNotation)
		{
			int rank = int.Parse(algebraicNotation[1].ToString()) - 1;
			int file = "abcdefgh".IndexOf(algebraicNotation[0]);

			return (rank, file);
		}

		private void KingMoved(int rank, int file)
		{
			if (string.Compare(_activeColor, "w") == 0)
			{
				_whiteKing = (rank, file);
			}
			else if (string.Compare(_activeColor, "b") == 0)
			{
				_blackKing = (rank, file);
			}
		}

		private void MakeMove(string from, string to)
		{
			(int rankFrom, int fileFrom) = GetRankAndFile(from);
			(int rankTo, int fileTo) = GetRankAndFile(to);

			_from = _squares[rankFrom, fileFrom];
			_to = _squares[rankTo, fileTo];

			_squares[rankTo, fileTo] = _from;
			_squares[rankFrom, fileFrom] = '1';

			if (string.Compare(_from.ToString().ToUpper(), "K") == 0)
			{
				KingMoved(rankTo, fileTo);
			}

			_activeColor = string.Compare(_activeColor, "w") == 0 ? "b" : "w";
		}

		private int Minimax(Position position, int depth, int alpha, int beta, bool isMaximizingPlayer)
		{
			if (depth == 0)
			{
				int result = position.Evaluate();

				return result;
			}

			if (isMaximizingPlayer)
			{
				int bestValue = int.MinValue;
				Dictionary<string, List<string>> moves = position.GetMoves();
				string fen = position.GetFEN();

				foreach (string from in moves.Keys)
				{
					foreach (string to in moves[from])
					{
						Position newPosition = new Position(fen);

						newPosition.MakeMove(from, to);

						int value = Minimax(newPosition, depth - 1, alpha, beta, false);

						bestValue = Math.Max(value, bestValue);
						alpha = Math.Max(alpha, value);

						if (beta <= alpha)
						{
							break;
						}
					}
				}

				return bestValue;
			}
			else
			{
				int bestValue = int.MaxValue;
				Dictionary<string, List<string>> moves = position.GetMoves();
				string fen = position.GetFEN();

				foreach (string from in moves.Keys)
				{
					foreach (string to in moves[from])
					{
						Position newPosition = new Position(fen);

						newPosition.MakeMove(from, to);
						
						int value = Minimax(newPosition, depth - 1, alpha, beta, true); // todo: position or newPosition?

						bestValue = Math.Min(value, bestValue);
						beta = Math.Min(beta, value);

						if (beta <= alpha)
						{
							break;
						}
					}
				}

				return bestValue;
			}
		}

		// check
		private bool InCheck(int rankFrom, int fileFrom, int rankTo, int fileTo)
		{
			bool result = false;
			char pieceFrom = _squares[rankFrom, fileFrom];
			char pieceTo = _squares[rankTo, fileTo];
			List<char> verticalChecks = new List<char>();
			List<char> horizontalChecks = new List<char>();
			List<char> diagonalChecks = new List<char>();
			List<char> knightChecks = new List<char>();

			_squares[rankFrom, fileFrom] = '1';
			_squares[rankTo, fileTo] = pieceFrom;

			if (string.Compare(pieceFrom.ToString().ToUpper(), "K") == 0)
			{
				KingMoved(rankTo, fileTo);
			}

			(int rank, int file) = (0, 0);

			if (pieceFrom == 'K' 
				|| pieceFrom == 'Q' 
				|| pieceFrom == 'R' 
				|| pieceFrom == 'N' 
				|| pieceFrom == 'B' 
				|| pieceFrom == 'P')
			{
				rank = _whiteKing.Rank;
				file = _whiteKing.File;
			}
			else if (pieceFrom == 'k' 
				|| pieceFrom == 'q' 
				|| pieceFrom == 'r' 
				|| pieceFrom == 'n' 
				|| pieceFrom == 'b' 
				|| pieceFrom == 'p')
			{
				rank = _blackKing.Rank;
				file = _blackKing.File;
			}

			char piece = _squares[rank, file];

			if (string.Compare(_activeColor, "w") == 0)
			{
				verticalChecks.AddRange(new[] { 'q', 'r' });
				horizontalChecks.AddRange(new[] { 'q', 'r' });
				diagonalChecks.AddRange(new[] { 'q', 'b', 'p' });
				knightChecks.AddRange(new[] { 'n' });
			}
			else if (string.Compare(_activeColor, "b") == 0)
			{
				verticalChecks.AddRange(new[] { 'Q', 'R' });
				horizontalChecks.AddRange(new[] { 'Q', 'R' });
				diagonalChecks.AddRange(new[] { 'Q', 'B', 'P' });
				knightChecks.AddRange(new[] { 'N' });
			}

			if (InCheck(rank, file, NORTH, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, NORTH_EAST, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, EAST, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, SOUTH_EAST, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, SOUTH, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, SOUTH_WEST, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, WEST, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, NORTH_WEST, piece, verticalChecks, horizontalChecks, diagonalChecks)
				|| InCheck(rank, file, NORTH, piece)
				|| InCheck(rank, file, NORTH_EAST, piece)
				|| InCheck(rank, file, EAST, piece)
				|| InCheck(rank, file, SOUTH_EAST, piece)
				|| InCheck(rank, file, SOUTH, piece)
				|| InCheck(rank, file, SOUTH_WEST, piece)
				|| InCheck(rank, file, WEST, piece)
				|| InCheck(rank, file, NORTH_WEST, piece))
			{
				result = true;
				goto Finish;
			}

			// knights
			List<(int rank, int file)> coordinates = new List<(int rank, int file)>();

			coordinates.Add((rank + 2, file + 1));
			coordinates.Add((rank + 1, file + 2));
			coordinates.Add((rank - 1, file + 2));
			coordinates.Add((rank - 2, file + 1));

			coordinates.Add((rank - 2, file - 1));
			coordinates.Add((rank - 1, file - 2));
			coordinates.Add((rank + 1, file - 2));
			coordinates.Add((rank + 2, file - 1));

			foreach ((int rank, int file) coordinate in coordinates)
			{
				if ((coordinate.rank >= 0 && coordinate.rank < 8)
					&& (coordinate.file >= 0 && coordinate.file < 8)
					&& knightChecks.Contains(_squares[coordinate.rank, coordinate.file]))
				{
					result = true;
					break;
				}
			}

			Finish:

			_squares[rankFrom, fileFrom] = pieceFrom;
			_squares[rankTo, fileTo] = pieceTo;

			if (string.Compare(pieceFrom.ToString().ToUpper(), "K") == 0)
			{
				KingMoved(rankFrom, fileFrom);
			}

			return result;
		}

		private bool InCheck(int rank, int file, (int RankStep, int FileStep) direction, char piece, List<char> verticalChecks, List<char> horizontalChecks, List<char> diagonalChecks)
		{
			bool result = false;
			int r = rank + direction.RankStep;
			int f = file + direction.FileStep;
			bool vertical = direction.FileStep == 0;
			bool horizontal = direction.RankStep == 0;
			bool diagonal = !vertical && !horizontal;
			bool northEast = direction.RankStep == 1 && direction.FileStep == 1;
			bool southEast = direction.RankStep == -1 && direction.FileStep == 1;
			bool southWest = direction.RankStep == -1 && direction.FileStep == -1;
			bool northWest = direction.RankStep == 1 && direction.FileStep == -1;

			while ((r >= 0 && r < 8)
				&& (f >= 0 && f < 8))
			{
				if (_squares[r, f] != '1')
				{
					if (vertical
						&& verticalChecks.Contains(_squares[r, f]))
					{
						result = true;
					}
					else if (horizontal
						&& horizontalChecks.Contains(_squares[r, f]))
					{
						result = true;
					}
					else if (diagonal
						&& diagonalChecks.Contains(_squares[r, f]))
					{
						if (northEast
							|| northWest)
						{
							if (piece == 'k'
								&& _squares[r, f] == 'P')
							{
								// black king can only be checked by white pawns south east and south west
							}
							else if (piece == 'K'
								&& _squares[r, f] == 'p')
							{
								// white king can only be checked by black pawns north east and north west
								result = r == rank + 1
									&& f == file + (northEast ? 1 : -1);
							}
							else
							{
								result = true;
							}
						}
						else if (southEast
							|| southWest)
						{
							if (piece == 'K'
								&& _squares[r, f] == 'p')
							{
								// white king can only be checked by black pawns north east and north west
							}
							else if (piece == 'k'
								&& _squares[r, f] == 'P')
							{
								// black king can only be checked by white pawns south east and south west
								result = r == rank - 1
									&& f == file + (southEast ? 1 : -1);
							}
							else
							{
								result = true;
							}
						}
					}

					break;
				}

				if (result)
				{
					break;
				}

				r += direction.RankStep;
				f += direction.FileStep;
			}

			return result;
		}

		private bool InCheck(int rank, int file, (int RankStep, int FileStep) direction, char piece)
		{
			bool result = false;
			int r = rank + direction.RankStep;
			int f = file + direction.FileStep;

			while ((r >= 0 && r < 8)
				&& (f >= 0 && f < 8))
			{
				if ((piece =='K' && _squares[r, f] == 'k')
					|| (piece == 'k' && _squares[r, f] == 'K'))
				{
					result = true;
				}

				break;
			}

			return result;
		}

		// moves
		private void GetMoves(int rank, int file, (int RankStep, int FileStep) direction, Dictionary<string, List<string>> moves, int limit = int.MaxValue)
		{
			int count = 0;
			bool stop = false;
			int r = rank + direction.RankStep;
			int f = file + direction.FileStep;
			bool vertical = direction.FileStep == 0;
			bool diagonal = direction.RankStep != 0 && direction.FileStep != 0;

			while ((r >= 0 && r < 8)
				&& (f >= 0 && f < 8))
			{
				if (CanMoveTo(_squares[rank, file], _squares[r, f], out stop, vertical, diagonal))
				{
					if (!InCheck(rank, file, r, f))
					{
						AddMove(rank, file, r, f, moves);
					}
				}

				count++;
				r += direction.RankStep;
				f += direction.FileStep;

				if (count == limit
					|| stop)
				{
					break;
				}
			}
		}

		private void GetMovesForBishop(int rank, int file, Dictionary<string, List<string>> moves)
		{
			GetMoves(rank, file, NORTH_EAST, moves);
			GetMoves(rank, file, SOUTH_EAST, moves);
			GetMoves(rank, file, SOUTH_WEST, moves);
			GetMoves(rank, file, NORTH_WEST, moves);
		}

		private void GetMovesForKing(int rank, int file, string queen, string king, Dictionary<string, List<string>> moves)
		{
			GetMoves(rank, file, NORTH, moves, 1);
			GetMoves(rank, file, NORTH_EAST, moves, 1);
			GetMoves(rank, file, EAST, moves, 1);
			GetMoves(rank, file, SOUTH_EAST, moves, 1);
			GetMoves(rank, file, SOUTH, moves, 1);
			GetMoves(rank, file, SOUTH_WEST, moves, 1);
			GetMoves(rank, file, WEST, moves, 1);
			GetMoves(rank, file, NORTH_WEST, moves, 1);

			// castling queenside
			if (_castlingRights.IndexOf(queen) >= 0
				&& !InCheck(rank, 4, rank, 4)
				&& _squares[rank, 3] == '1'
				&& !InCheck(rank, 4, rank, 3)
				&& _squares[rank, 2] == '1'
				&& !InCheck(rank, 4, rank, 2)
				&& _squares[rank, 1] == '1')
			{
				AddMove(rank, 4, rank, 2, moves);
			}

			// castling kingside
			if (_castlingRights.IndexOf(king) >= 0
				&& !InCheck(rank, 4, rank, 4)
				&& _squares[rank, 5] == '1'
				&& !InCheck(rank, 4, rank, 5)
				&& _squares[rank, 6] == '1'
				&& !InCheck(rank, 4, rank, 6))
			{
				AddMove(rank, 4, rank, 6, moves);
			}
		}

		private void GetMovesForKnight(int rank, int file, Dictionary<string, List<string>> moves)
		{
			List<(int rank, int file)> coordinates = new List<(int, int)>();

			coordinates.Add((rank + 2, file + 1));
			coordinates.Add((rank + 1, file + 2));
			coordinates.Add((rank - 1, file + 2));
			coordinates.Add((rank - 2, file + 1));

			coordinates.Add((rank - 2, file - 1));
			coordinates.Add((rank - 1, file - 2));
			coordinates.Add((rank + 1, file - 2));
			coordinates.Add((rank + 2, file - 1));

			foreach ((int rank, int file) coordinate in coordinates)
			{
				if (coordinate.rank >= 0
					&& coordinate.rank < 8
					&& coordinate.file >= 0
					&& coordinate.file < 8)
				{
					bool stop;

					if (CanMoveTo(_squares[rank, file], _squares[coordinate.rank, coordinate.file], out stop))
					{
						if (!InCheck(rank, file, coordinate.rank, coordinate.file))
						{
							AddMove(rank, file, coordinate.rank, coordinate.file, moves);
						}
					}
				}
			}
		}

		private void GetMovesForPawn(int rank, int file, (int Rank, int File) vertical, (int Rank, int File) verticalEast, (int Rank, int File) verticalWest, int rankOffset,  Dictionary<string, List<string>> moves)
		{
			GetMoves(rank, file, vertical, moves, rank == 1 || rank == 6 ? 2 : 1);
			GetMoves(rank, file, verticalEast, moves, 1);
			GetMoves(rank, file, verticalWest, moves, 1);

			// en passant
			if (string.Compare(_possibleEnPassantTarget, "-") != 0)
			{
				(int r, int f) = GetRankAndFile(_possibleEnPassantTarget);

				if ((r + rankOffset == rank && f + 1 == file)
					|| (r + rankOffset == rank && f - 1 == file))
				{
					AddMove(rank, file, r, f, moves);
				}
			}
		}

		private void GetMovesForQueen(int rank, int file, Dictionary<string, List<string>> moves)
		{
			GetMoves(rank, file, NORTH, moves);
			GetMoves(rank, file, NORTH_EAST, moves);
			GetMoves(rank, file, EAST, moves);
			GetMoves(rank, file, SOUTH_EAST, moves);
			GetMoves(rank, file, SOUTH, moves);
			GetMoves(rank, file, SOUTH_WEST, moves);
			GetMoves(rank, file, WEST, moves);
			GetMoves(rank, file, NORTH_WEST, moves);
		}

		private void GetMovesForRook(int rank, int file, Dictionary<string, List<string>> moves)
		{
			GetMoves(rank, file, NORTH, moves);
			GetMoves(rank, file, EAST, moves);
			GetMoves(rank, file, SOUTH, moves);
			GetMoves(rank, file, WEST, moves);
		}
	}
}