using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding.Examples
{
    /// <summary>¬спомогательный скрипт в примере сцены'Turn Based'</summary>
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
            // ”бедитесь, что дверь изначально заблокирована
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

            // ѕодождите, пока есть другой (SingleNodeBlocker), занимающий тот же узел, что и дверь,
            // скорее всего, это другой блок, который стоит на узле двери, и тогда мы не сможем закрыть дверь
            if (BlockManager.Instance.NodeContainsAnyExcept(node, selector))
            {
                // ƒверь заблокирована
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
            // ѕрекратите ожидание и «акройте, если он запущен
            StopAllCoroutines();

            // ¬оспроизведение анимации открытой двери
            animator.CrossFade("open", 0.2f);
            open = true;

            // –азблокируйте дверной узел, чтобы подразделени€ могли снова пройти по нему
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
