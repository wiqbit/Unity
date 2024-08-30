using System.Collections.Generic;

namespace Assets.Scripts.Engine
{
	internal class Chess
	{
		public string GetMove(string forsythEdwardsNotation)
		{
			Position position = new Position(forsythEdwardsNotation);
			string result = position.GetMove();

			return result;
		}

		public Dictionary<string, List<string>> GetMoves(string forsythEdwardsNotation)
		{
			Position position = new Position(forsythEdwardsNotation);
			Dictionary<string, List<string>> result = position.GetMoves();

			return result;
		}
	}
}