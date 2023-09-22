using UnityEngine;

namespace Pathfinding
{
    /// <summary>
    /// ��������� ��������� ���� � �����.
    ///
    /// ��� ������� � ��������� �����, ��� �� ������
    /// ������� ���������, ����� �������� ���� ������ ������ ��������� ��� ������ ����
    /// �� �� ����������� ��� �� ����.
    ///
    /// ����������: ��� ������ ������������ ������ � �����-���� ��������� �����������
    /// ��������� ���� �� ����������� ������� ��������.
    /// ��������: ��������� �� ��� ������� �������������
    ///
    /// ��������: BlockManager
    /// ��������: ��������� ���������� (������� ������ �������� � ������-������������)
    /// </summary>
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_single_node_blocker.php")]
    public class SingleNodeBlocker : VersionedMonoBehaviour
    {
        public GraphNode lastBlocked { get; private set; } // ��������� ���������������

        /// <summary>
        /// ���������� ����, ��������� � �������������� ����� �������.
        ///
        /// ������������ ��������� ����, ������� ��� �������������� (���� ������� �������)
        /// </summary>
        public void BlockAtCurrentPosition()
        {
            BlockAt(transform.position);
        }

        /// <summary>
        /// ���������� ����, ��������� � ��������� �������.
        ///
        /// ������������ ��������� ����, ������� ��� �������������� (���� ������� �������)
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
        /// ������������� ��������� ����.
        ///
        /// ������������ ��������� ����, ������� ��� �������������� (���� ������� �������)
        /// </summary>
        public void Block(GraphNode node)
        {
            if (node == null)
                throw new System.ArgumentNullException("node");

            BlockManager.Instance.InternalBlock(node, this);
            lastBlocked = node;
        }

        /// <summary>������������� ��������� ����, ������� ��� ������������ (���� ������� ���)</summary>
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
