using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding
{
    using Pathfinding.Util;
    using static Pathfinding.BlockManager;

    /// <summary>
    /// Менеджер для сценариев блокировки, таких как SingleNodeBlocker.
    ///
    /// Это часть пошаговых утилит. Его можно использовать для любых игр, но в первую очередь она предназначена для пошаговых игр.
    ///
    /// See: TurnBasedAI
    /// See: turnbased (рабочие ссылки смотрите в онлайн-документации)
    /// </summary>
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_block_manager.php")]
    public class BlockManager : VersionedMonoBehaviour
    {
        public static BlockManager Instance { get; private set; }//(ОДНОЭЛЕМЕНТНЫЙ ПАТТЕРН SINGLETON)

        protected override void Awake()
        {
            base.Awake();
            Instance = this; //созданим экземпляр класса
        }       

        /// <summary>Содержит информацию о том, какие объекты-блокировщики отдельных узлов заблокировали конкретный узел</summary>
        Dictionary<GraphNode, List<SingleNodeBlocker>> blocked = new Dictionary<GraphNode, List<SingleNodeBlocker>>();

        public enum BlockMode
        {
            /// <summary>Все блокировщики, кроме тех, что указаны в списке TraversalProvider.selector, будут заблокированы</summary>
            AllExceptSelector,
            /// <summary>Только элементы в списке TraversalProvider.selector будут заблокированы </summary>
            OnlySelector
        }

        /// <summary>Блокирует узлы в соответствии с BlockManager</summary>
        public class TraversalProvider : ITraversalProvider
        {
            /// <summary>Содержит информацию о том, какие узлы заняты</summary>
            readonly BlockManager blockManager;

            /// <summary>Влияет на то, какие узлы считаются заблокированными</summary>
            public BlockMode mode { get; private set; }

            /// <summary>
            /// Блокираторы для этого пути.
            /// Эффект зависит от <see cref="mode"/>.
            ///
            /// Обратите внимание, что наличие большого селектора приводит к снижению производительности.
            ///
            /// See: mode
            /// </summary>
            readonly List<SingleNodeBlocker> selector;

            public TraversalProvider(BlockManager blockManager, BlockMode mode, List<SingleNodeBlocker> selector)
            {
                if (blockManager == null) throw new System.ArgumentNullException("blockManager");
                if (selector == null) throw new System.ArgumentNullException("selector");

                this.blockManager = blockManager;
                this.mode = mode;
                this.selector = selector;
            }

            #region ITraversalProvider implementation

            public bool CanTraverse(Path path, GraphNode node) // Может пересекать
            {
                // Это первое IF - реализация по умолчанию, которая используется, когда не используется поставщик обхода
                if (!node.Walkable || (path.enabledTags >> (int)node.Tag & 0x1) == 0)
                {
                    return false;
                }
                else if (mode == BlockMode.OnlySelector)
                {
                    return !blockManager.NodeContainsAnyOf(node, selector);
                }
                else
                {
                    // assume mode == BlockMode.AllExceptSelector
                    return !blockManager.NodeContainsAnyExcept(node, selector);
                }
            }

            public uint GetTraversalCost(Path path, GraphNode node) //Получите стоимость прохождения
            {
                // То же, что и реализация по умолчанию
                return path.GetTagPenalty((int)node.Tag) + node.Penalty;
            }

            #endregion
        }

        void Start()
        {
            if (!AstarPath.active)
                throw new System.Exception("No AstarPath object in the scene");
        }

        /// <summary>Истинно, если узел содержит какой-либо блокиратор, который включен в список выбранных</summary>
        public bool NodeContainsAnyOf(GraphNode node, List<SingleNodeBlocker> selector) //Узел содержит Любой Из
        {
            List<SingleNodeBlocker> blockersInNode;

            if (!blocked.TryGetValue(node, out blockersInNode))
            {
                return false;
            }

            for (int i = 0; i < blockersInNode.Count; i++)
            {
                var inNode = blockersInNode[i];
                for (int j = 0; j < selector.Count; j++)
                {
                    // Необходимо использовать ReferenceEquals, потому что этот код может быть вызван из отдельного потока
                    // и сравнение на равенство, которое предоставляет Unity, не является потокобезопасным
                    if (System.Object.ReferenceEquals(inNode, selector[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Истинно, если узел содержит какой-либо блокиратор, который не включен в список выбранных(исключений)</summary>
        public bool NodeContainsAnyExcept(GraphNode node, List<SingleNodeBlocker> selector) // Узел содержит все, кроме
        {
            List<SingleNodeBlocker> blockersInNode;

            if (!blocked.TryGetValue(node, out blockersInNode))
            {
                return false;
            }

            for (int i = 0; i < blockersInNode.Count; i++)
            {
                var inNode = blockersInNode[i];
                bool found = false;
                for (int j = 0; j < selector.Count; j++)
                {
                    // Необходимо использовать ReferenceEquals, потому что этот код может быть вызван из отдельного потока
                    // и сравнение на равенство, которое предоставляет Unity, не является потокобезопасным
                    if (System.Object.ReferenceEquals(inNode, selector[j]))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return true;
            }
            return false;
        }

        /// <summary>
        /// Зарегистрируйте блокировщик как присутствующий на указанном узле.
        /// Многократный вызов этого метода приведет к добавлению нескольких экземпляров блокировщика к узлу.
        ///
        /// Примечание: Узел не будет заблокирован немедленно. Вместо этого поиск пути
        /// потоки будут приостановлены, а затем будет применено обновление. Это, однако,
        /// гарантированно будет применен до запуска следующего запроса пути.
        /// </summary>
        public void InternalBlock(GraphNode node, SingleNodeBlocker blocker) // Внутренняя блокировка
        {
            AstarPath.active.AddWorkItem(new AstarWorkItem(() =>
            {
                List<SingleNodeBlocker> blockersInNode;
                if (!blocked.TryGetValue(node, out blockersInNode))
                {
                    blockersInNode = blocked[node] = ListPool<SingleNodeBlocker>.Claim();
                }

                blockersInNode.Add(blocker);
            }));
        }

        /// <summary>
        /// Удалите блокировщик с указанного узла.
        /// Удалит только один экземпляр, вызывая этот метод несколько раз
        /// times удалит несколько экземпляров блокировщика с узла.
        ///
        /// Примечание: Узел не будет разблокирован немедленно. Вместо этого поиск пути
        /// потоки будут приостановлены, а затем будет применено обновление. Это, однако,
        /// гарантированно будет применен до запуска следующего запроса пути.
        /// </summary>
        public void InternalUnblock(GraphNode node, SingleNodeBlocker blocker) // Внутренняя разблокировка
        {
            AstarPath.active.AddWorkItem(new AstarWorkItem(() =>
            {
                List<SingleNodeBlocker> blockersInNode;
                if (blocked.TryGetValue(node, out blockersInNode))
                {
                    blockersInNode.Remove(blocker);

                    if (blockersInNode.Count == 0)
                    {
                        blocked.Remove(node);
                        ListPool<SingleNodeBlocker>.Release(ref blockersInNode);
                    }
                }
            }));
        }
    }
}
