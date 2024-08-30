using System;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
	internal abstract class Base : MonoBehaviour
	{
		private protected string GetAlgebraicNotation(int rank, int file)
		{
			string result = $"{" abcdefgh".Substring(file, 1)}{rank}";

			return result;
		}

		protected void GetRankAndFile(string algebraicNotation, out int rank, out int file)
		{
			rank = int.Parse(algebraicNotation.Substring(1, 1));
			file = " abcdefgh".IndexOf(algebraicNotation.Substring(0, 1));
		}

		protected GameObject InstantiatePiece(string forsythEdwardsNotation, int file, GameObject surface, bool positionBelowSurface)
		{
			GameObject result = null;
			string name = null;

			if (string.Compare(forsythEdwardsNotation.ToUpper(), "N") == 0)
			{
				name = $"{(IsWhite(forsythEdwardsNotation) ? "W" : "B")}NF{(file <= 4 ? "R" : "L")}";
			}
			else if (string.Compare(forsythEdwardsNotation, "1") != 0)
			{
				name = $"{(IsWhite(forsythEdwardsNotation) ? "W" : "B")}{forsythEdwardsNotation.ToUpper()}";
			}

			if (!string.IsNullOrEmpty(name))
			{
				GameObject piece = FindObjectsOfType<GameObject>(true).ToList().Find(gameObjectCandidate => string.Compare(gameObjectCandidate.name, name) == 0);

				Vector3 position = surface.transform.position;

				if (positionBelowSurface)
				{
					Bounds bounds = piece.GetComponentInChildren<MeshRenderer>().bounds;
					position = new Vector3(surface.transform.position.x, 0F - bounds.size.y / 2F - 1F, surface.transform.position.z);
				}

				result = Instantiate(piece, position, piece.transform.rotation);

				result.SetActive(true);

				result.name = $"{piece.name}_{Guid.NewGuid().ToString("N")}";
			}

			return result;
		}

		protected bool IsWhite(string piece)
		{
			bool result = "KQRNBP".IndexOf(piece) >= 0;

			return result;
		}

		protected void SetActive(string name, bool value)
		{
			GameObject piece = FindObjectsOfType<GameObject>(true).ToList().Find(gameObjectCandidate => string.Compare(gameObjectCandidate.name, name) == 0);

			piece.SetActive(value);
		}
	}
}