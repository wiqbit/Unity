using UnityEngine;

namespace Assets.Scripts
{
    internal class SquareController : Base
    {
		public GameObject _board = null;

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
			GameSystem.Instance.SquareSelected(name);
		}
	}
}