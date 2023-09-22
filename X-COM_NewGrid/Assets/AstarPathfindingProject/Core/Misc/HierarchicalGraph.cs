using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;
using UnityEngine.Profiling;

namespace Pathfinding {
    /// <summary>
    /// Содержит иерархический график для ускорения определенных запросов поиска пути.
    ///
    /// Распространенным типом запроса, который должен быть очень быстрым, является запрос в форме "доступен ли этот узел с этого другого узла".
    /// Это, например, используется при выборе конечного узла пути. Конечный узел определяется как ближайший узел к конечной точке
    /// который может быть достигнут с начального узла.
    ///
    /// Основная цель этой структуры данных - отслеживать, в каком подключенном компоненте содержится каждый узел, чтобы быстро выполнять такие запросы.
    ///
    /// See: https://en.wikipedia.org/wiki/Connected_component_(graph_theory)
    ///
    /// Подключенный компонент - это набор узлов, такой, что существует допустимый путь между каждой парой узлов в этом наборе.
    /// Таким образом, на приведенный выше запрос можно просто ответить, проверив, находятся ли они в одном и том же подключенном компоненте.
    /// Подключенный компонент отображается на узлах как <see cref="Pathfinding.GraphNode.Area"/> свойство и в этом классе, используя метод <see cref="getArea"/>.
    ///
    /// На изображении ниже (показан график в виде сетки размером 200х200) каждый подключенный компонент окрашен отдельным цветом.
    /// Однако фактический цвет ничего конкретного не означает, только то, что они разные.
    /// [Откройте онлайн-документацию, чтобы просмотреть изображения]
    ///
    /// До версии 4.2 подключенные компоненты представляли собой просто число, хранящееся на каждом узле, и при обновлении графика
    /// подключенные компоненты были полностью пересчитаны. Это может быть сделано относительно эффективно с помощью заливочного устройства
    /// алгоритм (см. https://en.wikipedia.org/wiki/Flood_fill ) однако для этого по-прежнему требуется проход через каждый отдельный узел
    /// что может быть довольно дорогостоящим на больших графиках.
    ///
    /// Этот класс вместо этого строит гораздо меньший граф, который по-прежнему сохраняет ту же связность, что и исходный граф.
    /// Каждый узел в этом иерархическом графе представляет большее число реальных узлов, которые являются одним единственным подключенным компонентом.
    /// Взгляните на изображение ниже для примера. На изображении каждый цвет представляет собой отдельный иерархический узел, а черные соединения проходят между центром каждого иерархического узла.
    ///
    /// [Откройте онлайн-документацию, чтобы просмотреть изображения]
    ///
    /// С помощью иерархического графика связанные компоненты могут быть вычислены путем заполнения иерархического графика потоком вместо реального графика.
    /// Затем, когда нам нужно знать, к какому подключенному компоненту принадлежит узел, мы ищем подключенный компонент иерархического узла, к которому принадлежит узел.
    ///
    /// Выгода не сразу очевидна. Вышеописанное - всего лишь немного более сложный способ выполнить то же самое. Однако реальная выгода возникает при обновлении графика.
    /// Когда график обновляется, все иерархические узлы, которые содержат любой узел, на который повлияло обновление, полностью удаляются, а затем, как только все они были удалены, новые иерархические узлы пересчитываются на их месте.
    /// Как только это будет сделано, связанные компоненты всего графа могут быть обновлены путем заполнения потоком только иерархического графа. Поскольку иерархический граф значительно меньше реального, это происходит значительно быстрее.
    ///
    /// [Откройте онлайн-документацию, чтобы просмотреть видео]
    ///
    /// Итак, наконец, используя все это, связанные компоненты графика могут быть пересчитаны очень быстро по мере обновления графика.
    /// Эффект от этого возрастает тем больше, чем больше график, и чем меньше обновление графика. При такой оптимизации внесение небольшого обновления в сетчатый график размером 1000х1000 выполняется порядка в 40 раз быстрее.
    /// Однако при одновременном сканировании графика или внесении обновлений во весь график скорость не увеличивается. На самом деле из-за дополнительной сложности это происходит немного медленнее, однако после профилирования дополнительное время кажется в основном незначительным по сравнению с остальными затратами на сканирование графика.
    ///
    /// [Откройте онлайн-документацию, чтобы посмотреть видео]
    ///
    /// See: <see cref="Pathfinding.PathUtilities.IsPathPossible"/>
    /// See: <see cref="Pathfinding.NNConstraint"/>
    /// See: <see cref="Pathfinding.GraphNode.Area"/>
    /// </summary>
    public class HierarchicalGraph {
		const int Tiling = 16;
		const int MaxChildrenPerNode = Tiling * Tiling;
		const int MinChildrenPerNode = MaxChildrenPerNode/2;

		List<GraphNode>[] children = new List<GraphNode>[0];
		List<int>[] connections = new List<int>[0];
		int[] areas = new int[0];
		byte[] dirty = new byte[0];

		public int version { get; private set; }
		public System.Action onConnectedComponentsChanged;

		System.Action<GraphNode> connectionCallback;

		Queue<GraphNode> temporaryQueue = new Queue<GraphNode>();
		List<GraphNode> currentChildren = null;
		List<int> currentConnections = null;
		int currentHierarchicalNodeIndex;
		Stack<int> temporaryStack = new Stack<int>();

		int numDirtyNodes = 0;
		GraphNode[] dirtyNodes = new GraphNode[128];

		Stack<int> freeNodeIndices = new Stack<int>();

		int gizmoVersion = 0;

		public HierarchicalGraph () {
			// Cache this callback to avoid allocating a new one every time the FindHierarchicalNodeChildren method is called.
			// It is a big ugly to have to use member variables for the state information in that method, but I see no better way.
			connectionCallback = (GraphNode neighbour) => {
				var hIndex = neighbour.HierarchicalNodeIndex;
				if (hIndex == 0) {
					if (currentChildren.Count < MaxChildrenPerNode && neighbour.Walkable /* && (((GridNode)currentChildren[0]).XCoordinateInGrid/Tiling == ((GridNode)neighbour).XCoordinateInGrid/Tiling) && (((GridNode)currentChildren[0]).ZCoordinateInGrid/Tiling == ((GridNode)neighbour).ZCoordinateInGrid/Tiling)*/) {
						neighbour.HierarchicalNodeIndex = currentHierarchicalNodeIndex;
						temporaryQueue.Enqueue(neighbour);
						currentChildren.Add(neighbour);
					}
				} else if (hIndex != currentHierarchicalNodeIndex && !currentConnections.Contains(hIndex)) {
					// The Contains call can in theory be very slow as an hierarchical node may be adjacent to an arbitrary number of nodes.
					// However in practice due to how the nodes are constructed they will only be adjacent to a smallish (≈4-6) number of other nodes.
					// So a Contains call will be much faster than say a Set lookup.
					currentConnections.Add(hIndex);
				}
			};

			Grow();
		}

		void Grow () {
			var newChildren = new List<GraphNode>[System.Math.Max(64, children.Length*2)];
			var newConnections = new List<int>[newChildren.Length];
			var newAreas = new int[newChildren.Length];
			var newDirty = new byte[newChildren.Length];

			children.CopyTo(newChildren, 0);
			connections.CopyTo(newConnections, 0);
			areas.CopyTo(newAreas, 0);
			dirty.CopyTo(newDirty, 0);

			for (int i = children.Length; i < newChildren.Length; i++) {
				newChildren[i] = ListPool<GraphNode>.Claim(MaxChildrenPerNode);
				newConnections[i] = new List<int>();
				if (i > 0) freeNodeIndices.Push(i);
			}

			children = newChildren;
			connections = newConnections;
			areas = newAreas;
			dirty = newDirty;
		}

		int GetHierarchicalNodeIndex () {
			if (freeNodeIndices.Count == 0) Grow();
			return freeNodeIndices.Pop();
		}

		internal void OnCreatedNode (GraphNode node) {
			if (node.NodeIndex >= dirtyNodes.Length) {
				var newDirty = new GraphNode[System.Math.Max(node.NodeIndex + 1, dirtyNodes.Length*2)];
				dirtyNodes.CopyTo(newDirty, 0);
				dirtyNodes = newDirty;
			}
			AddDirtyNode(node);
		}

        /// <summary>
        /// Помечает этот узел как грязный, потому что изменилась его подключаемость или проходимость.
        /// Это должно вызываться классами узлов после того, как в них были внесены какие-либо изменения в связности/ проходимости.
        ///
        /// See: <see cref="GraphNode.SetConnectivityDirty"/>
        /// </summary>
        public void AddDirtyNode (GraphNode node) {
			if (!node.IsHierarchicalNodeDirty) {
				node.IsHierarchicalNodeDirty = true;
				// While the dirtyNodes array is guaranteed to be large enough to hold all nodes in the graphs
				// the array may also end up containing many destroyed nodes. This can in rare cases cause it to go out of bounds.
				// In that case we need to go through the array and filter out any destroyed nodes while making sure to mark their
				// corresponding hierarchical nodes as being dirty.
				if (numDirtyNodes < dirtyNodes.Length) {
					dirtyNodes[numDirtyNodes] = node;
					numDirtyNodes++;
				} else {
					int maxIndex = 0;
					for (int i = numDirtyNodes - 1; i >= 0; i--) {
						if (dirtyNodes[i].Destroyed) {
							numDirtyNodes--;
							dirty[dirtyNodes[i].HierarchicalNodeIndex] = 1;
							dirtyNodes[i] = dirtyNodes[numDirtyNodes];
							dirtyNodes[numDirtyNodes] = null;
						} else {
							maxIndex = System.Math.Max(maxIndex, dirtyNodes[i].NodeIndex);
						}
					}
					if (numDirtyNodes >= dirtyNodes.Length) throw new System.Exception("Failed to compactify dirty nodes array. This should not happen. " + maxIndex + " " + numDirtyNodes + " " + dirtyNodes.Length);
					AddDirtyNode(node);
				}
			}
		}

		public int NumConnectedComponents { get; private set; }

		/// <summary>Get the connected component index of a hierarchical node</summary>
		public uint GetConnectedComponent (int hierarchicalNodeIndex) {
			return (uint)areas[hierarchicalNodeIndex];
		}

		void RemoveHierarchicalNode (int hierarchicalNode, bool removeAdjacentSmallNodes) {
			freeNodeIndices.Push(hierarchicalNode);
			var conns = connections[hierarchicalNode];

			for (int i = 0; i < conns.Count; i++) {
				var adjacentHierarchicalNode = conns[i];
				// If dirty, this node will be removed later anyway, so don't bother doing anything with it.
				if (dirty[adjacentHierarchicalNode] != 0) continue;

				if (removeAdjacentSmallNodes && children[adjacentHierarchicalNode].Count < MinChildrenPerNode) {
					dirty[adjacentHierarchicalNode] = 2;
					RemoveHierarchicalNode(adjacentHierarchicalNode, false);
				} else {
					// Remove the connection from the other node to this node as we are removing this node.
					connections[adjacentHierarchicalNode].Remove(hierarchicalNode);
				}
			}
			conns.Clear();

			var nodeChildren = children[hierarchicalNode];

			for (int i = 0; i < nodeChildren.Count; i++) {
				AddDirtyNode(nodeChildren[i]);
			}

			nodeChildren.ClearFast();
		}

		/// <summary>Recalculate the hierarchical graph and the connected components if any nodes have been marked as dirty</summary>
		public void RecalculateIfNecessary () {
			if (numDirtyNodes > 0) {
				Profiler.BeginSample("Recalculate Connected Components");
				for (int i = 0; i < numDirtyNodes; i++) {
					dirty[dirtyNodes[i].HierarchicalNodeIndex] = 1;
				}

				// Remove all hierarchical nodes and then build new hierarchical nodes in their place
				// which take into account the new graph data.
				for (int i = 1; i < dirty.Length; i++) {
					if (dirty[i] == 1) RemoveHierarchicalNode(i, true);
				}
				for (int i = 1; i < dirty.Length; i++) dirty[i] = 0;

				for (int i = 0; i < numDirtyNodes; i++) {
					dirtyNodes[i].HierarchicalNodeIndex = 0;
				}

				for (int i = 0; i < numDirtyNodes; i++) {
					var node = dirtyNodes[i];
					// Be nice to the GC
					dirtyNodes[i] = null;
					node.IsHierarchicalNodeDirty = false;

					if (node.HierarchicalNodeIndex == 0 && node.Walkable && !node.Destroyed) {
						FindHierarchicalNodeChildren(GetHierarchicalNodeIndex(), node);
					}
				}

				numDirtyNodes = 0;
				// Recalculate the connected components of the hierarchical nodes
				FloodFill();
				Profiler.EndSample();
				gizmoVersion++;
			}
		}

		/// <summary>
		/// Recalculate everything from scratch.
		/// This is primarily to be used for legacy code for compatibility reasons, not for any new code.
		///
		/// See: <see cref="RecalculateIfNecessary"/>
		/// </summary>
		public void RecalculateAll () {
			AstarPath.active.data.GetNodes(node => AddDirtyNode(node));
			RecalculateIfNecessary();
		}

		/// <summary>Flood fills the graph of hierarchical nodes and assigns the same area ID to all hierarchical nodes that are in the same connected component</summary>
		void FloodFill () {
			for (int i = 0; i < areas.Length; i++) areas[i] = 0;

			Stack<int> stack = temporaryStack;
			int currentArea = 0;
			for (int i = 1; i < areas.Length; i++) {
				// Already taken care of
				if (areas[i] != 0) continue;

				currentArea++;
				areas[i] = currentArea;
				stack.Push(i);
				while (stack.Count > 0) {
					int node = stack.Pop();
					var conns = connections[node];
					for (int j = conns.Count - 1; j >= 0; j--) {
						var otherNode = conns[j];
						// Note: slightly important that this is != currentArea and not != 0 in case there are some connected, but not stongly connected components in the graph (this will happen in only veeery few types of games)
						if (areas[otherNode] != currentArea) {
							areas[otherNode] = currentArea;
							stack.Push(otherNode);
						}
					}
				}
			}

			NumConnectedComponents = System.Math.Max(1, currentArea + 1);
			version++;
		}

		/// <summary>Run a BFS out from a start node and assign up to MaxChildrenPerNode nodes to the specified hierarchical node which are not already assigned to another hierarchical node</summary>
		void FindHierarchicalNodeChildren (int hierarchicalNode, GraphNode startNode) {
			// Set some state for the connectionCallback delegate to use
			currentChildren = children[hierarchicalNode];
			currentConnections = connections[hierarchicalNode];
			currentHierarchicalNodeIndex = hierarchicalNode;

			var que = temporaryQueue;
			que.Enqueue(startNode);

			startNode.HierarchicalNodeIndex = hierarchicalNode;
			currentChildren.Add(startNode);

			while (que.Count > 0) {
				que.Dequeue().GetConnections(connectionCallback);
			}

			for (int i = 0; i < currentConnections.Count; i++) {
				connections[currentConnections[i]].Add(hierarchicalNode);
			}

			que.Clear();
		}

		public void OnDrawGizmos (Pathfinding.Util.RetainedGizmos gizmos) {
			var hasher = new Pathfinding.Util.RetainedGizmos.Hasher(AstarPath.active);

			hasher.AddHash(gizmoVersion);

			if (!gizmos.Draw(hasher)) {
				var builder = ObjectPool<RetainedGizmos.Builder>.Claim();
				var centers = ArrayPool<UnityEngine.Vector3>.Claim(areas.Length);
				for (int i = 0; i < areas.Length; i++) {
					Int3 center = Int3.zero;
					var childs = children[i];
					if (childs.Count > 0) {
						for (int j = 0; j < childs.Count; j++) center += childs[j].position;
						center /= childs.Count;
						centers[i] = (UnityEngine.Vector3)center;
					}
				}

				for (int i = 0; i < areas.Length; i++) {
					if (children[i].Count > 0) {
						for (int j = 0; j < connections[i].Count; j++) {
							if (connections[i][j] > i) {
								builder.DrawLine(centers[i], centers[connections[i][j]], UnityEngine.Color.black);
							}
						}
					}
				}

				builder.Submit(gizmos, hasher);
			}
		}
	}
}
