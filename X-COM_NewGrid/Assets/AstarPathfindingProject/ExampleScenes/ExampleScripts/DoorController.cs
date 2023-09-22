using UnityEngine;

namespace Pathfinding.Examples {
	/// <summary>Example script used in the example scenes</summary>
	[HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_examples_1_1_door_controller.php")]
	public class DoorController : MonoBehaviour {
		private bool open = false;

		public int opentag = 1;
		public int closedtag = 1;
		public bool updateGraphsWithGUO = true;
		public float yOffset = 5;

		Bounds bounds;

		public void Start () {
			// Capture the bounds of the collider while it is closed
			bounds = GetComponent<Collider>().bounds;

			// Initially open the door
			SetState(open);
		}

		void OnGUI () {
			// Show a UI button for opening and closing the door
			if (GUI.Button(new Rect(5, yOffset, 100, 22), "Toggle Door")) {
				SetState(!open);
			}
		}

		public void SetState (bool open) {
			this.open = open;

			if (updateGraphsWithGUO) {
                // Обновите график под дверью
                // Установите метку для узлов под дверью
                // К чему-либо, указывающему на то, что дверь открыта или закрыта
                GraphUpdateObject guo = new GraphUpdateObject(bounds);
				int tag = open ? opentag : closedtag;

				// There are only 32 tags
				if (tag > 31) { Debug.LogError("tag > 31"); return; }

				guo.modifyTag = true;
				guo.setTag = tag;
				guo.updatePhysics = false;

				AstarPath.active.UpdateGraphs(guo);
			}

			// Play door animations
			if (open) {
				GetComponent<Animation>().Play("Open");
			} else {
				GetComponent<Animation>().Play("Close");
			}
		}
	}
}
