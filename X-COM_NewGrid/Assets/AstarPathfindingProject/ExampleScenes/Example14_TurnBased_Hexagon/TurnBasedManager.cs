using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine.EventSystems;

namespace Pathfinding.Examples
{
    /// <summary>��������������� ������ � ������� ����� 'Turn Based'</summary>
    [HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_examples_1_1_turn_based_manager.php")]
    public class TurnBasedManager : MonoBehaviour
    {
        TurnBasedAI selected;

        public float movementSpeed;
        public GameObject nodePrefab;
        public LayerMask layerMask;

        List<GameObject> possibleMoves = new List<GameObject>();
        EventSystem eventSystem;

        public State state = State.SelectUnit;

        public enum State
        {
            SelectUnit,
            SelectTarget,
            Move
        }

        void Awake()
        {
            eventSystem = FindObjectOfType<EventSystem>();
        }

        void Update()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // ������������ ����� ����, ���� ���� ��������� ��� ��������� ����������������� ����������
            if (eventSystem.IsPointerOverGameObject())
            {
                return;
            }

            if (state == State.SelectTarget)
            {
                HandleButtonUnderRay(ray);
            }

            if (state == State.SelectUnit || state == State.SelectTarget)
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    var unitUnderMouse = GetByRay<TurnBasedAI>(ray);

                    if (unitUnderMouse != null)
                    {
                        Select(unitUnderMouse);
                        DestroyPossibleMoves();
                        GeneratePossibleMoves(selected);
                        state = State.SelectTarget;
                    }
                }
            }
        }

        // ������: ������� � ��������� �����
        void HandleButtonUnderRay(Ray ray)
        {
            var button = GetByRay<Astar3DButton>(ray);

            if (button != null && Input.GetKeyDown(KeyCode.Mouse0))
            {
                button.OnClick();

                DestroyPossibleMoves();
                state = State.Move;
                StartCoroutine(MoveToNode(selected, button.node));
            }
        }

        T GetByRay<T>(Ray ray) where T : class
        {
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, float.PositiveInfinity, layerMask))
            {
                return hit.transform.GetComponentInParent<T>();
            }
            return null;
        }

        void Select(TurnBasedAI unit)
        {
            selected = unit;
        }

        IEnumerator MoveToNode(TurnBasedAI unit, GraphNode node) // ��� �����������
        {
            var path = ABPath.Construct(unit.transform.position, (Vector3)node.position);

            path.traversalProvider = unit.traversalProvider;

            // ������������ ���� ��� �������
            AstarPath.StartPath(path);

            // ��������� ���������� ���������� ����
            yield return StartCoroutine(path.WaitForPath());

            if (path.error)
            {
                // �� ��������, ��� ����� ������, �� ��� ��� �������� ��������� ����
                // � ��������� ������ ������� ������ ������� ����
                // ��������, ���� ��� ������������ ����� ���������� �������������, ������������
                // ������������, � ����� ��������, �� ����� ���� �������
                Debug.LogError("Path failed:\n" + path.errorLog);
                state = State.SelectTarget;
                GeneratePossibleMoves(selected);
                yield break;
            }

            // ���������� ������� ����, ����� ������ ������� �����, �����
            // node - ��� �������� ����� � ����
            unit.targetNode = path.path[path.path.Count - 1];

            yield return StartCoroutine(MoveAlongPath(unit, path, movementSpeed));

            unit.blocker.BlockAtCurrentPosition();

            // �������� ����� ������ ��� �����������
            state = State.SelectUnit;
        }

        /// <summary>������������� ������ �� ����������</summary>
        static IEnumerator MoveAlongPath(TurnBasedAI unit, ABPath path, float speed) // ���������� �� ��������
        {
            if (path.error || path.vectorPath.Count == 0)
                throw new System.ArgumentException("Cannot follow an empty path");

            // ����� ������� ��������, ������ �������������� � ������� ������� catmull rom
            float distanceAlongSegment = 0;
            for (int i = 0; i < path.vectorPath.Count - 1; i++)
            {
                var p0 = path.vectorPath[Mathf.Max(i - 1, 0)];
                // Start of current segment
                var p1 = path.vectorPath[i];
                // End of current segment
                var p2 = path.vectorPath[i + 1];
                var p3 = path.vectorPath[Mathf.Min(i + 2, path.vectorPath.Count - 1)];

                var segmentLength = Vector3.Distance(p1, p2);

                while (distanceAlongSegment < segmentLength)
                {
                    var interpolatedPoint = AstarSplines.CatmullRom(p0, p1, p2, p3, distanceAlongSegment / segmentLength);
                    unit.transform.position = interpolatedPoint;
                    yield return null;
                    distanceAlongSegment += Time.deltaTime * speed;
                }

                distanceAlongSegment -= segmentLength;
            }

            unit.transform.position = path.vectorPath[path.vectorPath.Count - 1];
        }

        void DestroyPossibleMoves()
        {
            foreach (var go in possibleMoves)
            {
                GameObject.Destroy(go);
            }
            possibleMoves.Clear();
        }

        void GeneratePossibleMoves(TurnBasedAI unit) // ����������� ��������� ����
        {
            var path = ConstantPath.Construct(unit.transform.position, unit.movementPoints * 1000 + 1);

            path.traversalProvider = unit.traversalProvider;

            // ������������ ���� ��� �������
            AstarPath.StartPath(path);

            // ������������� ��������� ������ ���� ����������
            // ��� ������������, ��� ������ ���������� ���, �����
            // ��� �� ������� ������� ��������
            path.BlockUntilCalculated();

            foreach (var node in path.allNodes)
            {
                if (node != path.startNode)
                {
                    // �������� ����� ������ ����, ����� ������� ����, �� �������� ����� ���������
                    // ����������: ���� �� ����������� ������������ ��� � �������� ����, ��������, ��� ���������
                    // ������������ ��� ��������, ����� �������� ����������� �������� ����� ������� ��������
                    var go = GameObject.Instantiate(nodePrefab, (Vector3)node.position, Quaternion.identity) as GameObject;
                    possibleMoves.Add(go);

                    go.GetComponent<Astar3DButton>().node = node;
                }
            }
        }
    }
}
