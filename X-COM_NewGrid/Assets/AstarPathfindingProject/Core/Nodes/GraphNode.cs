using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;

namespace Pathfinding {
	using Pathfinding.Util;

    /// <summary>������������ ����� ���������� � ������ �����</summary>
    public struct Connection {
        /// <summary>����, � �������� ������������ ��� ����������</summary>
        public GraphNode node;

        /// <summary>
        /// ��������� ����������� �� ����� ����������.
        /// ��������� � 1000 �������� ������������� ��������� ����������� ����� ������� �������.
        /// </summary>
        public uint cost;

        /// <summary>
        /// ������� ����� ����, ������� ���������� ��� ����������.
        /// ������������ ��� ����� �����.
        /// �������� 0 ������������� ������������� ������� ��� ������� 0 � ������� 1 � ����. 1 ������������� �������� 1 � 2 � �.�.
        /// �������� <see cref="NoSharedEdge"/> ��������, ��� ��� ���������� ������ �� ���������� �����-���� ������� (� �������� ��� ������������ ��� ������ ��� �����).
        ///
        /// ����������: ��-�� ������������ ���� <see cref="node"/> � <see cref="cost"/> ���������� 12 ����, ������� ����� ���������
        /// �� 16 ���� ��� ������������� � �������, ���� ���� ��� ���� ����� �������.
        /// ����� �������, ��� ���� �� ������������ ���������� ������������� ������.
        ///
        /// See: TriangleMeshNode
        /// See: TriangleMeshNode.AddConnection
        /// </summary>
        public byte shapeEdge;

		public const byte NoSharedEdge = 0xFF;

		public Connection (GraphNode node, uint cost, byte shapeEdge = NoSharedEdge) {
			this.node = node;
			this.cost = cost;
			this.shapeEdge = shapeEdge;
		}

		public override int GetHashCode () {
			return node.GetHashCode() ^ (int)cost;
		}

		public override bool Equals (object obj) {
			if (obj == null) return false;
			var conn = (Connection)obj;
			return conn.node == node && conn.cost == cost && conn.shapeEdge == shapeEdge;
		}
	}

    /// <summary>������� ����� ��� ���� �����</summary>
    public abstract class GraphNode {
        /// <summary>���������� ���������� ������. ����� ������ ��������� ������� ��������, ����� ���<see cref="TemporaryFlag1"/> and <see cref="TemporaryFlag2"/>.</summary>
        private int nodeIndex;

        /// <summary>
        /// ������� ����, ���������� ��������� ���������� ������.
        /// See: Walkable
        /// See: Area
        /// See: GraphIndex
        /// See: Tag
        /// </summary>
        protected uint flags;

#if !ASTAR_NO_PENALTY
        /// <summary>
        /// ��������� ������ �� �������� �� ����� ����.
        /// ��� ����� ���� ������������ ��� ����, ����� ��������� / ��������� ����������� �� ������������ �����.
        ///
        /// /// ����� � ������� 1000 (Int 3.��������) ������������� ��������� ����������� ����� ������� �������.
        ///
        /// ��������: ������-���������� (������� ������ �������� � ������-������������)
        /// </summary>
        private uint penalty;
#endif

        /// <summary>
        /// ����, � �������� ����������� ���� ����.
        ///
        /// ���� �� ������, ��� ���� ����������� � ������������� ���� �����, �� ������ �������� ��� � ����� ����:
        /// <code>
        /// GraphNode node = ...;
        /// GridGraph graph = node.Graph as GridGraph;
        /// </code>
        ///
        /// ������ �������� null, ���� ���� ��� ���������.
        /// </summary>
        public NavGraph Graph {
			get {
				return Destroyed ? null : AstarData.GetGraph(this);
			}
		}

        /// <summary>����������� ��� ���� �����.</summary>
        protected GraphNode (AstarPath astar) {
			if (!System.Object.ReferenceEquals(astar, null)) {
				this.nodeIndex = astar.GetNewNodeIndex();
				astar.InitializeNode(this);
			} else {
				throw new System.Exception("��� ��������� ������� AstarPath, � �������� ����� ���� �� �����������");
			}
		}  
		
		/// <summary>
        /// ���������� ����.
        /// ������� ��� ��������� ������ ������ ����, ������������ ��� ����� ����.
        /// ���� �������� �� ����� ����� ������ ��� �����, ����� ��� ������������, � ��� ����� ����� ���� ���� �����������.
        /// � ��������� ������ ����� ���������� ������ ������.
        ///
        /// ����� ������ �������� <see cref="Destroyed"/> ������ �������� true, � ����������� ������ ����� ������ ������ �� �������.
        ///
        /// ����������: ��������������, ��� ������� �������� ��������� AstarPath �������� ��� �� �����, ������� ������ ���� ����.
        ///
        /// ��������������: ������ ���������� ������ ������������ �������� �� �� ����������� �����
        /// </summary>
        public void Destroy () {
			if (Destroyed) return;

			ClearConnections(true);

			if (AstarPath.active != null) {
				AstarPath.active.DestroyNode(this);
			}
			NodeIndex = DestroyedNodeIndex;
		}

		public bool Destroyed {
			get {
				return NodeIndex == DestroyedNodeIndex;
			}
		}

        // ���� ���-�� ������� ����� 200 ��������� �����, �� ���� ������ �� ��� ������, ������ � ���� ������ � ����-�� ��������� ��������� ����� �������� ��������, ����� ��� �������� ����������� ������
        const int NodeIndexMask = 0xFFFFFFF;
		const int DestroyedNodeIndex = NodeIndexMask - 1;
		const int TemporaryFlag1Mask = 0x10000000;
		const int TemporaryFlag2Mask = 0x20000000;

        /// <summary>
        /// ���������� ���������� ������.
        /// ������ ���� ������� ���������� ������.
        /// ���� ������ �� ����������� �����������, ��������, � ���������� ���� �� �������.
        /// </summary>
        public int NodeIndex { get { return nodeIndex & NodeIndexMask; } private set { nodeIndex = (nodeIndex & ~NodeIndexMask) | value; } }

        /// <summary>
        /// ��������� ���� ��� ���������� �����.
        /// ����� �������������� ������ � ������ Unity. ������ ���� �������� �� �������� false ����� ������� �������������.
        /// </summary>
        internal bool TemporaryFlag1 { get { return (nodeIndex & TemporaryFlag1Mask) != 0; } set { nodeIndex = (nodeIndex & ~TemporaryFlag1Mask) | (value ? TemporaryFlag1Mask : 0); } }

		/// <summary>
		/// Temporary flag for internal purposes.
		/// May only be used in the Unity thread. Must be reset to false after every use.
		/// </summary>
		internal bool TemporaryFlag2 { get { return (nodeIndex & TemporaryFlag2Mask) != 0; } set { nodeIndex = (nodeIndex & ~TemporaryFlag2Mask) | (value ? TemporaryFlag2Mask : 0); } }

        /// <summary>
        /// ��������� ���� � ������� ������������.
        /// ����������: ������� ����������� ��� Int 3, � �� ��� Vector3.
        /// �� ������ ������������� Int 3 � Vector3, ��������� ����� ��������������.
        /// <code> var v3 = (Vector3)node.position; </code>
        /// </summary>
        public Int3 position;

		#region Constants
		/// <summary>Position of the walkable bit. See: <see cref="Walkable"/></summary>
		const int FlagsWalkableOffset = 0;
		/// <summary>Mask of the walkable bit. See: <see cref="Walkable"/></summary>
		const uint FlagsWalkableMask = 1 << FlagsWalkableOffset;

		/// <summary>Start of hierarchical node index bits. See: <see cref="HierarchicalNodeIndex"/></summary>
		const int FlagsHierarchicalIndexOffset = 1;
		/// <summary>Mask of hierarchical node index bits. See: <see cref="HierarchicalNodeIndex"/></summary>
		const uint HierarchicalIndexMask = (131072-1) << FlagsHierarchicalIndexOffset;

		/// <summary>Start of <see cref="IsHierarchicalNodeDirty"/> bits. See: <see cref="IsHierarchicalNodeDirty"/></summary>
		const int HierarchicalDirtyOffset = 18;

		/// <summary>Mask of the <see cref="IsHierarchicalNodeDirty"/> bit. See: <see cref="IsHierarchicalNodeDirty"/></summary>
		const uint HierarchicalDirtyMask = 1 << HierarchicalDirtyOffset;

		/// <summary>Start of graph index bits. See: <see cref="GraphIndex"/></summary>
		const int FlagsGraphOffset = 24;
		/// <summary>Mask of graph index bits. See: <see cref="GraphIndex"/></summary>
		const uint FlagsGraphMask = (256u-1) << FlagsGraphOffset;

		public const uint MaxHierarchicalNodeIndex = HierarchicalIndexMask >> FlagsHierarchicalIndexOffset;

		/// <summary>Max number of graphs-1</summary>
		public const uint MaxGraphIndex = FlagsGraphMask >> FlagsGraphOffset;

		/// <summary>Start of tag bits. See: <see cref="Tag"/></summary>
		const int FlagsTagOffset = 19;
		/// <summary>Mask of tag bits. See: <see cref="Tag"/></summary>
		const uint FlagsTagMask = (32-1) << FlagsTagOffset;

		#endregion

		#region Properties

		/// <summary>
		/// Holds various bitpacked variables.
		///
		/// Bit 0: <see cref="Walkable"/>
		/// Bits 1 through 17: <see cref="HierarchicalNodeIndex"/>
		/// Bit 18: <see cref="IsHierarchicalNodeDirty"/>
		/// Bits 19 through 23: <see cref="Tag"/>
		/// Bits 24 through 31: <see cref="GraphIndex"/>
		///
		/// Warning: You should pretty much never modify this property directly. Use the other properties instead.
		/// </summary>
		public uint Flags {
			get {
				return flags;
			}
			set {
				flags = value;
			}
		}

		/// <summary>
		/// Penalty cost for walking on this node.
		/// This can be used to make it harder/slower to walk over certain nodes.
		/// A cost of 1000 (<see cref="Pathfinding.Int3.Precision"/>) corresponds to the cost of moving 1 world unit.
		///
		/// See: graph-updates (view in online documentation for working links)
		/// </summary>
		public uint Penalty {
#if !ASTAR_NO_PENALTY
			get {
				return penalty;
			}
			set {
				if (value > 0xFFFFFF)
					Debug.LogWarning("Very high penalty applied. Are you sure negative values haven't underflowed?\n" +
						"Penalty values this high could with long paths cause overflows and in some cases infinity loops because of that.\n" +
						"Penalty value applied: "+value);
				penalty = value;
			}
#else
			get { return 0U; }
			set {}
#endif
		}

        /// <summary>
        /// True, ���� ���� �������� ��� �������.
        ///
        /// ��������: ������-���������� (������� ������ �������� � ������-������������)
        /// </summary>
        public bool Walkable {
			get {
				return (flags & FlagsWalkableMask) != 0;
			}
			set {
				flags = flags & ~FlagsWalkableMask | (value ? 1U : 0U) << FlagsWalkableOffset;
				AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
			}
		}

        /// <summary>
        /// ������������� ����, ���������� ���� ����.
        /// ���� �������� �� �������� ��������� ������������� �����, � ������� ���� ���� �� ������� ���� � ������� ������� ����.
        /// ��� ��������� ������������ ��� ��������� ���������� ������������ �����������, ������� ������������ ��� �������� ����������� ����, �������� �� ���� � ������� ����.
        ///
        /// See: <see cref="Pathfinding.HierarchicalGraph"/>
        ///
        /// Warning: This is an internal property and you should most likely not change it.
        /// </summary>
        internal int HierarchicalNodeIndex {
			get {
				return (int)((flags & HierarchicalIndexMask) >> FlagsHierarchicalIndexOffset);
			}
			set {
				flags = (flags & ~HierarchicalIndexMask) | (uint)(value << FlagsHierarchicalIndexOffset);
			}
		}

        /// <summary>��������� ���������� �����������</summary>
        internal bool IsHierarchicalNodeDirty {
			get {
				return (flags & HierarchicalDirtyMask) != 0;
			}
			set {
				flags = flags & ~HierarchicalDirtyMask | (value ? 1U : 0U) << HierarchicalDirtyOffset;
			}
		}

        /// <summary>
        /// ������������ ���������, ���������� ����.
        /// ��� ��������������� � ������ ��������� ����� � ���� ����� ������� ����� (���� ����� ��������� ������� ���������� �� "�������").
        /// ������ ������� ������������ ����� ����� ����� ����� �������, ��� ��� ����������� ���� ����� ������ ������ ������.
        ///
        /// See: https://en.wikipedia.org/wiki/Connected_component_(graph_theory)
        /// See: <see cref="Pathfinding.HierarchicalGraph"/>
        /// </summary>
        public uint Area {
			get {
				return AstarPath.active.hierarchicalGraph.GetConnectedComponent(HierarchicalNodeIndex);
			}
		}

		/// <summary>
		/// Graph which contains this node.
		/// See: <see cref="Pathfinding.AstarData.graphs"/>
		/// See: <see cref="Graph"/>
		/// </summary>
		public uint GraphIndex {
			get {
				return (flags & FlagsGraphMask) >> FlagsGraphOffset;
			}
			set {
				flags = flags & ~FlagsGraphMask | value << FlagsGraphOffset;
			}
		}

		/// <summary>
		/// Node tag.
		/// See: tags (view in online documentation for working links)
		/// See: graph-updates (view in online documentation for working links)
		/// </summary>
		public uint Tag {
			get {
				return (flags & FlagsTagMask) >> FlagsTagOffset;
			}
			set {
				flags = flags & ~FlagsTagMask | ((value << FlagsTagOffset) & FlagsTagMask);
			}
		}

        #endregion

        /// <summary>
        /// �������� �������, ��� ����������� ���� ����������.
        /// ��� ������������ ��� ���������� ��������� ����������� �������.
        ///
        /// See: <see cref="Pathfinding.HierarchicalGraph"/>
        ///
        /// �� ������ ������� ���� �����, ���� �� ��������� ����������� ��� ������������ ����, �� ��������� ������ �������� ������
        /// �����, ��� <see cref="Walkable"/> ������������� ��� <see cref="AddConnection"/>�����. ��������, ���� �� ������� �������� <see cref="Pathfinding.MeshNode.connections"/> ������, ������� ��� ����� ��� ������ ����� ������.
        /// </summary>
        public void SetConnectivityDirty () {
			AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
		}

       

		public virtual void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
            //�������, �� ��������� ���������� �� ���������
            pathNode.UpdateG(path);

			handler.heap.Add(pathNode);

			GetConnections((GraphNode other) => {
				PathNode otherPN = handler.GetPathNode(other);
				if (otherPN.parent == pathNode && otherPN.pathID == handler.PathID) other.UpdateRecursiveG(path, otherPN, handler);
			});
		}

        /// <summary>
        /// �������� ������� �� ����� ������������� � ����� ����.
        /// <code>
        /// node.GetConnections(connectedTo => {
        ///     Debug.DrawLine((Vector3)node.position, (Vector3)connectedTo.position, Color.red);
        /// });
        /// </code>
        ///
        /// �� ������ �������� ��� ������������ ���� � ������, �������� �����
        /// <code>
        /// var connections = new List<GraphNode>();
        /// node.GetConnections(connections.Add);
        /// </code>
        /// </summary>
        public abstract void GetConnections(System.Action<GraphNode> action);

        /// <summary>
        /// �������� ���������� � ����� ���� � ���������� ����.
        /// ���� ����������� ��� ����������, ��������� ����� ������ ��������� �
        /// �������������� ����������� �� �����������.
        ///
        /// ����������: ����������� ������ ������������� ����������. ����������� ����������� ������ ��� �� ������� �� ������ ����
        /// ����� �������� ������������ ����������.
        ///
        /// <code>
        /// AstarPath.active.AddWorkItem(new AstarWorkItem(ctx => {
        ///     // Connect two nodes
        ///     var node1 = AstarPath.active.GetNearest(transform.position, NNConstraint.None).node;
        ///     var node2 = AstarPath.active.GetNearest(transform.position + Vector3.right, NNConstraint.None).node;
        ///     var cost = (uint)(node2.position - node1.position).costMagnitude;
        ///     node1.AddConnection(node2, cost);
        ///     node2.AddConnection(node1, cost);
        ///
        ///     node1.ContainsConnection(node2); // True
        ///
        ///     node1.RemoveConnection(node2);
        ///     node2.RemoveConnection(node1);
        /// }));
        /// </code>
        /// </summary>
        public abstract void AddConnection(GraphNode node, uint cost);

        /// <summary>
        /// ������� ����� ���������� � ����� ���� � ��������� �����.
        /// ���� ������ ���������� �� ����������, ������ ������� �� �����.
        ///
        /// ����������: ��� ������ ������� ���������� � ����� ���� �� ������ ����.
        /// ��������, �� �������� ������� �� �� ������� �� ������ ����, ����� ������� ��� ��������� �����������
        /// � ����� ����.
        ///
        /// <code>
        /// AstarPath.active.AddWorkItem(new AstarWorkItem(ctx => {
        ///     // Connect two nodes
        ///     var node1 = AstarPath.active.GetNearest(transform.position, NNConstraint.None).node;
        ///     var node2 = AstarPath.active.GetNearest(transform.position + Vector3.right, NNConstraint.None).node;
        ///     var cost = (uint)(node2.position - node1.position).costMagnitude;
        ///     node1.AddConnection(node2, cost);
        ///     node2.AddConnection(node1, cost);
        ///
        ///     node1.ContainsConnection(node2); // True
        ///
        ///     node1.RemoveConnection(node2);
        ///     node2.RemoveConnection(node1);
        /// }));
        /// </code>
        /// </summary>
        public abstract void RemoveConnection(GraphNode node);

        /// <summary>������� ��� ���������� � ����� ����.</summary>
        /// <param name="alsoReverse">if true, neighbours will be requested to remove connections to this node.</param>
        public abstract void ClearConnections(bool alsoReverse);

        /// <summary>
        /// ���������, ����� �� ���� ���� ����������� � ���������� ����.
        ///
        /// <code>
        /// AstarPath.active.AddWorkItem(new AstarWorkItem(ctx => {
        ///     // Connect two nodes
        ///     var node1 = AstarPath.active.GetNearest(transform.position, NNConstraint.None).node;
        ///     var node2 = AstarPath.active.GetNearest(transform.position + Vector3.right, NNConstraint.None).node;
        ///     var cost = (uint)(node2.position - node1.position).costMagnitude;
        ///     node1.AddConnection(node2, cost);
        ///     node2.AddConnection(node1, cost);
        ///
        ///     node1.ContainsConnection(node2); // True
        ///
        ///     node1.RemoveConnection(node2);
        ///     node2.RemoveConnection(node1);
        /// }));
        /// </code>
        /// </summary>
        public virtual bool ContainsConnection (GraphNode node) {
			// Simple but slow default implementation
			bool contains = false;

			GetConnections(neighbour => {
				contains |= neighbour == node;
			});
			return contains;
		}

        /// <summary>
        /// �������� ������ � ����� ���� �� ��������� ����.
        /// ��� ������� ������ �������� ������ � ����� � ������ ������, ������� ��������� ��� ���� (���� � ������).
        ///
        /// ����������: True, ���� ����� ��� ������� ��������. �������� False, ���� ��� ��������� �����-�� ����������� ������ � �� ������� �������� ������.
        /// ���� ��� ������ ��������� � node1.GetPortal (node2,...) � node2.GetPortal (node1,...) ���������� �������� false,
		/// ����������� ������� �������� � ���������� � ���� ������� ����.
        ///
        /// ���������� �� ��������� ������ ���������� �������� false.
        ///
        /// ��� ������������� ��� ������� ����� �������� ����� ������ �������.
        ///
        /// See: http://digestingduck.blogspot.se/2010/03/simple-stupid-funnel-algorithm.html
        /// </summary>
        /// <param name="other">The node which is on the other side of the portal (strictly speaking it does not actually have to be on the other side of the portal though).</param>
        /// <param name="left">List of portal points on the left side of the funnel</param>
        /// <param name="right">List of portal points on the right side of the funnel</param>
        /// <param name="backwards">If this is true, the call was made on a node with the other node as the node before this one in the path.
        /// In this case you may choose to do nothing since a similar call will be made to the other node with this node referenced as other (but then with backwards = true).
        /// You do not have to care about switching the left and right lists, that is done for you already.</param>
        public virtual bool GetPortal (GraphNode other, List<Vector3> left, List<Vector3> right, bool backwards) {
			return false;
		}

        /// <summary>
        /// �������� ����.
        /// ������������ ��������� ��� ��������� A*.
        /// </summary>
        public abstract void Open(Path path, PathNode pathNode, PathHandler handler);

        /// <summary>������� ����������� ���� � ���������� ������� ��������</summary>
        public virtual float SurfaceArea () {
			return 0;
		}

		/// <summary>
		/// A random point on the surface of the node.
		/// For point nodes and other nodes which do not have a surface, this will always return the position of the node.
		/// </summary>
		public virtual Vector3 RandomPointOnSurface () {
			return (Vector3)position;
		}

        /// <summary>��������� ����� �� ����������� ����� ���� � ����� p</summary>
        public abstract Vector3 ClosestPointOnNode(Vector3 p);

        /// <summary>
        /// ���-���, ������������ ��� �������� ����, ��������� �� ������ � ����������.
        /// ���������, ����� ����� ���������� ���������� ��� ����.
        /// </summary>
        public virtual int GetGizmoHashCode () {
			// Some hashing, the constants are just some arbitrary prime numbers. #flags contains the info for #Tag and #Walkable
			return position.GetHashCode() ^ (19 * (int)Penalty) ^ (41 * (int)(flags & ~(HierarchicalIndexMask | HierarchicalDirtyMask)));
		}

        /// <summary>������������ ������ ���� � ������ ������</summary>
        public virtual void SerializeNode (GraphSerializationContext ctx) {
			//Write basic node data.
			ctx.writer.Write(Penalty);
			// Save all flags except the hierarchical node index and the dirty bit
			ctx.writer.Write(Flags & ~(HierarchicalIndexMask | HierarchicalDirtyMask));
		}

        /// <summary>������������� ������ ���� �� ������� ������</summary>
        public virtual void DeserializeNode (GraphSerializationContext ctx) {
			Penalty = ctx.reader.ReadUInt32();
			// Load all flags except the hierarchical node index and the dirty bit (they aren't saved in newer versions and older data should just be cleared)
			// Note that the dirty bit needs to be preserved here because it may already be set (due to the node being created)
			Flags = (ctx.reader.ReadUInt32() & ~(HierarchicalIndexMask | HierarchicalDirtyMask)) | (Flags & (HierarchicalIndexMask | HierarchicalDirtyMask));

			// Set the correct graph index (which might have changed, e.g if loading additively)
			GraphIndex = ctx.graphIndex;
		}

		/// <summary>
		/// Used to serialize references to other nodes e.g connections.
		/// Use the GraphSerializationContext.GetNodeIdentifier and
		/// GraphSerializationContext.GetNodeFromIdentifier methods
		/// for serialization and deserialization respectively.
		///
		/// Nodes must override this method and serialize their connections.
		/// Graph generators do not need to call this method, it will be called automatically on all
		/// nodes at the correct time by the serializer.
		/// </summary>
		public virtual void SerializeReferences (GraphSerializationContext ctx) {
		}

		/// <summary>
		/// Used to deserialize references to other nodes e.g connections.
		/// Use the GraphSerializationContext.GetNodeIdentifier and
		/// GraphSerializationContext.GetNodeFromIdentifier methods
		/// for serialization and deserialization respectively.
		///
		/// Nodes must override this method and serialize their connections.
		/// Graph generators do not need to call this method, it will be called automatically on all
		/// nodes at the correct time by the serializer.
		/// </summary>
		public virtual void DeserializeReferences (GraphSerializationContext ctx) {
		}
	}

	public abstract class MeshNode : GraphNode {
		protected MeshNode (AstarPath astar) : base(astar) {
		}

		/// <summary>
		/// All connections from this node.
		/// See: <see cref="AddConnection"/>
		/// See: <see cref="RemoveConnection"/>
		///
		/// Note: If you modify this array or the contents of it you must call <see cref="SetConnectivityDirty"/>.
		/// </summary>
		public Connection[] connections;

		/// <summary>Get a vertex of this node.</summary>
		/// <param name="i">vertex index. Must be between 0 and #GetVertexCount (exclusive).</param>
		public abstract Int3 GetVertex(int i);

		/// <summary>
		/// Number of corner vertices that this node has.
		/// For example for a triangle node this will return 3.
		/// </summary>
		public abstract int GetVertexCount();

		/// <summary>
		/// Closest point on the surface of this node when seen from above.
		/// This is usually very similar to <see cref="ClosestPointOnNode"/> but when the node is in a slope this can be significantly different.
		/// [Open online documentation to see images]
		/// When the blue point in the above image is used as an argument this method call will return the green point while the <see cref="ClosestPointOnNode"/> method will return the red point.
		/// </summary>
		public abstract Vector3 ClosestPointOnNodeXZ(Vector3 p);

		public override void ClearConnections (bool alsoReverse) {
			// Remove all connections to this node from our neighbours
			if (alsoReverse && connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					// Null check done here because NavmeshTile.Destroy
					// requires it for some optimizations it does
					// Normally connection elements are never null
					if (connections[i].node != null) {
						connections[i].node.RemoveConnection(this);
					}
				}
			}

			ArrayPool<Connection>.Release(ref connections, true);
			AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
		}

		public override void GetConnections (System.Action<GraphNode> action) {
			if (connections == null) return;
			for (int i = 0; i < connections.Length; i++) action(connections[i].node);
		}

		public override bool ContainsConnection (GraphNode node) {
			for (int i = 0; i < connections.Length; i++) if (connections[i].node == node) return true;
			return false;
		}

		public override void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
			pathNode.UpdateG(path);

			handler.heap.Add(pathNode);

			for (int i = 0; i < connections.Length; i++) {
				GraphNode other = connections[i].node;
				PathNode otherPN = handler.GetPathNode(other);
				if (otherPN.parent == pathNode && otherPN.pathID == handler.PathID) {
					other.UpdateRecursiveG(path, otherPN, handler);
				}
			}
		}

		/// <summary>
		/// Add a connection from this node to the specified node.
		///
		/// If the connection already exists, the cost will simply be updated and
		/// no extra connection added.
		///
		/// Note: Only adds a one-way connection. Consider calling the same function on the other node
		/// to get a two-way connection.
		/// </summary>
		/// <param name="node">Node to add a connection to</param>
		/// <param name="cost">Cost of traversing the connection. A cost of 1000 corresponds approximately to the cost of moving 1 world unit.</param>
		public override void AddConnection (GraphNode node, uint cost) {
			AddConnection(node, cost, Connection.NoSharedEdge);
		}

		/// <summary>
		/// Add a connection from this node to the specified node.
		/// See: Pathfinding.Connection.edge
		///
		/// If the connection already exists, the cost will simply be updated and
		/// no extra connection added.
		///
		/// Note: Only adds a one-way connection. Consider calling the same function on the other node
		/// to get a two-way connection.
		/// </summary>
		/// <param name="node">Node to add a connection to</param>
		/// <param name="cost">Cost of traversing the connection. A cost of 1000 corresponds approximately to the cost of moving 1 world unit.</param>
		/// <param name="shapeEdge">Which edge on the shape of this node to use or #Connection.NoSharedEdge if no edge is used.</param>
		public void AddConnection (GraphNode node, uint cost, byte shapeEdge) {
			if (node == null) throw new System.ArgumentNullException();

			// Check if we already have a connection to the node
			if (connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					if (connections[i].node == node) {
						// Just update the cost for the existing connection
						connections[i].cost = cost;
						// Update edge only if it was a definite edge, otherwise reuse the existing one
						// This makes it possible to use the AddConnection(node,cost) overload to only update the cost
						// without changing the edge which is required for backwards compatibility.
						connections[i].shapeEdge = shapeEdge != Connection.NoSharedEdge ? shapeEdge : connections[i].shapeEdge;
						return;
					}
				}
			}

			// Create new arrays which include the new connection
			int connLength = connections != null ? connections.Length : 0;

			var newconns = ArrayPool<Connection>.ClaimWithExactLength(connLength+1);
			for (int i = 0; i < connLength; i++) {
				newconns[i] = connections[i];
			}

			newconns[connLength] = new Connection(node, cost, (byte)shapeEdge);

			if (connections != null) {
				ArrayPool<Connection>.Release(ref connections, true);
			}

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
		/// </summary>
		public override void RemoveConnection (GraphNode node) {
			if (connections == null) return;

			// Iterate through all connections and check if there are any to the node
			for (int i = 0; i < connections.Length; i++) {
				if (connections[i].node == node) {
					// Create new arrays which have the specified node removed
					int connLength = connections.Length;

					var newconns = ArrayPool<Connection>.ClaimWithExactLength(connLength-1);
					for (int j = 0; j < i; j++) {
						newconns[j] = connections[j];
					}
					for (int j = i+1; j < connLength; j++) {
						newconns[j-1] = connections[j];
					}

					if (connections != null) {
						ArrayPool<Connection>.Release(ref connections, true);
					}

					connections = newconns;
					AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
					return;
				}
			}
		}

		/// <summary>Checks if point is inside the node when seen from above</summary>
		public virtual bool ContainsPoint (Int3 point) {
			return ContainsPoint((Vector3)point);
		}

		/// <summary>
		/// Checks if point is inside the node when seen from above.
		///
		/// Note that <see cref="ContainsPointInGraphSpace"/> is faster than this method as it avoids
		/// some coordinate transformations. If you are repeatedly calling this method
		/// on many different nodes but with the same point then you should consider
		/// transforming the point first and then calling ContainsPointInGraphSpace.
		/// <code>
		/// Int3 p = (Int3)graph.transform.InverseTransform(point);
		///
		/// node.ContainsPointInGraphSpace(p);
		/// </code>
		/// </summary>
		public abstract bool ContainsPoint(Vector3 point);

		/// <summary>
		/// Checks if point is inside the node in graph space.
		///
		/// In graph space the up direction is always the Y axis so in principle
		/// we project the triangle down on the XZ plane and check if the point is inside the 2D triangle there.
		/// </summary>
		public abstract bool ContainsPointInGraphSpace(Int3 point);

		public override int GetGizmoHashCode () {
			var hash = base.GetGizmoHashCode();

			if (connections != null) {
				for (int i = 0; i < connections.Length; i++) {
					hash ^= 17 * connections[i].GetHashCode();
				}
			}
			return hash;
		}

		public override void SerializeReferences (GraphSerializationContext ctx) {
			if (connections == null) {
				ctx.writer.Write(-1);
			} else {
				ctx.writer.Write(connections.Length);
				for (int i = 0; i < connections.Length; i++) {
					ctx.SerializeNodeReference(connections[i].node);
					ctx.writer.Write(connections[i].cost);
					ctx.writer.Write(connections[i].shapeEdge);
				}
			}
		}

		public override void DeserializeReferences (GraphSerializationContext ctx) {
			int count = ctx.reader.ReadInt32();

			if (count == -1) {
				connections = null;
			} else {
				connections = ArrayPool<Connection>.ClaimWithExactLength(count);

				for (int i = 0; i < count; i++) {
					connections[i] = new Connection(
						ctx.DeserializeNodeReference(),
						ctx.reader.ReadUInt32(),
						ctx.meta.version < AstarSerializer.V4_1_0 ? (byte)0xFF : ctx.reader.ReadByte()
						);
				}
			}
		}
	}
}
