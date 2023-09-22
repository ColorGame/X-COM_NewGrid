using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding
{
    using Pathfinding.Util;
    using static Pathfinding.BlockManager;

    /// <summary>
    /// �������� ��� ��������� ����������, ����� ��� SingleNodeBlocker.
    ///
    /// ��� ����� ��������� ������. ��� ����� ������������ ��� ����� ���, �� � ������ ������� ��� ������������� ��� ��������� ���.
    ///
    /// See: TurnBasedAI
    /// See: turnbased (������� ������ �������� � ������-������������)
    /// </summary>
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_block_manager.php")]
    public class BlockManager : VersionedMonoBehaviour
    {
        public static BlockManager Instance { get; private set; }//(�������������� ������� SINGLETON)

        protected override void Awake()
        {
            base.Awake();
            Instance = this; //�������� ��������� ������
        }       

        /// <summary>�������� ���������� � ���, ����� �������-������������ ��������� ����� ������������� ���������� ����</summary>
        Dictionary<GraphNode, List<SingleNodeBlocker>> blocked = new Dictionary<GraphNode, List<SingleNodeBlocker>>();

        public enum BlockMode
        {
            /// <summary>��� ������������, ����� ���, ��� ������� � ������ TraversalProvider.selector, ����� �������������</summary>
            AllExceptSelector,
            /// <summary>������ �������� � ������ TraversalProvider.selector ����� ������������� </summary>
            OnlySelector
        }

        /// <summary>��������� ���� � ������������ � BlockManager</summary>
        public class TraversalProvider : ITraversalProvider
        {
            /// <summary>�������� ���������� � ���, ����� ���� ������</summary>
            readonly BlockManager blockManager;

            /// <summary>������ �� ��, ����� ���� ��������� ����������������</summary>
            public BlockMode mode { get; private set; }

            /// <summary>
            /// ����������� ��� ����� ����.
            /// ������ ������� �� <see cref="mode"/>.
            ///
            /// �������� ��������, ��� ������� �������� ��������� �������� � �������� ������������������.
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

            public bool CanTraverse(Path path, GraphNode node) // ����� ����������
            {
                // ��� ������ IF - ���������� �� ���������, ������� ������������, ����� �� ������������ ��������� ������
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

            public uint GetTraversalCost(Path path, GraphNode node) //�������� ��������� �����������
            {
                // �� ��, ��� � ���������� �� ���������
                return path.GetTagPenalty((int)node.Tag) + node.Penalty;
            }

            #endregion
        }

        void Start()
        {
            if (!AstarPath.active)
                throw new System.Exception("No AstarPath object in the scene");
        }

        /// <summary>�������, ���� ���� �������� �����-���� ����������, ������� ������� � ������ ���������</summary>
        public bool NodeContainsAnyOf(GraphNode node, List<SingleNodeBlocker> selector) //���� �������� ����� ��
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
                    // ���������� ������������ ReferenceEquals, ������ ��� ���� ��� ����� ���� ������ �� ���������� ������
                    // � ��������� �� ���������, ������� ������������� Unity, �� �������� ����������������
                    if (System.Object.ReferenceEquals(inNode, selector[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>�������, ���� ���� �������� �����-���� ����������, ������� �� ������� � ������ ���������(����������)</summary>
        public bool NodeContainsAnyExcept(GraphNode node, List<SingleNodeBlocker> selector) // ���� �������� ���, �����
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
                    // ���������� ������������ ReferenceEquals, ������ ��� ���� ��� ����� ���� ������ �� ���������� ������
                    // � ��������� �� ���������, ������� ������������� Unity, �� �������� ����������������
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
        /// ��������������� ����������� ��� �������������� �� ��������� ����.
        /// ������������ ����� ����� ������ �������� � ���������� ���������� ����������� ������������ � ����.
        ///
        /// ����������: ���� �� ����� ������������ ����������. ������ ����� ����� ����
        /// ������ ����� ��������������, � ����� ����� ��������� ����������. ���, ������,
        /// �������������� ����� �������� �� ������� ���������� ������� ����.
        /// </summary>
        public void InternalBlock(GraphNode node, SingleNodeBlocker blocker) // ���������� ����������
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
        /// ������� ����������� � ���������� ����.
        /// ������ ������ ���� ���������, ������� ���� ����� ��������� ���
        /// times ������ ��������� ����������� ������������ � ����.
        ///
        /// ����������: ���� �� ����� ������������� ����������. ������ ����� ����� ����
        /// ������ ����� ��������������, � ����� ����� ��������� ����������. ���, ������,
        /// �������������� ����� �������� �� ������� ���������� ������� ����.
        /// </summary>
        public void InternalUnblock(GraphNode node, SingleNodeBlocker blocker) // ���������� �������������
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
