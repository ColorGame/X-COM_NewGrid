using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding
{
    using Pathfinding.Util;

    /// <summary>
    /// Соединяет два узла через два промежуточных точечных узла.
    /// В отличие от компонента NodeLink, этот тип ссылки не будет соединять узлы напрямую
    /// вместо этого он создаст два точечных узла в начальной и конечной позиции этой ссылки и соединит
    /// через эти узлы.
    ///
    /// Если ближайший узел к этому объекту называется A, а ближайший узел к конечному преобразованию вызывается
    /// D, тогда он создаст один точечный узел в позиции этого объекта (назовем его B) и один точечный узел в
    /// положение конечного преобразования (назовем его C), затем оно соединит A с B, B с C и C с D.
    ///
    /// Этот тип ссылки можно обнаружить при переходе по ссылке, поскольку он имеет эти специальные точечные узлы посередине.
    /// Ссылка, соответствующая одному из этих промежуточных узлов, может быть получена с помощью метода <see cref="GetNodeLink"/>
    /// что может быть очень полезно, если вы хотите, например, воспроизвести анимацию, связанную с конкретной ссылкой, при переходе по ссылке.
    ///
    /// Смотрите: Пример scene RecastExample2 содержит несколько ссылок, на которые вы можете взглянуть, чтобы увидеть, как они используются.
    /// </summary>
    [AddComponentMenu("Pathfinding/Link2")]
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_node_link2.php")]
    public class NodeLink2 : GraphModifier
    {
        protected static Dictionary<GraphNode, NodeLink2> reference = new Dictionary<GraphNode, NodeLink2>();
        public static NodeLink2 GetNodeLink(GraphNode node)
        {
            NodeLink2 v;

            reference.TryGetValue(node, out v);
            return v;
        }

        /// <summary>Конечное положение звена</summary>
        public Transform end;

        /// <summary>
        /// Соединение будет в этот раз проходить сложнее / медленнее.
        /// Обратите внимание, что значения ниже 1 не всегда заставят навигатор выбирать этот путь вместо другого, даже если этот должен
        /// приведет к снижению общей стоимости, если вы также не настроите эвристическую шкалу A* Inspector -> Settings -> Pathfinding or disable the heuristic altogether.
        /// </summary>
        public float costFactor = 1.0f;

        /// <summary>Установите одностороннее соединение</summary>
        public bool oneWay = false;

        public Transform StartTransform
        {
            get { return transform; }
        }

        public Transform EndTransform
        {
            get { return end; }
        }

        public PointNode startNode { get; private set; }
        public PointNode endNode { get; private set; }
        GraphNode connectedNode1, connectedNode2;
        Vector3 clamped1, clamped2;
        bool postScanCalled = false;

        [System.Obsolete("Вместо этого используйте начальный узел (строчные s)")]
        public GraphNode StartNode
        {
            get { return startNode; }
        }

        [System.Obsolete("Вместо этого используйте концевую сноску (строчные e)")]
        public GraphNode EndNode
        {
            get { return endNode; }
        }

        public override void OnPostScan()
        {
            InternalOnPostScan();
        }

        public void InternalOnPostScan()
        {
            if (EndTransform == null || StartTransform == null) return;

#if ASTAR_NO_POINT_GRAPH
			throw new System.Exception("Point graph is not included. Check your A* optimization settings.");
#else
            if (AstarPath.active.data.pointGraph == null)
            {
                var graph = AstarPath.active.data.AddGraph(typeof(PointGraph)) as PointGraph;
                graph.name = "PointGraph (used for node links)";
            }

            if (startNode != null && startNode.Destroyed)
            {
                reference.Remove(startNode);
                startNode = null;
            }

            if (endNode != null && endNode.Destroyed)
            {
                reference.Remove(endNode);
                endNode = null;
            }

            // Создайте новые узлы на точечном графике
            if (startNode == null) startNode = AstarPath.active.data.pointGraph.AddNode((Int3)StartTransform.position);
            if (endNode == null) endNode = AstarPath.active.data.pointGraph.AddNode((Int3)EndTransform.position);

            connectedNode1 = null;
            connectedNode2 = null;

            if (startNode == null || endNode == null)
            {
                startNode = null;
                endNode = null;
                return;
            }

            postScanCalled = true;
            reference[startNode] = this;
            reference[endNode] = this;
            Apply(true);
#endif
        }

        public override void OnGraphsPostUpdate()
        {
            // Не утруждайте себя запуском этого сейчас, так как On PostS can все равно будет вызван позже
            if (AstarPath.active.isScanning)
                return;

            if (connectedNode1 != null && connectedNode1.Destroyed)
            {
                connectedNode1 = null;
            }
            if (connectedNode2 != null && connectedNode2.Destroyed)
            {
                connectedNode2 = null;
            }

            if (!postScanCalled)
            {
                OnPostScan();
            }
            else
            {
                Apply(false);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

#if !ASTAR_NO_POINT_GRAPH
            if (Application.isPlaying && AstarPath.active != null && AstarPath.active.data != null && AstarPath.active.data.pointGraph != null && !AstarPath.active.isScanning)
            {
                // Вызовите обновление графиков после публикации как можно скорее, когда будет безопасно обновлять графики
                AstarPath.active.AddWorkItem(OnGraphsPostUpdate);
            }
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            postScanCalled = false;

            if (startNode != null) reference.Remove(startNode);
            if (endNode != null) reference.Remove(endNode);

            if (startNode != null && endNode != null)
            {
                startNode.RemoveConnection(endNode);
                endNode.RemoveConnection(startNode);

                if (connectedNode1 != null && connectedNode2 != null)
                {
                    startNode.RemoveConnection(connectedNode1);
                    connectedNode1.RemoveConnection(startNode);

                    endNode.RemoveConnection(connectedNode2);
                    connectedNode2.RemoveConnection(endNode);
                }
            }
        }

        void RemoveConnections(GraphNode node)
        {
            //ЧТО ДЕЛАТЬ, возможно, было бы лучше заменить соединение
            node.ClearConnections(true);
        }

        [ContextMenu("Recalculate neighbours")]
        void ContextApplyForce()
        {
            if (Application.isPlaying)
            {
                Apply(true);
            }
        }

        public void Apply(bool forceNewCheck) // Применять
        {
            //ЗАДАЧИ
            //Эта функция предполагает, что соединения с узлами n1, n2 никогда не нужно будет удалять в будущем (например, потому что узлы перемещаются или что-то в этом роде).
            NNConstraint nn = NNConstraint.None;
            int graph = (int)startNode.GraphIndex;

            //Поиск по всем графикам, кроме того, на котором находятся начальный и конечный узлы
            nn.graphMask = ~(1 << graph);

            startNode.SetPosition((Int3)StartTransform.position);
            endNode.SetPosition((Int3)EndTransform.position);

            RemoveConnections(startNode);
            RemoveConnections(endNode);

            uint cost = (uint)Mathf.RoundToInt(((Int3)(StartTransform.position - EndTransform.position)).costMagnitude * costFactor);
            startNode.AddConnection(endNode, cost);
            endNode.AddConnection(startNode, cost);

            if (connectedNode1 == null || forceNewCheck)
            {
                var info = AstarPath.active.GetNearest(StartTransform.position, nn);
                connectedNode1 = info.node;
                clamped1 = info.position;
            }

            if (connectedNode2 == null || forceNewCheck)
            {
                var info = AstarPath.active.GetNearest(EndTransform.position, nn);
                connectedNode2 = info.node;
                clamped2 = info.position;
            }

            if (connectedNode2 == null || connectedNode1 == null) return;

            //Добавьте соединения между узлами или замените старые соединения, если они существуют
            connectedNode1.AddConnection(startNode, (uint)Mathf.RoundToInt(((Int3)(clamped1 - StartTransform.position)).costMagnitude * costFactor));
            if (!oneWay) connectedNode2.AddConnection(endNode, (uint)Mathf.RoundToInt(((Int3)(clamped2 - EndTransform.position)).costMagnitude * costFactor));

            if (!oneWay) startNode.AddConnection(connectedNode1, (uint)Mathf.RoundToInt(((Int3)(clamped1 - StartTransform.position)).costMagnitude * costFactor));
            endNode.AddConnection(connectedNode2, (uint)Mathf.RoundToInt(((Int3)(clamped2 - EndTransform.position)).costMagnitude * costFactor));
        }

        private readonly static Color GizmosColor = new Color(206.0f / 255.0f, 136.0f / 255.0f, 48.0f / 255.0f, 0.5f);
        private readonly static Color GizmosColorSelected = new Color(235.0f / 255.0f, 123.0f / 255.0f, 32.0f / 255.0f, 1.0f);

        public virtual void OnDrawGizmosSelected()
        {
            OnDrawGizmos(true);
        }

        public void OnDrawGizmos()
        {
            OnDrawGizmos(false);
        }

        public void OnDrawGizmos(bool selected)
        {
            Color color = selected ? GizmosColorSelected : GizmosColor;

            if (StartTransform != null)
            {
                Draw.Gizmos.CircleXZ(StartTransform.position, 0.4f, color);
            }
            if (EndTransform != null)
            {
                Draw.Gizmos.CircleXZ(EndTransform.position, 0.4f, color);
            }

            if (StartTransform != null && EndTransform != null)
            {
                Draw.Gizmos.Bezier(StartTransform.position, EndTransform.position, color);
                if (selected)
                {
                    Vector3 cross = Vector3.Cross(Vector3.up, (EndTransform.position - StartTransform.position)).normalized;
                    Draw.Gizmos.Bezier(StartTransform.position + cross * 0.1f, EndTransform.position + cross * 0.1f, color);
                    Draw.Gizmos.Bezier(StartTransform.position - cross * 0.1f, EndTransform.position - cross * 0.1f, color);
                }
            }
        }

        internal static void SerializeReferences(Pathfinding.Serialization.GraphSerializationContext ctx)
        {
            var links = GetModifiersOfType<NodeLink2>();

            ctx.writer.Write(links.Count);
            foreach (var link in links)
            {
                ctx.writer.Write(link.uniqueID);
                ctx.SerializeNodeReference(link.startNode);
                ctx.SerializeNodeReference(link.endNode);
                ctx.SerializeNodeReference(link.connectedNode1);
                ctx.SerializeNodeReference(link.connectedNode2);
                ctx.SerializeVector3(link.clamped1);
                ctx.SerializeVector3(link.clamped2);
                ctx.writer.Write(link.postScanCalled);
            }
        }

        internal static void DeserializeReferences(Pathfinding.Serialization.GraphSerializationContext ctx)
        {
            int count = ctx.reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var linkID = ctx.reader.ReadUInt64();
                var startNode = ctx.DeserializeNodeReference();
                var endNode = ctx.DeserializeNodeReference();
                var connectedNode1 = ctx.DeserializeNodeReference();
                var connectedNode2 = ctx.DeserializeNodeReference();
                var clamped1 = ctx.DeserializeVector3();
                var clamped2 = ctx.DeserializeVector3();
                var postScanCalled = ctx.reader.ReadBoolean();

                GraphModifier link;
                if (usedIDs.TryGetValue(linkID, out link))
                {
                    var link2 = link as NodeLink2;
                    if (link2 != null)
                    {
                        if (startNode != null) reference[startNode] = link2;
                        if (endNode != null) reference[endNode] = link2;

                        // If any nodes happened to be registered right now
                        if (link2.startNode != null) reference.Remove(link2.startNode);
                        if (link2.endNode != null) reference.Remove(link2.endNode);

                        link2.startNode = startNode as PointNode;
                        link2.endNode = endNode as PointNode;
                        link2.connectedNode1 = connectedNode1;
                        link2.connectedNode2 = connectedNode2;
                        link2.postScanCalled = postScanCalled;
                        link2.clamped1 = clamped1;
                        link2.clamped2 = clamped2;
                    }
                    else
                    {
                        throw new System.Exception("Попытался десериализовать ссылку NodeLink2, но ссылка была неправильного типа или она была уничтожена.\nЕсли NodeLink2 включен в сериализованные графические данные, тот же компонент NodeLink2 должен присутствовать в сцене при загрузке графических данных.");
                    }
                }
                else
                {
                    throw new System.Exception("Попытался десериализовать ссылку NodeLink2, но ссылку не удалось найти в сцене.\nЕсли NodeLink2 включен в сериализованные графические данные, тот же компонент NodeLink2 должен присутствовать в сцене при загрузке графических данных.");
                }
            }
        }
    }
}
