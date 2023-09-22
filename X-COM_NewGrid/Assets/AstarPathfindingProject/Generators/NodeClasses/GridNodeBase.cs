
using UnityEngine;
using Pathfinding.Serialization;

namespace Pathfinding {
    /// <summary>Базовый класс для GridNode(узла сетки) и LevelGridNode(узла сетки уровня)</summary>
    public abstract class GridNodeBase : GraphNode {
		protected GridNodeBase (AstarPath astar) : base(astar) {
		}

		const int GridFlagsWalkableErosionOffset = 8;
		const int GridFlagsWalkableErosionMask = 1 << GridFlagsWalkableErosionOffset;

		const int GridFlagsWalkableTmpOffset = 9;
		const int GridFlagsWalkableTmpMask = 1 << GridFlagsWalkableTmpOffset;

		protected const int NodeInGridIndexLayerOffset = 24;
		protected const int NodeInGridIndexMask = 0xFFFFFF;

        /// <summary>
        /// Битовое поле, содержащее координаты x и z узла, а также слоя (для многоуровневых сеточных графов).
        /// See: NodeInGridIndex
        /// </summary>
        protected int nodeInGridIndex;
		protected ushort gridFlags;

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
		/// <summary>
		/// Custon non-grid connections from this node.
		/// See: <see cref="AddConnection"/>
		/// See: <see cref="RemoveConnection"/>
		///
		/// This field is removed if the ASTAR_GRID_NO_CUSTOM_CONNECTIONS compiler directive is used.
		/// Removing it can save a tiny bit of memory. You can enable the define in the Optimizations tab in the A* inspector.
		/// See: compiler-directives (view in online documentation for working links)
		///
		/// Note: If you modify this array or the contents of it you must call <see cref="SetConnectivityDirty"/>.
		/// </summary>
		public Connection[] connections;
#endif

        /// <summary>
        /// Индекс узла в сетке.
        /// Это график x + z*.ширина
        /// Таким образом, вы можете получить индексы X и Z, используя
        /// <code>
        /// int index = node.NodeInGridIndex;
        /// int x = index % graph.width;
        /// int z = index / graph.width;
        /// // where graph is GridNode.GetGridGraph (node.graphIndex), i.e the graph the nodes are contained in.
        /// </code>
        /// </summary>
        public int NodeInGridIndex { get { return nodeInGridIndex & NodeInGridIndexMask; } set { nodeInGridIndex = (nodeInGridIndex & ~NodeInGridIndexMask) | value; } }

		/// <summary>
		/// X coordinate of the node in the grid.
		/// The node in the bottom left corner has (x,z) = (0,0) and the one in the opposite
		/// corner has (x,z) = (width-1, depth-1)
		/// See: ZCoordInGrid
		/// See: NodeInGridIndex
		/// </summary>
		public int XCoordinateInGrid {
			get {
				return NodeInGridIndex % GridNode.GetGridGraph(GraphIndex).width;
			}
		}

		/// <summary>
		/// Z coordinate of the node in the grid.
		/// The node in the bottom left corner has (x,z) = (0,0) and the one in the opposite
		/// corner has (x,z) = (width-1, depth-1)
		/// See: XCoordInGrid
		/// See: NodeInGridIndex
		/// </summary>
		public int ZCoordinateInGrid {
			get {
				return NodeInGridIndex / GridNode.GetGridGraph(GraphIndex).width;
			}
		}

        /// <summary>
        /// Сохраняет проходимость перед нанесением эрозии.
        /// Используется внутренне при обновлении графика.
        /// </summary>
        public bool WalkableErosion {
			get {
				return (gridFlags & GridFlagsWalkableErosionMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableErosionMask | (value ? (ushort)GridFlagsWalkableErosionMask : (ushort)0)); }
			}
		}

        /// <summary>Временная переменная, используемая внутренне при обновлении графика.</summary>
        public bool TmpWalkable {
			get {
				return (gridFlags & GridFlagsWalkableTmpMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableTmpMask | (value ? (ushort)GridFlagsWalkableTmpMask : (ushort)0)); }
			}
		}

        /// <summary>
        /// Истинно, если узел имеет сетевые подключения ко всем своим 8 соседям.
        /// Примечание: Это всегда будет возвращать значение false, если для Grid Graph.neighbors задано значение, отличное от восьми.
        /// See: GetNeighbourAlongDirection
        /// </summary>
        public abstract bool HasConnectionsToAllEightNeighbours { get; }

		public override float SurfaceArea () {
			GridGraph gg = GridNode.GetGridGraph(GraphIndex);

			return gg.nodeSize*gg.nodeSize;
		}

		public override Vector3 RandomPointOnSurface () {
			GridGraph gg = GridNode.GetGridGraph(GraphIndex);

			var graphSpacePosition = gg.transform.InverseTransform((Vector3)position);

			return gg.transform.Transform(graphSpacePosition + new Vector3(Random.value - 0.5f, 0, Random.value - 0.5f));
		}

		/// <summary>
		/// Transforms a world space point to a normalized point on this node's surface.
		/// (0.5,0.5) represents the node's center. (0,0), (1,0), (1,1) and (0,1) each represent the corners of the node.
		///
		/// See: <see cref="UnNormalizePoint"/>
		/// </summary>
		public Vector2 NormalizePoint (Vector3 worldPoint) {
			GridGraph gg = GridNode.GetGridGraph(GraphIndex);
			var graphSpacePosition = gg.transform.InverseTransform(worldPoint);

			return new Vector2(graphSpacePosition.x - this.XCoordinateInGrid, graphSpacePosition.z - this.ZCoordinateInGrid);
		}

		/// <summary>
		/// Transforms a normalized point on this node's surface to a world space point.
		/// (0.5,0.5) represents the node's center. (0,0), (1,0), (1,1) and (0,1) each represent the corners of the node.
		///
		/// See: <see cref="NormalizePoint"/>
		/// </summary>
		public Vector3 UnNormalizePoint (Vector2 normalizedPointOnSurface) {
			GridGraph gg = GridNode.GetGridGraph(GraphIndex);

			return (Vector3)this.position + gg.transform.TransformVector(new Vector3(normalizedPointOnSurface.x - 0.5f, 0, normalizedPointOnSurface.y - 0.5f));
		}

		public override int GetGizmoHashCode () {
			var hash = base.GetGizmoHashCode();

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			if (connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					hash ^= 17 * connections[i].GetHashCode();
				}
			}
#endif
			hash ^= 109 * gridFlags;
			return hash;
		}

        /// <summary>
        /// Соседний узел сетки в указанном направлении.
        /// Это вернет значение null, если узел не имеет подключения к узлу
        /// в том направлении.
        ///
        /// Параметр dir соответствует направлениям в сетке следующим образом:
        /// <code>
        ///         Z
        ///         |
        ///         |
        ///
        ///      6  2  5
        ///       \ | /
        /// --  3 - X - 1  ----- X
        ///       / | \
        ///      7  0  4
        ///
        ///         |
        ///         |
        /// </code>
        ///
        /// See: GetConnections
        ///
        /// Note: This method only takes grid connections into account, not custom connections (i.e. those added using <see cref="AddConnection"/> or using node links).
        /// </summary>
        public abstract GridNodeBase GetNeighbourAlongDirection(int direction);

        /// <summary>
        /// Истинно, если узел имеет соединение с соседним узлом в указанном направлении.
        ///
        /// Параметр dir соответствует направлениям в сетке следующим образом:
        /// <code>
        ///         Z
        ///         |
        ///         |
        ///
        ///      6  2  5
        ///       \ | /
        /// --  3 - X - 1  ----- X
        ///       / | \
        ///      7  0  4
        ///
        ///         |
        ///         |
        /// </code>
        ///
        /// See: <see cref="GetConnections"/>
        /// See: <see cref="GetNeighbourAlongDirection"/>
        ///
        /// Note: This method only takes grid connections into account, not custom connections (i.e. those added using <see cref="AddConnection"/> or using node links).
        /// </summary>
        public virtual bool HasConnectionInDirection (int direction) {
			// TODO: Can be optimized if overriden in each subclass
			return GetNeighbourAlongDirection(direction) != null;
		}

		public override bool ContainsConnection (GraphNode node) {
#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
			if (connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					if (connections[i].node == node) {
						return true;
					}
				}
			}
#endif

			for (int i = 0; i < 8; i++) {
				if (node == GetNeighbourAlongDirection(i)) {
					return true;
				}
			}

			return false;
		}

#if ASTAR_GRID_NO_CUSTOM_CONNECTIONS
		public override void AddConnection (GraphNode node, uint cost) {
			throw new System.NotImplementedException("GridNodes do not have support for adding manual connections with your current settings."+
				"\nPlease disable ASTAR_GRID_NO_CUSTOM_CONNECTIONS in the Optimizations tab in the A* Inspector");
		}

		public override void RemoveConnection (GraphNode node) {
			throw new System.NotImplementedException("GridNodes do not have support for adding manual connections with your current settings."+
				"\nPlease disable ASTAR_GRID_NO_CUSTOM_CONNECTIONS in the Optimizations tab in the A* Inspector");
		}

		public void ClearCustomConnections (bool alsoReverse) {
		}
#else
		/// <summary>Same as <see cref="ClearConnections"/>, but does not clear grid connections, only custom ones (e.g added by <see cref="AddConnection"/> or a NodeLink component)</summary>
		public void ClearCustomConnections (bool alsoReverse) {
			if (connections != null) for (int i = 0; i < connections.Length; i++) connections[i].node.RemoveConnection(this);
			connections = null;
			AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
		}

		public override void ClearConnections (bool alsoReverse) {
			ClearCustomConnections(alsoReverse);
		}

		public override void GetConnections (System.Action<GraphNode> action) {
			if (connections != null) for (int i = 0; i < connections.Length; i++) action(connections[i].node);
		}

		public override void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
			ushort pid = handler.PathID;

			if (connections != null) for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i].node;
					PathNode otherPN = handler.GetPathNode(other);
					if (otherPN.parent == pathNode && otherPN.pathID == pid) other.UpdateRecursiveG(path, otherPN, handler);
				}
		}

		public override void Open (Path path, PathNode pathNode, PathHandler handler) {
			ushort pid = handler.PathID;

			if (connections != null) for (int i = 0; i < connections.Length; i++) {
					GraphNode other = connections[i].node;
					if (!path.CanTraverse(other)) continue;

					PathNode otherPN = handler.GetPathNode(other);

					uint tmpCost = connections[i].cost;

					if (otherPN.pathID != pid) {
						otherPN.parent = pathNode;
						otherPN.pathID = pid;

						otherPN.cost = tmpCost;

						otherPN.H = path.CalculateHScore(other);
						otherPN.UpdateG(path);

						//Debug.Log ("G " + otherPN.G + " F " + otherPN.F);
						handler.heap.Add(otherPN);
						//Debug.DrawRay ((Vector3)otherPN.node.Position, Vector3.up,Color.blue);
					} else {
						// Sorry for the huge number of #ifs

						//If not we can test if the path from the current node to this one is a better one then the one already used

#if ASTAR_NO_TRAVERSAL_COST
						if (pathNode.G+tmpCost < otherPN.G)
#else
						if (pathNode.G+tmpCost+path.GetTraversalCost(other) < otherPN.G)
#endif
						{
							//Debug.Log ("Path better from " + NodeIndex + " to " + otherPN.node.NodeIndex + " " + (pathNode.G+tmpCost+path.GetTraversalCost(other)) + " < " + otherPN.G);
							otherPN.cost = tmpCost;

							otherPN.parent = pathNode;

							other.UpdateRecursiveG(path, otherPN, handler);
						}
					}
				}
		}

        /// <summary>
        /// Добавьте соединение с этого узла к указанному узлу.
        /// Если подключение уже существует, стоимость будет просто обновлена без добавления дополнительного подключения.
        ///
        /// Примечание: Добавляется только одностороннее соединение. Рассмотрите возможность выполнения той же функции на другом узле, 
		/// чтобы получить двустороннее соединение.
        ///
        /// Обратите внимание, что при этом всегда будет добавляться пользовательское соединение, которое немного медленнее, 
		/// чем список внутренних подключений к соседним узлам сетки. Если вы хотите изменять соединения только между узлами сетки,
		/// расположенными рядом друг с другом, и не хотите настраиваемых затрат, то использование <see cref="SetConnectionInternal"/> может быть лучшим вариантом.
        /// </summary>
        public override void AddConnection (GraphNode node, uint cost) {
			if (node == null) throw new System.ArgumentNullException();

			if (connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					if (connections[i].node == node) {
						connections[i].cost = cost;
						return;
					}
				}
			}

			int connLength = connections != null ? connections.Length : 0;

			var newconns = new Connection[connLength+1];
			for (int i = 0; i < connLength; i++) {
				newconns[i] = connections[i];
			}

			newconns[connLength] = new Connection(node, cost);

			connections = newconns;
			AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
		}

		/// <summary>
		/// Removes any connection from this node to the specified node.
		/// If no such connection exists, nothing will be done.
		///
		/// Note: This only removes the connection from this node to the other node.
		/// You may want to call the same function on the other node to remove its eventual connection
		/// to this node.
		///
		/// Version: Before 4.3.48 This method only handled custom connections (those added using link components or the AddConnection method).
		/// Regular grid connections had to be added or removed using <see cref="Pathfinding.GridNode.SetConnectionInternal"/>. Starting with 4.3.48 this method
		/// can remove all types of connections.
		/// </summary>
		public override void RemoveConnection (GraphNode node) {
			if (connections == null) return;

			for (int i = 0; i < connections.Length; i++) {
				if (connections[i].node == node) {
					int connLength = connections.Length;

					var newconns = new Connection[connLength-1];
					for (int j = 0; j < i; j++) {
						newconns[j] = connections[j];
					}
					for (int j = i+1; j < connLength; j++) {
						newconns[j-1] = connections[j];
					}

					connections = newconns;
					AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
					return;
				}
			}
		}

		public override void SerializeReferences (GraphSerializationContext ctx) {
			// TODO: Deduplicate code
			if (connections == null) {
				ctx.writer.Write(-1);
			} else {
				ctx.writer.Write(connections.Length);
				for (int i = 0; i < connections.Length; i++) {
					ctx.SerializeNodeReference(connections[i].node);
					ctx.writer.Write(connections[i].cost);
				}
			}
		}

		public override void DeserializeReferences (GraphSerializationContext ctx) {
			// Grid nodes didn't serialize references before 3.8.3
			if (ctx.meta.version < AstarSerializer.V3_8_3)
				return;

			int count = ctx.reader.ReadInt32();

			if (count == -1) {
				connections = null;
			} else {
				connections = new Connection[count];

				for (int i = 0; i < count; i++) {
					connections[i] = new Connection(ctx.DeserializeNodeReference(), ctx.reader.ReadUInt32());
				}
			}
		}
#endif
	}
}
