using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// Блокирует отдельные узлы в графе.
    ///
    /// Это полезно в пошаговых играх, где вы хотите
    /// единицы измерения, чтобы избежать всех других единиц измерения при поиске пути
    /// но не блокируется сам по себе.
    ///
    /// Примечание: Это нельзя использовать вместе с каким-либо сценарием перемещения
    /// поскольку узлы не блокируются обычным способом.
    /// Смотрите: Пошаговый ИИ для примера использования
    ///
    /// Смотрите: BlockManager
    /// Смотрите: пошаговая инструкция (рабочие ссылки смотрите в онлайн-документации)
    /// </summary>
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_single_node_blocker.php")]
    public class SingleNodeBlocker : VersionedMonoBehaviour
    {
        public GraphNode lastBlocked { get; private set; } // последний заблокированный

        /// <summary>
        /// Блокируйте узел, ближайший к местоположению этого объекта.
        ///
        /// Разблокирует последний узел, который был зарезервирован (если таковой имеется)
        /// </summary>
        public void BlockAtCurrentPosition()
        {
            BlockAt(transform.position);
        }

        /// <summary>
        /// Блокируйте узел, ближайший к указанной позиции.
        ///
        /// Разблокирует последний узел, который был зарезервирован (если таковой имеется)
        /// </summary>
        public void BlockAt(Vector3 position)
        {
            Unblock();
            var node = AstarPath.active.GetNearest(position, NNConstraint.None).node;
            if (node != null)
            {
                Block(node);
            }
        }

        /// <summary>
        /// Заблокировать указанный узел.
        ///
        /// Разблокирует последний узел, который был зарезервирован (если таковой имеется)
        /// </summary>
        public void Block(GraphNode node)
        {
            if (node == null)
                throw new System.ArgumentNullException("node");

            BlockManager.Instance.InternalBlock(node, this);
            lastBlocked = node;
        }

        /// <summary>Разблокируйте последний узел, который был заблокирован (если таковой был)</summary>
        public void Unblock()
        {
            if (lastBlocked == null || lastBlocked.Destroyed)
            {
                lastBlocked = null;
                return;
            }

            BlockManager.Instance.InternalUnblock(lastBlocked, this);
            lastBlocked = null;
        }
    }
}
