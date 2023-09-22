using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding.Examples
{
    /// <summary>��������������� ������ � ������� �����'Turn Based'</summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SingleNodeBlocker))]
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_examples_1_1_turn_based_door.php")]
    public class TurnBasedDoor : MonoBehaviour
    {
        Animator animator;
        SingleNodeBlocker blocker;


        bool open;

        void Awake()
        {
            animator = GetComponent<Animator>();
            blocker = GetComponent<SingleNodeBlocker>();
        }

        void Start()
        {
            // ���������, ��� ����� ���������� �������������
            blocker.BlockAtCurrentPosition();
            animator.CrossFade("close", 0.2f);
        }

        public void Close()
        {
            StartCoroutine(WaitAndClose());
        }

        IEnumerator WaitAndClose()
        {
            var selector = new List<SingleNodeBlocker>() { blocker };
            var node = AstarPath.active.GetNearest(transform.position).node;

            // ���������, ���� ���� ������ (SingleNodeBlocker), ���������� ��� �� ����, ��� � �����,
            // ������ �����, ��� ������ ����, ������� ����� �� ���� �����, � ����� �� �� ������ ������� �����
            if (BlockManager.Instance.NodeContainsAnyExcept(node, selector))
            {
                // ����� �������������
                animator.CrossFade("blocked", 0.2f);
            }

            while (BlockManager.Instance.NodeContainsAnyExcept(node, selector))
            {
                yield return null;
            }

            open = false;
            animator.CrossFade("close", 0.2f);
            blocker.BlockAtCurrentPosition();
        }

        public void Open()
        {
            // ���������� �������� � ��������, ���� �� �������
            StopAllCoroutines();

            // ��������������� �������� �������� �����
            animator.CrossFade("open", 0.2f);
            open = true;

            // ������������� ������� ����, ����� ������������� ����� ����� ������ �� ����
            blocker.Unblock();
        }

        public void Toggle()
        {
            if (open)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
    }
}
