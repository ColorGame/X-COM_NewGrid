using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;

namespace Pathfinding {
	using Pathfinding.Util;

    /// <summary>Представляет собой соединение с другим узлом</summary>
    public struct Connection {
        /// <summary>Узел, к которому подключается это соединение</summary>
        public GraphNode node;

        /// <summary>
        /// Стоимость перемещения по этому соединению.
        /// Стоимость в 1000 примерно соответствует стоимости перемещения одной мировой единицы.
        /// </summary>
        public uint cost;

        /// <summary>
        /// Сторона формы узла, которую использует это соединение.
        /// Используется для узлов сетки.
        /// Значение 0 соответствует использованию стороны для вершины 0 и вершины 1 в узле. 1 соответствует вершинам 1 и 2 и т.д.
        /// Значение <see cref="NoSharedEdge"/> означает, что это соединение вообще не использует какую-либо сторону (в основном это используется для связей вне сетки).
        ///
        /// Примечание: Из-за выравнивания поля <see cref="node"/> и <see cref="cost"/> используют 12 байт, которые будут заполнены
        /// до 16 байт при использовании в массиве, даже если это поле будет удалено.
        /// Таким образом, это поле не способствует увеличению использования памяти.
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

    /// <summary>Базовый класс для всех узлов</summary>
    public abstract class GraphNode {
        /// <summary>Внутренний уникальный индекс. Также хранит некоторые битовые значения, такие как<see cref="TemporaryFlag1"/> and <see cref="TemporaryFlag2"/>.</summary>
        private int nodeIndex;

        /// <summary>
        /// Битовое поле, содержащее несколько фрагментов данных.
        /// See: Walkable
        /// See: Area
        /// See: GraphIndex
        /// See: Tag
        /// </summary>
        protected uint flags;

#if !ASTAR_NO_PENALTY
        /// <summary>
        /// Стоимость штрафа за хождение по этому узлу.
        /// Это может быть использовано для того, чтобы усложнить / замедлить прохождение по определенным узлам.
        ///
        /// /// Штраф в размере 1000 (Int 3.Точность) соответствует стоимости прохождения одной мировой единицы.
        ///
        /// Смотрите: график-обновления (рабочие ссылки смотрите в онлайн-документации)
        /// </summary>
        private uint penalty;
#endif

        /// <summary>
        /// Граф, к которому принадлежит этот узел.
        ///
        /// Если вы знаете, что узел принадлежит к определенному типу графа, вы можете привести его к этому типу:
        /// <code>
        /// GraphNode node = ...;
        /// GridGraph graph = node.Graph as GridGraph;
        /// </code>
        ///
        /// Вернет значение null, если узел был уничтожен.
        /// </summary>
        public NavGraph Graph {
			get {
				return Destroyed ? null : AstarData.GetGraph(this);
			}
		}

        /// <summary>Конструктор для узла графа.</summary>
        protected GraphNode (AstarPath astar) {
			if (!System.Object.ReferenceEquals(astar, null)) {
				this.nodeIndex = astar.GetNewNodeIndex();
				astar.InitializeNode(this);
			} else {
				throw new System.Exception("Нет активного объекта AstarPath, к которому можно было бы привязаться");
			}
		}  
		
		/// <summary>
        /// Уничтожает узел.
        /// Очищает все временные данные поиска пути, используемые для этого узла.
        /// Граф отвечает за вызов этого метода для узлов, когда они уничтожаются, в том числе когда весь граф разрушается.
        /// В противном случае могут возникнуть утечки памяти.
        ///
        /// После вызова свойство <see cref="Destroyed"/> вернет значение true, и последующие вызовы этого метода ничего не сделают.
        ///
        /// Примечание: Предполагается, что текущий активный экземпляр AstarPath является тем же самым, который создал этот узел.
        ///
        /// Предупреждение: Должно вызываться только графическими классами на их собственных узлах
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

        // Если кто-то создаст более 200 миллионов узлов, то дела пойдут не так хорошо, однако в этот момент у кого-то наверняка возникнут более насущные проблемы, такие как нехватка оперативной памяти
        const int NodeIndexMask = 0xFFFFFFF;
		const int DestroyedNodeIndex = NodeIndexMask - 1;
		const int TemporaryFlag1Mask = 0x10000000;
		const int TemporaryFlag2Mask = 0x20000000;

        /// <summary>
        /// Внутренний уникальный индекс.
        /// Каждый узел получит уникальный индекс.
        /// Этот индекс не обязательно коррелирует, например, с положением узла на графике.
        /// </summary>
        public int NodeIndex { get { return nodeIndex & NodeIndexMask; } private set { nodeIndex = (nodeIndex & ~NodeIndexMask) | value; } }

        /// <summary>
        /// Временный флаг для внутренних целей.
        /// Может использоваться только в потоке Unity. Должно быть сброшено на значение false после каждого использования.
        /// </summary>
        internal bool TemporaryFlag1 { get { return (nodeIndex & TemporaryFlag1Mask) != 0; } set { nodeIndex = (nodeIndex & ~TemporaryFlag1Mask) | (value ? TemporaryFlag1Mask : 0); } }

		/// <summary>
		/// Temporary flag for internal purposes.
		/// May only be used in the Unity thread. Must be reset to false after every use.
		/// </summary>
		internal bool TemporaryFlag2 { get { return (nodeIndex & TemporaryFlag2Mask) != 0; } set { nodeIndex = (nodeIndex & ~TemporaryFlag2Mask) | (value ? TemporaryFlag2Mask : 0); } }

        /// <summary>
        /// Положение узла в мировом пространстве.
        /// Примечание: Позиция сохраняется как Int 3, а не как Vector3.
        /// Вы можете преобразовать Int 3 в Vector3, используя явное преобразование.
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
        /// True, если узел доступен для прохода.
        ///
        /// Смотрите: график-обновления (рабочие ссылки смотрите в онлайн-документации)
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
        /// Иерархический узел, содержащий этот узел.
        /// Граф разделен на кластеры небольших иерархических узлов, в которых есть путь от каждого узла к каждому другому узлу.
        /// Эта структура используется для ускорения вычислений подключенных компонентов, которые используются для быстрого определения того, доступен ли узел с другого узла.
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

        /// <summary>Некоторая внутренняя бухгалтерия</summary>
        internal bool IsHierarchicalNodeDirty {
			get {
				return (flags & HierarchicalDirtyMask) != 0;
			}
			set {
				flags = flags & ~HierarchicalDirtyMask | (value ? 1U : 0U) << HierarchicalDirtyOffset;
			}
		}

        /// <summary>
        /// Подключенный компонент, содержащий узел.
        /// Это визуализируется в режиме просмотра сцены в виде узлов разного цвета (если режим раскраски графика установлен на "Области").
        /// Каждая область представляет собой набор узлов таким образом, что нет допустимого пути между узлами разных цветов.
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
        /// Сообщите системе, что подключение узла изменилось.
        /// Это используется для вычисления связанных компонентов графика.
        ///
        /// See: <see cref="Pathfinding.HierarchicalGraph"/>
        ///
        /// Вы должны вызвать этот метод, если вы изменяете подключение или проходимость узла, не используя методы высокого уровня
        /// такие, как <see cref="Walkable"/> собственность или <see cref="AddConnection"/>метод. Например, если вы вручную измените <see cref="Pathfinding.MeshNode.connections"/> массив, который вам нужен для вызова этого метода.
        /// </summary>
        public void SetConnectivityDirty () {
			AstarPath.active.hierarchicalGraph.AddDirtyNode(this);
		}

       

		public virtual void UpdateRecursiveG (Path path, PathNode pathNode, PathHandler handler) {
            //Простая, но медленная реализация по умолчанию
            pathNode.UpdateG(path);

			handler.heap.Add(pathNode);

			GetConnections((GraphNode other) => {
				PathNode otherPN = handler.GetPathNode(other);
				if (otherPN.parent == pathNode && otherPN.pathID == handler.PathID) other.UpdateRecursiveG(path, otherPN, handler);
			});
		}

        /// <summary>
        /// Вызывает делегат со всеми подключениями с этого узла.
        /// <code>
        /// node.GetConnections(connectedTo => {
        ///     Debug.DrawLine((Vector3)node.position, (Vector3)connectedTo.position, Color.red);
        /// });
        /// </code>
        ///
        /// Вы можете добавить все подключенные узлы в список, подобный этому
        /// <code>
        /// var connections = new List<GraphNode>();
        /// node.GetConnections(connections.Add);
        /// </code>
        /// </summary>
        public abstract void GetConnections(System.Action<GraphNode> action);

        /// <summary>
        /// Добавьте соединение с этого узла к указанному узлу.
        /// Если подключение уже существует, стоимость будет просто обновлена и
        /// дополнительное подключение не добавляется.
        ///
        /// Примечание: Добавляется только одностороннее соединение. Рассмотрите возможность вызова той же функции на другом узле
        /// чтобы получить двустороннее соединение.
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
        /// Удаляет любое соединение с этого узла с указанным узлом.
        /// Если такого соединения не существует, ничего сделано не будет.
        ///
        /// Примечание: Это только удаляет соединение с этого узла на другой узел.
        /// Возможно, вы захотите вызвать ту же функцию на другом узле, чтобы удалить его возможное подключение
        /// к этому узлу.
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

        /// <summary>Удалите все соединения с этого узла.</summary>
        /// <param name="alsoReverse">if true, neighbours will be requested to remove connections to this node.</param>
        public abstract void ClearConnections(bool alsoReverse);

        /// <summary>
        /// Проверяет, имеет ли этот узел подключение к указанному узлу.
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
        /// Добавьте портал с этого узла на указанный узел.
        /// Эта функция должна добавить портал в левый и правый списки, который соединяет два узла (этот и другой).
        ///
        /// Возвращает: True, если вызов был признан успешным. Значение False, если был обнаружен какой-то неизвестный случай и не удалось добавить портал.
        /// Если оба вызова относятся к node1.GetPortal (node2,...) и node2.GetPortal (node1,...) возвращает значение false,
		/// модификатор воронки вернется к добавлению к пути позиций узла.
        ///
        /// Реализация по умолчанию просто возвращает значение false.
        ///
        /// При необходимости эта функция может добавить более одного портала.
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
        /// Откройте узел.
        /// Используется внутренне для алгоритма A*.
        /// </summary>
        public abstract void Open(Path path, PathNode pathNode, PathHandler handler);

        /// <summary>Площадь поверхности узла в квадратных мировых единицах</summary>
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

        /// <summary>Ближайшая точка на поверхности этого узла к точке p</summary>
        public abstract Vector3 ClosestPointOnNode(Vector3 p);

        /// <summary>
        /// Хэш-код, используемый для проверки того, нуждаются ли вещицы в обновлении.
        /// Изменится, когда могут измениться устройства для узла.
        /// </summary>
        public virtual int GetGizmoHashCode () {
			// Some hashing, the constants are just some arbitrary prime numbers. #flags contains the info for #Tag and #Walkable
			return position.GetHashCode() ^ (19 * (int)Penalty) ^ (41 * (int)(flags & ~(HierarchicalIndexMask | HierarchicalDirtyMask)));
		}

        /// <summary>Сериализовал данные узла в массив байтов</summary>
        public virtual void SerializeNode (GraphSerializationContext ctx) {
			//Write basic node data.
			ctx.writer.Write(Penalty);
			// Save all flags except the hierarchical node index and the dirty bit
			ctx.writer.Write(Flags & ~(HierarchicalIndexMask | HierarchicalDirtyMask));
		}

        /// <summary>Десериализует данные узла из массива байтов</summary>
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
