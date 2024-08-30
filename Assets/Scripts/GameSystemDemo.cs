using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Assets.Scripts
{
	internal class GameSystemDemo : GameSystem
	{
		// pgn-extract.exe .\kasparov_topalov_1999.pgn -Wlalg --nocomments
		// replace \r\n with space

		// Garry Kasparov vs Veselin Topalov
		// Veselin Topalov resigned
		private const string GAME_1_TITLE = "Garry Kasparov vs Veselin Topalov";
		private const string GAME_1 = "1. e2e4 d7d6 2. d2d4 g8f6 3. b1c3 g7g6 4. c1e3 f8g7 5. d1d2 c7c6 6. f2f3 b7b5 7. g1e2 b8d7 8. e3h6 g7h6 9. d2h6 c8b7 10. a2a3 e7e5 11. e1c1 d8e7 12. c1b1 a7a6 13. e2c1 e8c8 14. c1b3 e5d4 15. d1d4 c6c5 16. d4d1 d7b6 17. g2g3 c8b8 18. b3a5 b7a8 19. f1h3 d6d5 20. h6f4+ b8a7 21. h1e1 d5d4 22. c3d5 b6d5 23. e4d5 e7d6 24. d1d4 c5d4 25. e1e7+ a7b6 26. f4d4+ b6a5 27. b2b4+ a5a4 28. d4c3 d6d5 29. e7a7 a8b7 30. a7b7 d5c4 31. c3f6 a4a3 32. f6a6+ a3b4 33. c2c3+ b4c3 34. a6a1+ c3d2 35. a1b2+ d2d1 36. h3f1 d8d2 37. b7d7 d2d7 38. f1c4 b5c4 39. b2h8 d7d3 40. h8a8 c4c3 41. a8a4+ d1e1 42. f3f4 f7f5 43. b1c1 d3d2 44. a4a7";

		// Donald Byrne vs Robert James Fischer
		// Robert James Fischer won
		private const string GAME_2_TITLE = "Donald Byrne vs Robert James Fischer";
		private const string GAME_2 = "1. g1f3 g8f6 2. c2c4 g7g6 3. b1c3 f8g7 4. d2d4 e8g8 5. c1f4 d7d5 6. d1b3 d5c4 7. b3c4 c7c6 8. e2e4 b8d7 9. a1d1 d7b6 10. c4c5 c8g4 11. f4g5 b6a4 12. c5a3 a4c3 13. b2c3 f6e4 14. g5e7 d8b6 15. f1c4 e4c3 16. e7c5 f8e8+ 17. e1f1 g4e6 18. c5b6 e6c4+ 19. f1g1 c3e2+ 20. g1f1 e2d4+ 21. f1g1 d4e2+ 22. g1f1 e2c3+ 23. f1g1 a7b6 24. a3b4 a8a4 25. b4b6 c3d1 26. h2h3 a4a2 27. g1h2 d1f2 28. h1e1 e8e1 29. b6d8+ g7f8 30. f3e1 c4d5 31. e1f3 f2e4 32. d8b8 b7b5 33. h3h4 h7h5 34. f3e5 g8g7 35. h2g1 f8c5+ 36. g1f1 e4g3+ 37. f1e1 c5b4+ 38. e1d1 d5b3+ 39. d1c1 g3e2+ 40. c1b1 e2c3+ 41. b1c1 a2c2";

		// Alexander Beliavsky vs John Nunn
		// Alexander Beliavsky resigned
		private const string GAME_3_TITLE = "Alexander Beliavsky vs John Nunn";
		private const string GAME_3 = "1. d2d4 g8f6 2. c2c4 g7g6 3. b1c3 f8g7 4. e2e4 d7d6 5. f2f3 e8g8 6. c1e3 b8d7 7. d1d2 c7c5 8. d4d5 d7e5 9. h2h3 f6h5 10. e3f2 f7f5 11. e4f5 f8f5 12. g2g4 f5f3 13. g4h5 d8f8 14. c3e4 g7h6 15. d2c2 f8f4 16. g1e2 f3f2 17. e4f2 e5f3+ 18. e1d1 f4h4 19. f2d3 c8f5 20. e2c1 f3d2 21. h5g6 h7g6 22. f1g2 d2c4 23. c2f2 c4e3+ 24. d1e2 h4c4 25. g2f3 a8f8 26. h1g1 e3c2 27. e2d1 f5d3";

		private Queue<string> _whiteMoves = new Queue<string>();
		private Queue<string> _blackMoves = new Queue<string>();

		new private void Awake()
		{
			if (base._isDemoMode)
			{
				GetMoves(GAME_1);
			}

			base.Awake();
		}

		protected override void GetMovesForWhite()
		{
			if (base._isDemoMode)
			{
				StartCoroutine(GetMovesForWhiteCoroutine());
			}
			else
			{
				base.GetMovesForWhite();
			}
		}

		private IEnumerator GetMovesForWhiteCoroutine()
		{
			base._statusText.text = "White's move";

			float extraDelay = base._isDemoMode && base._isFirstMove ? 20F : 0F;

			yield return new WaitForSeconds(1F + extraDelay);

			base._isFirstMove = false;

			string forsythEdwardsNotation = base.GetForsythEdwardsNotation();

			base._moves.Clear();
			base._moves = base._chess.GetMoves(forsythEdwardsNotation);

			if (base._moves.Count == 0)
			{
				base._statusText.text = "Black won";
				base.State = State.GameOver;
			}
			else
			{
				if (_whiteMoves.Count == 0)
				{
					base._statusText.text = "White resigned";
					base.State = State.GameOver;
				}
				else
				{
					string move = _whiteMoves.Dequeue();
					string from = move.Substring(0, 2);
					string to = move.Substring(2, 2);

					base.SquareSelected(from);
					base.SquareSelected(to);
				}
			}
		}

		protected override void MakeMoveForBlack()
		{
			if (base._isDemoMode)
			{
				StartCoroutine(MakeMoveForBlackCoroutine());
			}
			else
			{
				base.MakeMoveForBlack();
			}
		}

		private IEnumerator MakeMoveForBlackCoroutine()
		{
			base._statusText.text = "Black's move";

			yield return new WaitForSeconds(1F);

			string forsythEdwardsNotation = base.GetForsythEdwardsNotation();

			base._moves.Clear();
			base._moves = base._chess.GetMoves(forsythEdwardsNotation);

			if (base._moves.Count == 0)
			{
				base._statusText.text = "White won";
				base.State = State.GameOver;
			}
			else
			{
				if (_blackMoves.Count == 0)
				{
					base._statusText.text = "Black resigned";
					base.State = State.GameOver;
				}
				else
				{
					string move = _blackMoves.Dequeue();
					string from = move.Substring(0, 2);
					string to = move.Substring(2, 2);

					base.SquareSelected(from);
					base.SquareSelected(to);
				}
			}
		}

		protected override void MoveHelper(Square toSquare)
		{
			base.MoveHelper(toSquare);

			// new game, take back
			if (!base._isDemoMode)
			{
				bool interactable = (base._history.Count > 0 && base._history.Count % 2 == 0)
					|| base.State == State.GameOver;

				base._newGame.interactable = interactable;
				base._takeBack.interactable = interactable;
			}
		}

		private void GetMoves(string game)
		{
			string[] moves = Regex.Split(game, @"\d+\.");

			for (int i = 0; i < moves.Length; i++)
			{
				string move = moves[i].Trim();
				move = move.Replace("+", "");

				if (string.IsNullOrEmpty(move))
				{
					continue;
				}

				string[] halfMoves = move.Split(" ");

				_whiteMoves.Enqueue(halfMoves[0]);

				if (halfMoves.Length > 1)
				{
					_blackMoves.Enqueue(halfMoves[1]);
				}
			}
		}
	}
}