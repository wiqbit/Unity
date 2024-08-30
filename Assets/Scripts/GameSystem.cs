using Assets.Scripts.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
	internal enum State
	{
		SetupBoard,
		GetMovesForWhite,
		WaitingForMove,
		Move,
		MakeMoveForBlack,
		PromotePawn,
		PromotedPawnRemove,
		PromotedPawnAdd,
		GameOver
	}

	internal enum Color
	{
		w,
		b
	}

	internal class GameSystem : Base
	{
		private const string FORSYTH_EDWARDS_NOTATION_NEW_GAME = "rnbqkbnr/pppppppp/11111111/11111111/11111111/11111111/PPPPPPPP/RNBQKBNR w KQkq -";
		
		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_KINGSIDE_WHITE =	"rnbqkbnr/pppppppp/11111111/11111111/11111111/11111NP1/PPPPPPBP/RNBQK11R w KQkq -";
		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_QUEENSIDE_WHITE =	"rnbqkbnr/pppppppp/11111111/11111111/11111111/11NPB111/PPPQPPPP/R111KBNR w KQkq -";
		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_KINGSIDE_BLACK =	"rnbqk11r/ppppppbp/11111np1/11111111/11111111/11111111/PPPPPPPP/RNBQKBNR b KQkq -";
		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_QUEENSIDE_BLACK =	"r111kbnr/pppqpppp/11npb111/11111111/11111111/11111111/PPPPPPPP/RNBQKBNR b KQkq -";
		
		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_KINGSIDE_IN_CHECK = "rnb1kbnr/pppppppp/11111111/11111111/1q111111/11111111/PPP1PPPP/RNBQK11R w KQkq -";
		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_QUEENSIDE_IN_CHECK = "rnb1kbnr/pppppppp/11111111/11111111/1q111111/11111111/PPP1PPPP/R111KBNR w KQkq -";

		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_KINGSIDE_THROUGH_CHECK = "rnb1kbnr/pppppppp/11111111/11111111/11qb1111/11111111/PPPP11PP/RNBQK11R w KQkq -";
		private const string FORSYTH_EDWARDS_NOTATION_CASTLE_QUEENSIDE_THROUGH_CHECK = "rnb1kbnr/pppppppp/11111111/11111111/11111bq1/11111111/PPP11PPP/R111KBNR w KQkq -";

		private const string FORSYTH_EDWARDS_NOTATION_EN_PASSANT_WHITE_NORTHEAST = "rnbqkbnr/pppp1ppp/11111111/111Pp111/11111111/11111111/PPP1PPPP/RNBQKBNR w KQkq e6";
		private const string FORSYTH_EDWARDS_NOTATION_EN_PASSANT_WHITE_NORTHWEST = "rnbqkbnr/ppp1pppp/11111111/111pP111/11111111/11111111/PPP1PPPP/RNBQKBNR w KQkq d6";
		private const string FORSYTH_EDWARDS_NOTATION_EN_PASSANT_BLACK_SOUTHEAST = "rnbqkbnr/ppp1pppp/11111111/11111111/111pP111/11111111/PPPP1PPP/RNBQKBNR b KQkq e3";
		private const string FORSYTH_EDWARDS_NOTATION_EN_PASSANT_BLACK_SOUTHWEST = "rnbqkbnr/pppp1ppp/11111111/11111111/111Pp111/11111111/PPP1PPPP/RNBQKBNR b KQkq d3";

		private const string FORSYTH_EDWARDS_NOTATION_PAWN_PROMOTION_WHITE = "1111111k/111P1111/11K11111/11111111/11111111/11111111/11111111/11111111 w - -";
		private const string FORSYTH_EDWARDS_NOTATION_PAWN_PROMOTION_BLACK = "11111111/11111111/11111111/11111111/11111111/11k11111/111p1111/1111111K b - -";

		private string _forsythEdwardsNotation = FORSYTH_EDWARDS_NOTATION_NEW_GAME;

		public GameObject _a1 = null;
		protected Color _activeColor = Color.w;
		public AudioSource _backgroundMusic = null;
		private List<int> _backgroundMusicHistory = new List<int>();
		public Toggle _backgroundMusicToggle = null;
		public GameObject _camera = null;
		private string _castlingRights = "-";
		protected Chess _chess = new Chess();
		private Square _fromSquare = null;
		protected List<Move> _history = new List<Move>();
		private static GameSystem _instance = null;
		private object _instanceLock = new object();
		public bool _isDemoMode = false;
		protected bool _isFirstMove = true;
		public TMPro.TextMeshProUGUI _lastMovesBlackText = null;
		public TMPro.TextMeshProUGUI _lastMovesWhiteText = null;
		protected Dictionary<string, List<string>> _moves = new Dictionary<string, List<string>>();
		public Button _newGame = null;
		public GameObject _pawnPromotionCamera = null;
		public AudioClip _pieceDown = null;
		public AudioClip _pieceSlide = null;
		public GameObject _plane = null;
		private string _possibleEnPassantTarget = "-";
		private System.Random _random = new System.Random(((int)DateTime.Now.Ticks & 0x0000FFFF));
		public AudioSource _soundEffects = null;
		public Toggle _soundEffectsToggle = null;
		private Square[,] _squares = new Square[9, 9];
		private State _state = State.SetupBoard;
		public TMPro.TextMeshProUGUI _statusText = null;
		public Button _takeBack = null;
		private Queue<Square> _toSquares = new Queue<Square>();

		protected void Awake()
		{
			lock (_instanceLock)
			{
				if (_instance == null)
				{
					_instance = this;

					Camera camera = _instance._camera.GetComponent<Camera>();
					_instance.AdjustCameraFieldOfViewBasedOnScreenSize(camera);

					Camera pawnPromotionCamera = _instance._pawnPromotionCamera.GetComponent<Camera>();
					_instance.AdjustCameraFieldOfViewBasedOnScreenSize(pawnPromotionCamera);

					if (_isDemoMode
						&& _isFirstMove)
					{
						Invoke("PlayBackgroundMusic", 20F);
					}
					else
					{
						_instance.PlayBackgroundMusic();
					}

					_instance.DoWork();
				}
			}
		}

		private void DoWork()
		{
			switch (State)
			{
				case State.SetupBoard:
					SetupBoard();
					break;

				case State.GetMovesForWhite:
					GetMovesForWhite();
					break;

				case State.Move:
					Move();
					break;

				case State.MakeMoveForBlack:
					MakeMoveForBlack();
					break;

				case State.PromotePawn:
					PromotePawn();
					break;
			}
		}

		private void SetupBoard()
		{
			MeshRenderer meshRenderer = _plane.GetComponent<MeshRenderer>();
			Bounds bounds = meshRenderer.bounds;
			float boardWidth = bounds.max.x * 2F;
			float squareWidth = boardWidth / 8F;
			float boardDepth = bounds.max.z * 2F;
			float squareDepth = boardDepth / 8F;

			float y = _a1.transform.position.y;
			float z = _a1.transform.position.z;
			string[] forsythEdwardsNotationParts = _forsythEdwardsNotation.Split(" ");

			_possibleEnPassantTarget = forsythEdwardsNotationParts[3];
			_castlingRights = forsythEdwardsNotationParts[2];
			_activeColor = Enum.Parse<Color>(forsythEdwardsNotationParts[1]);

			string piecePlacement = forsythEdwardsNotationParts[0];
			int piecePlacementIndex = piecePlacement.Length - 8;

			for (int rank = 1; rank <= 8; rank++)
			{
				float x = _a1.transform.position.x;

				for (int file = 1; file <= 8; file++)
				{
					GameObject surface = null;
					Vector3 position = new Vector3(x, y, z);

					if (rank == 1
						&& file == 1)
					{
						surface = GameObject.Find("a1");
					}
					else
					{
						surface = Instantiate(_a1);
						surface.name = GetAlgebraicNotation(rank, file);
						surface.transform.position = position;
					}
					
					string forsythEdwardsNotation = piecePlacement.Substring(piecePlacementIndex, 1);
					GameObject piece = InstantiatePiece(forsythEdwardsNotation, file, surface, false);

					SetSquare(surface.name, surface, piece);

					x -= squareWidth;
					piecePlacementIndex++;
				}

				z -= squareDepth;
				piecePlacementIndex -= 17;
			}

			State = _activeColor == Color.w ? State.GetMovesForWhite : State.MakeMoveForBlack;
		}

		protected virtual void GetMovesForWhite()
		{
			StartCoroutine(GetMovesForWhiteCoroutine());
		}

		private void Move()
		{
			PieceController pieceController = _fromSquare.Piece.GetComponent<PieceController>();
			Square toSquare = _toSquares.Dequeue();

			if (_toSquares.Count == 0)
			{
				Move move = new Move() { Color = _activeColor, FromAlgebraicNotation = _fromSquare.Surface.name, ToAlgebraicNotation = toSquare.Surface.name, CapturedForsythEdwardsNotation = toSquare.ForsythEdwardsNotation, PawnPromotionForsythEdwardsNotation = "", CastlingRights = _castlingRights, PossibleEnPassantTarget = _possibleEnPassantTarget };

				_history.Add(move);
				MoveHelper(toSquare);

				toSquare.Piece = _fromSquare.Piece;
				_fromSquare.Piece = null;
			}

			PlayPieceSlide();

			pieceController.Move(toSquare, PieceController.MoveCallback.Moved);
		}

		protected virtual void MakeMoveForBlack()
		{
			StartCoroutine(MakeMoveForBlackCoroutine());
		}

		private void PromotePawn()
		{
			StartCoroutine(PromotePawnCoroutine());
		}

		// properties
		public static GameSystem Instance
		{
			get { return _instance; }
		}

		protected State State
		{
			get { return _state; }
			set
			{
				_state = value;
				DoWork();
			}
		}

		// helpers
		private void AdjustCameraFieldOfViewBasedOnScreenSize(Camera camera)
		{
			float horizontalFieldOfView = 70F;
			float horizontalFieldOfViewInRadians = horizontalFieldOfView * Mathf.Deg2Rad;
			float verticalFieldOfViewInRadians = 2F * Mathf.Atan(Mathf.Tan(horizontalFieldOfViewInRadians / 2F) / camera.aspect);
			float verticalFieldOfView = verticalFieldOfViewInRadians * Mathf.Rad2Deg;

			camera.fieldOfView = verticalFieldOfView;
		}

		public void BackgroundMusicToggleChanged(bool value)
		{
			_backgroundMusic.mute = !_backgroundMusicToggle.isOn;
		}

		protected string GetForsythEdwardsNotation()
		{
			StringBuilder result = new StringBuilder();

			// piece placement
			for (int rank = 8; rank >= 1; rank--)
			{
				if (rank < 8)
				{
					result.Append("/");
				}

				for (int file = 1; file <= 8; file++)
				{
					result.Append(_squares[rank, file].ForsythEdwardsNotation);
				}
			}

			// active color
			result.Append($" {_activeColor}");

			// castling rights
			result.Append($" {_castlingRights}");

			// possible en passant target
			result.Append($" {_possibleEnPassantTarget}");

			return result.ToString();
		}

		private IEnumerator GetMovesForWhiteCoroutine()
		{
			yield return 0;

			_statusText.text = "Your move";

			string forsythEdwardsNotation = GetForsythEdwardsNotation();

			_moves.Clear();
			_moves = _chess.GetMoves(forsythEdwardsNotation);

			if (_moves.Count == 0)
			{
				_statusText.text = "Black won";
				State = State.GameOver;
			}
			else
			{
				State = State.WaitingForMove;
			}
		}

		private Square GetSquare(string algebraicNotation)
		{
			GetRankAndFile(algebraicNotation, out int rank, out int file);

			return _squares[rank, file];
		}

		private IEnumerator MakeMoveForBlackCoroutine()
		{
			_statusText.text = "Black's move";

			yield return 0;

			string forsythEdwardsNotation = GetForsythEdwardsNotation();

			_moves.Clear();
			_moves = _chess.GetMoves(forsythEdwardsNotation);

			if (_moves.Count == 0)
			{
				_statusText.text = "You won";
				State = State.GameOver;
			}
			else
			{
				string move = _chess.GetMove(forsythEdwardsNotation);
				string from = move.Substring(0, 2);
				string to = move.Substring(2, 2);

				SquareSelected(from);
				SquareSelected(to);
			}
		}

		public void Moved()
		{
			if (_toSquares.Count == 0)
			{
				Move move = _history.Last();
				string lastMove = $"{(_activeColor == Color.w ? "White" : "Black")}: N/A";

				if (move != null)
				{
					lastMove = $"{(_activeColor == Color.w ? "White" : "Black")}: {move.FromAlgebraicNotation}:{move.ToAlgebraicNotation} {move.PawnPromotionForsythEdwardsNotation}";
				}

				(_activeColor == Color.w ? _lastMovesWhiteText : _lastMovesBlackText).text = lastMove;

				_fromSquare?.Unselect();
				_fromSquare = null;

				Square toSquare = GetSquare(move.ToAlgebraicNotation);

				if (toSquare.IsKnight)
				{
					GameObject piece = InstantiatePiece(toSquare.ForsythEdwardsNotation, toSquare.File, toSquare.Surface, false);

					Destroy(toSquare.Piece);

					toSquare.Piece = piece;
				}

				if (State != State.PromotePawn)
				{
					_activeColor = _activeColor == Color.w ? Color.b : Color.w;
					State = _activeColor == Color.w ? State.GetMovesForWhite : State.MakeMoveForBlack;
				}
			}
			else if (State != State.PromotePawn)
			{
				State = State.Move;
			}
		}

		protected virtual void MoveHelper(Square toSquare)
		{
			// capture
			if (toSquare.Piece != null)
			{
				PlayPieceDown();

				PieceController pieceController = toSquare.Piece.gameObject.GetComponent<PieceController>();

				pieceController.Remove(false);
			}

			// castling
			if (_fromSquare.IsKing)
			{
				if (string.Compare(_fromSquare.Surface.name, "e1") == 0
					|| string.Compare(_fromSquare.Surface.name, "e8") == 0)
				{
					if (string.Compare(toSquare.Surface.name, "g1") == 0
						|| string.Compare(toSquare.Surface.name, "g8") == 0)
					{
						// castled kingside
						Square fromSquareRook = GetSquare(_fromSquare.IsWhite ? "h1" : "h8");
						Square toSquareRook = GetSquare(_fromSquare.IsWhite ? "f1" : "f8");
						PieceController pieceController = fromSquareRook.Piece.GetComponent<PieceController>();

						pieceController.Move(toSquareRook, PieceController.MoveCallback.None);

						toSquareRook.Piece = fromSquareRook.Piece;
						fromSquareRook.Piece = null;
					}
					else if (string.Compare(toSquare.Surface.name, "c1") == 0
						|| string.Compare(toSquare.Surface.name, "c8") == 0)
					{
						// castled queenside
						Square fromSquareRook = GetSquare(_fromSquare.IsWhite ? "a1" : "a8");
						Square toSquareRook = GetSquare(_fromSquare.IsWhite ? "d1" : "d8");
						PieceController pieceController = fromSquareRook.Piece.GetComponent<PieceController>();

						pieceController.Move(toSquareRook, PieceController.MoveCallback.None);

						toSquareRook.Piece = fromSquareRook.Piece;
						fromSquareRook.Piece = null;
					}
				}

				_castlingRights = _castlingRights.Replace(_fromSquare.IsWhite ? "K" : "k", string.Empty);
				_castlingRights = _castlingRights.Replace(_fromSquare.IsWhite ? "Q" : "q", string.Empty);
			}
			else if (_fromSquare.IsRook)
			{
				if (string.Compare(_fromSquare.Surface.name, "h1") == 0
					|| string.Compare(_fromSquare.Surface.name, "h8") == 0)
				{
					// moved kingside rook
					_castlingRights = _castlingRights.Replace(_fromSquare.IsWhite ? "K" : "k", string.Empty);
				}
				else if (string.Compare(_fromSquare.Surface.name, "a1") == 0
					|| string.Compare(_fromSquare.Surface.name, "a8") == 0)
				{
					// moved queenside rook
					_castlingRights = _castlingRights.Replace(_fromSquare.IsWhite ? "Q" : "q", string.Empty);
				}
			}

			if (string.IsNullOrEmpty(_castlingRights))
			{
				_castlingRights = "-";
			}

			// en passant
			if (_fromSquare.IsPawn)
			{
				if (string.Compare(_possibleEnPassantTarget, "-") != 0)
				{
					Square possibleEnPassantTarget = GetSquare(_possibleEnPassantTarget);
					PieceController pieceController = null;

					if ((_fromSquare.Rank == 5 && toSquare.Rank == 6 && _fromSquare.File + 1 == toSquare.File) // northeast
						|| (_fromSquare.Rank == 5 && toSquare.Rank == 6 && _fromSquare.File - 1 == toSquare.File)) // northwest
					{
						pieceController = _squares[possibleEnPassantTarget.Rank - 1, possibleEnPassantTarget.File].Piece.GetComponent<PieceController>();
					}
					else if ((_fromSquare.Rank == 4 && toSquare.Rank == 3 && _fromSquare.File + 1 == toSquare.File) // southeast
						|| (_fromSquare.Rank == 4 && toSquare.Rank == 3 && _fromSquare.File - 1 == toSquare.File)) // southwest
					{
						pieceController = _squares[possibleEnPassantTarget.Rank + 1, possibleEnPassantTarget.File].Piece.GetComponent<PieceController>();
					}

					pieceController?.Remove(false);
				}

				if (_fromSquare.Rank == 2
					&& toSquare.Rank == 4)
				{
					_possibleEnPassantTarget = GetAlgebraicNotation(_fromSquare.Rank + 1, _fromSquare.File);
				}
                else if (_fromSquare.Rank == 7
					&& toSquare.Rank == 5)
				{
					_possibleEnPassantTarget = GetAlgebraicNotation(_fromSquare.Rank - 1, _fromSquare.File);
				}
				else
				{
					_possibleEnPassantTarget = "-";
				}
            }

			// pawn promotiom
			if (_fromSquare.IsPawn)
			{
				if (toSquare.Rank == 8)
				{
					State = State.PromotePawn;
				}
				else if (toSquare.Rank == 1)
				{
					State = State.PromotePawn;
				}
			}
		}

		public void NewGameClicked()
		{
			_activeColor = Color.w;
			_castlingRights = "-";
			_fromSquare = null;
			_history.Clear();
			_lastMovesBlackText.text = "White: N/A";
			_lastMovesWhiteText.text = "Black: N/A";
			_newGame.interactable = false;
			_possibleEnPassantTarget = "-";
			_statusText.text = "Your move";
			_takeBack.interactable = false;
			_toSquares.Clear();

			for (int rank = 1; rank <= 8; rank++)
			{
				for (int file = 1; file <= 8; file++)
				{
					Destroy(_squares[rank, file].Piece);

					if (!(rank == 1 && file == 1))
					{
						Destroy(_squares[rank, file].Surface);
					}
				}
			}

			State = State.SetupBoard;
		}

		private void PlayBackgroundMusic()
		{
			StartCoroutine(PlayBackgroundMusicCoroutine());
		}

		private IEnumerator PlayBackgroundMusicCoroutine()
		{
			float volume = 0.25F;
			float fadeTime = 2F;
			float fadeStep = fadeTime / 20F;
			int clip = _random.Next(1, 9);

			while (_backgroundMusicHistory.Contains(clip))
			{
				clip = _random.Next(1, 9);
			}

			if (_backgroundMusicHistory.Count == 4)
			{
				_backgroundMusicHistory.RemoveAt(0);
			}

			_backgroundMusicHistory.Add(clip);

			if (_backgroundMusic.clip != null)
			{
				while (_backgroundMusic.volume > 0F)
				{
					_backgroundMusic.volume -= (volume * fadeStep);

					yield return new WaitForSeconds(fadeTime * fadeStep);
				}
			}

			_backgroundMusic.clip = Resources.Load($"Audio/Piano Instrumental {clip}") as AudioClip;
			
			Invoke("PlayBackgroundMusic", _backgroundMusic.clip.length - 2F);
			
			_backgroundMusic.Play();

			while (_backgroundMusic.volume < volume)
			{
				_backgroundMusic.volume += (volume * fadeStep);

				yield return new WaitForSeconds(fadeTime * fadeStep);
			}
		}

		private void PlayPieceDown()
		{
			_soundEffects.PlayOneShot(_pieceDown);
		}

		private void PlayPieceSlide()
		{
			_soundEffects.PlayOneShot(_pieceSlide);
		}

		private IEnumerator PromotePawnCoroutine()
		{
			while (_fromSquare != null)
			{
				yield return new WaitForSeconds(Time.deltaTime);
			}

			if (_activeColor == Color.w)
			{
				_camera.GetComponent<AudioListener>().enabled = false;
				_camera.SetActive(false);

				_pawnPromotionCamera.GetComponent<AudioListener>().enabled = true;
				_pawnPromotionCamera.SetActive(true);

				SetActive("WQPP", true);
				SetActive("WRPP", true);
				SetActive("WBPP", true);
				SetActive("WNFRPP", true);
			}
			else if (_activeColor == Color.b)
			{
				PromotedPawn("q");
			}
		}

		public void PromotedPawn(string forsythEdwardsNotation)
		{
			_pawnPromotionCamera.GetComponent<AudioListener>().enabled = false;
			_pawnPromotionCamera.SetActive(false);

			_camera.GetComponent<AudioListener>().enabled = true;
			_camera.SetActive(true);

			SetActive("WQPP", false);
			SetActive("WRPP", false);
			SetActive("WBPP", false);
			SetActive("WNFRPP", false);

			Move move = _history.Last();

			if (_activeColor == Color.b)
			{
				forsythEdwardsNotation = forsythEdwardsNotation.ToLower();
			}

			move.PawnPromotionForsythEdwardsNotation = forsythEdwardsNotation;

			string lastMove = $"{(move.Color == Color.w ? "White" : "Black")}: {move.FromAlgebraicNotation}:{move.ToAlgebraicNotation} {move.PawnPromotionForsythEdwardsNotation}";
			(move.Color == Color.w ? _lastMovesWhiteText : _lastMovesBlackText).text = lastMove;

			Square toSquare = GetSquare(move.ToAlgebraicNotation);
			PieceController pieceController = toSquare.Piece.GetComponent<PieceController>();

			pieceController.Remove(true);

			GameObject piece = InstantiatePiece(forsythEdwardsNotation, toSquare.File, toSquare.Surface, true);
			toSquare.Piece = piece;
			pieceController = piece.GetComponent<PieceController>();

			pieceController.Move(toSquare, PieceController.MoveCallback.PawnPromotionMoved);

			_activeColor = _activeColor == Color.w ? Color.b : Color.w;
			State = _activeColor == Color.w ? State.GetMovesForWhite : State.MakeMoveForBlack;
		}

		private void SetSquare(string algebraicNotation, GameObject surface, GameObject piece)
		{
			GetRankAndFile(algebraicNotation, out int rank, out int file);
			Destroy(_squares[rank, file]?.Piece);

			_squares[rank, file] = new Square(rank, file, surface, piece);
		}

		public void SoundEffectsToggleChanged(bool value)
		{
			_soundEffects.mute = !_soundEffectsToggle.isOn;
		}

		public void SquareSelected(string algebraicNotation)
		{
			Square square = GetSquare(algebraicNotation);

			if (_fromSquare == null)
			{
				if ((_activeColor == Color.w && square.IsWhite)
					|| (_activeColor == Color.b && square.IsBlack))
				{
					_fromSquare = square;
					square.Select();
				}
			}
			else if (_fromSquare == square)
			{
				_fromSquare = null;
				square.Unselect();
			}
			else
			{
				if (!_moves.ContainsKey(_fromSquare.Surface.name) 
					|| !_moves[_fromSquare.Surface.name].Contains(square.Surface.name))
				{
					return;
				}

				if (_fromSquare.IsKnight)
				{
					int rank = square.Rank;
					int file = square.File + _fromSquare.File - square.File;
					Square intermediateSquare = _squares[rank, file];

					_toSquares.Enqueue(intermediateSquare);
					_toSquares.Enqueue(square);
				}
				else
				{
					_toSquares.Enqueue(square);
				}

				State = State.Move;
			}
		}

		public void TakeBackClicked()
		{
			_takeBack.interactable = false;

			Move move = _history.Last();

			TakeBack(move);
		}

		private void TakeBack(Move move)
		{
			Square fromSquare = GetSquare(move.ToAlgebraicNotation);
			Square toSquare = GetSquare(move.FromAlgebraicNotation);

			if (fromSquare.IsKnight)
			{
				int rank = toSquare.Rank;
				int file = toSquare.File + fromSquare.File - toSquare.File;
				Square intermediateSquare = _squares[rank, file];

				move.ToSquares.Enqueue(intermediateSquare);
				move.ToSquares.Enqueue(toSquare);
			}
			else
			{
				move.ToSquares.Enqueue(toSquare);
			}

			PieceController pieceController = fromSquare.Piece.GetComponent<PieceController>();

			pieceController.Move(move.ToSquares.Dequeue(), PieceController.MoveCallback.TookBack);

			// capture
			move.Captured = InstantiatePiece(move.CapturedForsythEdwardsNotation, fromSquare.File, fromSquare.Surface, true);

			if (move.Captured != null)
			{
				pieceController = move.Captured.GetComponent<PieceController>();

				pieceController.Move(fromSquare, PieceController.MoveCallback.None);
			}

			// castling
			if (fromSquare.IsKing)
			{
				if (string.Compare(fromSquare.Surface.name, "g1") == 0
					|| string.Compare(fromSquare.Surface.name, "g8") == 0)
				{
					if (string.Compare(toSquare.Surface.name, "e1") == 0
						|| string.Compare(toSquare.Surface.name, "e8") == 0)
					{
						// castled kingside
						Square fromSquareRook = GetSquare(fromSquare.IsWhite ? "f1" : "f8");
						Square toSquareRook = GetSquare(fromSquare.IsWhite ? "h1" : "h8");
						pieceController = fromSquareRook.Piece.GetComponent<PieceController>();

						pieceController.Move(toSquareRook, PieceController.MoveCallback.None);

						toSquareRook.Piece = fromSquareRook.Piece;
						fromSquareRook.Piece = null;
					}
				}
				else if (string.Compare(fromSquare.Surface.name, "c1") == 0
					|| string.Compare(fromSquare.Surface.name, "c8") == 0)
				{
					if (string.Compare(toSquare.Surface.name, "e1") == 0
						|| string.Compare(toSquare.Surface.name, "e8") == 0)
					{
						// castled queenside
						Square fromSquareRook = GetSquare(fromSquare.IsWhite ? "d1" : "d8");
						Square toSquareRook = GetSquare(fromSquare.IsWhite ? "a1" : "a8");
						pieceController = fromSquareRook.Piece.GetComponent<PieceController>();

						pieceController.Move(toSquareRook, PieceController.MoveCallback.None);

						toSquareRook.Piece = fromSquareRook.Piece;
						fromSquareRook.Piece = null;
					}
				}
			}

			// en passant
			if (fromSquare.IsPawn)
			{
				Square enPassantSquare = null;
				string forsythEdwardsNotation = null;

				if ((fromSquare.Rank == 6 && toSquare.Rank == 5 && fromSquare.File - 1 == toSquare.File) // northeast
					|| (fromSquare.Rank == 6 && toSquare.Rank == 5 && fromSquare.File + 1 == toSquare.File)) // northwest
				{
					enPassantSquare = _squares[fromSquare.Rank - 1, fromSquare.File];
					forsythEdwardsNotation = "p";
				}
				else if ((fromSquare.Rank == 3 && toSquare.Rank == 4 && fromSquare.File - 1 == toSquare.File) // southeast
					|| (fromSquare.Rank == 3 && toSquare.Rank == 4 && fromSquare.File + 1 == toSquare.File)) // southwest
				{
					enPassantSquare = _squares[fromSquare.Rank + 1, fromSquare.File];
					forsythEdwardsNotation = "P";
				}

				if (enPassantSquare != null)
				{
					GameObject piece = InstantiatePiece(forsythEdwardsNotation, enPassantSquare.File, enPassantSquare.Surface, true);

					enPassantSquare.Piece = piece;

					pieceController = piece.GetComponent<PieceController>();

					pieceController.Move(enPassantSquare, PieceController.MoveCallback.None);
				}
			}

			// pawn promotion
			if (!string.IsNullOrEmpty(move.PawnPromotionForsythEdwardsNotation))
			{
				// remove the piece promoted to
				pieceController = fromSquare.Piece.GetComponent<PieceController>();
				pieceController.Remove(false);

				// replace the piece with a pawn
				GameObject piece = InstantiatePiece(move.Color == Color.w ? "P" : "p", fromSquare.File, fromSquare.Surface, true);
				fromSquare.Piece = piece;
				pieceController = piece.GetComponent<PieceController>();
				pieceController.Move(fromSquare, PieceController.MoveCallback.None);

				// move the pawn back
				pieceController.Move(toSquare, PieceController.MoveCallback.TookBack);
			}
		}

		public void TookBack()
		{
			Move move = _history.Last();
			Square fromSquare = GetSquare(move.ToAlgebraicNotation);
			Square toSquare = GetSquare(move.FromAlgebraicNotation);

			if (move.ToSquares.Count == 0)
			{
				toSquare.Piece = fromSquare.Piece;
				fromSquare.Piece = move.Captured;

				// knight?
				if (toSquare.IsKnight)
				{
					GameObject piece = InstantiatePiece(toSquare.ForsythEdwardsNotation, toSquare.File, toSquare.Surface, false);

					Destroy(toSquare.Piece);

					toSquare.Piece = piece;
				}

				_history.RemoveAt(_history.Count - 1);

				string lastMove = $"{(move.Color == Color.w ? "White: N/A" : "Black: N/A")}";

				if (_history.Count >= 1)
				{
					Move oneMoveAgo = _history[_history.Count - 1];

					_castlingRights = oneMoveAgo.CastlingRights;
					_possibleEnPassantTarget = oneMoveAgo.PossibleEnPassantTarget;
				}

				if (_history.Count >= 2)
				{
					Move twoMovesAgo = _history[_history.Count - 2];

					lastMove = $"{(move.Color == Color.w ? "White:" : "Black:")} {twoMovesAgo.FromAlgebraicNotation}:{twoMovesAgo.ToAlgebraicNotation} {twoMovesAgo.PawnPromotionForsythEdwardsNotation}";
				}

				(move.Color == Color.w ? _lastMovesWhiteText : _lastMovesBlackText).text = lastMove;

				if (_history.Count % 2 == 1)
				{
					move = _history.Last();

					TakeBack(move);
				}
				else
				{
					_takeBack.interactable = (_history.Count > 0 && _history.Count % 2 == 0)
						|| State == State.GameOver;

					State = State.GetMovesForWhite;
				}
			}
			else
			{
				PieceController pieceController = fromSquare.Piece.GetComponent<PieceController>();

				pieceController.Move(move.ToSquares.Dequeue(), PieceController.MoveCallback.TookBack);
			}
		}
	}
}