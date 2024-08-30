using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
	internal class Move
	{
		public Move()
		{
			ToSquares = new Queue<Square>();
		}

		public Color Color { get; set; }
		public string FromAlgebraicNotation { get; set; }
		public string ToAlgebraicNotation { get; set; }
		public string CapturedForsythEdwardsNotation { get; set; }
		public string PawnPromotionForsythEdwardsNotation { get; set; }
		public string CastlingRights { get; set; }
		public string PossibleEnPassantTarget { get; set; }
		public Queue<Square> ToSquares { get; set; }
		public GameObject Captured {  get; set; }
	}
}