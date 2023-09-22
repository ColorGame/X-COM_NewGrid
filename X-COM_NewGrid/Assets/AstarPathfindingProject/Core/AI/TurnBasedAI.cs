using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding.Examples {
	/// <summary>Helper script in the example scene 'Turn Based'</summary>
	[HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_examples_1_1_turn_based_a_i.php")]
	public class TurnBasedAI : VersionedMonoBehaviour {
		public int movementPoints = 2;
		public BlockManager blockManager;
		public SingleNodeBlocker blocker;
		public GraphNode targetNode;
		public BlockManager.TraversalProvider traversalProvider;

		void Start () {
			blocker.BlockAtCurrentPosition();
		}

		protected override void Awake () {
			base.Awake();
            // Установите провайдера обхода так, чтобы он блокировал все узлы, которые заблокированы SingleNodeBlocker
            // за исключением SingleNodeBlocker, принадлежащего этому ИИ (мы не хотим, чтобы вы сами блокировали).
            traversalProvider = new BlockManager.TraversalProvider(blockManager, BlockManager.BlockMode.AllExceptSelector, new List<SingleNodeBlocker>() { blocker });
		}
	}
}
