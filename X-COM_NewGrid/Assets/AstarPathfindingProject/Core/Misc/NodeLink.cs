using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pathfinding {
	using Pathfinding.Util;

    /// <summary>
    /// ��������� ��� ���� ������ �����������.
    /// ���������� ���������� ��� ������ ��� ���������� �� ���� (������� ����� ���� ������� ��� ������), ��� ����� �� ������ ������������ Node Link 2.
    ///
    /// [�������� ������-������������, ����� ����������� �����������]
    ///
    /// ��������: ��������������-������� (������� ������ �������� � ������-������������)
    /// </summary>
    [AddComponentMenu("Pathfinding/Link")]
	[HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_node_link.php")]
	public class NodeLink : GraphModifier {
		/// <summary>End position of the link</summary>
		public Transform end;

        /// <summary>
        /// ���������� ����� � ���� ��� ��������� ������� / ���������.
        /// �������� ��������, ��� �������� ������ ������� �� ������ �������� ��������� �������� ���� ���� ������ �������, ���� ���� ���� ������
        /// �������� � �������� ����� ���������, ���� �� ����� �� �������������� ������������� ����� A* Inspector -> Settings -> Pathfinding or disable the heuristic altogether.
        /// </summary>
        public float costFactor = 1.0f;

		/// <summary>Make a one-way connection</summary>
		public bool oneWay = false;

		/// <summary>Delete existing connection instead of adding one</summary>
		public bool deleteConnection = false;

		public Transform Start {
			get { return transform; }
		}

		public Transform End {
			get { return end; }
		}

		public override void OnPostScan () {
			if (AstarPath.active.isScanning) {
				InternalOnPostScan();
			} else {
				AstarPath.active.AddWorkItem(new AstarWorkItem(force => {
					InternalOnPostScan();
					return true;
				}));
			}
		}

		public void InternalOnPostScan () {
			Apply();
		}

		public override void OnGraphsPostUpdate () {
			if (!AstarPath.active.isScanning) {
				AstarPath.active.AddWorkItem(new AstarWorkItem(force => {
					InternalOnPostScan();
					return true;
				}));
			}
		}

		public virtual void Apply () {
			if (Start == null || End == null || AstarPath.active == null) return;

			GraphNode startNode = AstarPath.active.GetNearest(Start.position).node;
			GraphNode endNode = AstarPath.active.GetNearest(End.position).node;

			if (startNode == null || endNode == null) return;


			if (deleteConnection) {
				startNode.RemoveConnection(endNode);
				if (!oneWay)
					endNode.RemoveConnection(startNode);
			} else {
				uint cost = (uint)System.Math.Round((startNode.position-endNode.position).costMagnitude*costFactor);

				startNode.AddConnection(endNode, cost);
				if (!oneWay)
					endNode.AddConnection(startNode, cost);
			}
		}

		public void OnDrawGizmos () {
			if (Start == null || End == null) return;

			Draw.Gizmos.Bezier(Start.position, End.position, deleteConnection ? Color.red : Color.green);
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem("Edit/Pathfinding/Link Pair %&l")]
		public static void LinkObjects () {
			Transform[] tfs = Selection.transforms;
			if (tfs.Length == 2) {
				LinkObjects(tfs[0], tfs[1], false);
			}
			SceneView.RepaintAll();
		}

		[UnityEditor.MenuItem("Edit/Pathfinding/Unlink Pair %&u")]
		public static void UnlinkObjects () {
			Transform[] tfs = Selection.transforms;
			if (tfs.Length == 2) {
				LinkObjects(tfs[0], tfs[1], true);
			}
			SceneView.RepaintAll();
		}

		[UnityEditor.MenuItem("Edit/Pathfinding/Delete Links on Selected %&b")]
		public static void DeleteLinks () {
			Transform[] tfs = Selection.transforms;
			for (int i = 0; i < tfs.Length; i++) {
				NodeLink[] conns = tfs[i].GetComponents<NodeLink>();
				for (int j = 0; j < conns.Length; j++) DestroyImmediate(conns[j]);
			}
			SceneView.RepaintAll();
		}

		public static void LinkObjects (Transform a, Transform b, bool removeConnection) {
			NodeLink connecting = null;

			NodeLink[] conns = a.GetComponents<NodeLink>();
			for (int i = 0; i < conns.Length; i++) {
				if (conns[i].end == b) {
					connecting = conns[i];
					break;
				}
			}

			conns = b.GetComponents<NodeLink>();
			for (int i = 0; i < conns.Length; i++) {
				if (conns[i].end == a) {
					connecting = conns[i];
					break;
				}
			}

			if (removeConnection) {
				if (connecting != null) DestroyImmediate(connecting);
			} else {
				if (connecting == null) {
					connecting = a.gameObject.AddComponent<NodeLink>();
					connecting.end = b;
				} else {
					connecting.deleteConnection = !connecting.deleteConnection;
				}
			}
		}
#endif
	}
}
