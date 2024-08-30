using UnityEngine;

namespace Assets.Scripts
{
	public class PiecePawnPromotionController : MonoBehaviour
	{
		void Start()
		{
			// Start is called before the first frame update
		}

		void Update()
		{
			// Update is called once per frame
		}

		public void OnMouseUp()
		{
			string forsythEdwardsNotation = null;

			switch (gameObject.name)
			{
				case "white_queen_pp":
					forsythEdwardsNotation = "Q";
					break;

				case "white_rook_pp":
					forsythEdwardsNotation = "R";
					break;

				case "white_bishop_pp":
					forsythEdwardsNotation = "B";
					break;

				case "white_knight_fr_pp":
					forsythEdwardsNotation = "N";
					break;
			}

			GameSystem.Instance.PromotedPawn(forsythEdwardsNotation);
		}
	}
}