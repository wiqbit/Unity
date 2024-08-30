using UnityEngine;

namespace Assets.Scripts
{
	internal class Square
	{
		private MeshRenderer _meshRenderer = null;
		private Material _originalMaterial = null;
		private Material _blueMaterial = null;

		public Square(int rank, int file, GameObject surface, GameObject piece)
		{
			Rank = rank;
			File = file;
			Surface = surface;
			Piece = piece;
			Transform transform = Surface.transform.GetChild(0);
			_meshRenderer = transform.GetComponent<MeshRenderer>();
			_originalMaterial = _meshRenderer.material;
			_blueMaterial = Resources.Load("Materials/blue", typeof(Material)) as Material;
		}

		public void Select()
		{
			_meshRenderer.material = _blueMaterial;
		}

		public void Unselect()
		{
			_meshRenderer.material = _originalMaterial;
		}

		public int File { get; private set; }
		public string ForsythEdwardsNotation
		{
			get
			{
				string result = "1";

				if (Piece != null)
				{
					result = Piece.name.Substring(1, 1);

					if (IsBlack)
					{
						result = result.ToLower();
					}
				}

				return result;
			}
		}
		public bool IsBlack
		{
			get { return Piece != null && Piece.name.StartsWith("B"); }
		}
		public bool IsKing
		{
			get { return Piece != null && string.Compare(Piece.name.Substring(1, 1), "K") == 0; }
		}
		public bool IsKnight
		{
			get { return Piece != null && string.Compare(Piece.name.Substring(1, 1), "N") == 0; }
		}
		public bool IsPawn
		{
			get { return Piece != null && string.Compare(Piece.name.Substring(1, 1), "P") == 0; }
		}
		public bool IsRook
		{
			get { return Piece != null && string.Compare(Piece.name.Substring(1, 1), "R") == 0; }
		}
		public bool IsWhite
		{
			get { return Piece != null && Piece.name.StartsWith("W"); }
		}
		public GameObject Piece { get; set; }
		public int Rank { get; private set; }
		public GameObject Surface { get; private set; }
	}
}