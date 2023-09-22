using UnityEngine;
using System.Collections;
using Pathfinding;

namespace Pathfinding.Examples
{
    /// <summary>
    /// ������ ���������������� ��������� ��� �������� ������� � ���������� ����.
    /// ������������ � ������� "���������" ����� ��� �������� ����������� � ������������� �����.
    /// </summary>
    [ExecuteInEditMode]
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_snap_to_node.php")]
    public class SnapToNode : MonoBehaviour
    {
        void Update()
        {
            if (transform.hasChanged && AstarPath.active != null)
            {
                var node = AstarPath.active.GetNearest(transform.position, NNConstraint.None).node;
                if (node != null)
                {
                    transform.position = (Vector3)node.position;
                    transform.hasChanged = false;
                }
            }
        }
    }
}
