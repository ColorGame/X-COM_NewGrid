//#define ASTARDEBUG //Draws a ray for each node visitedmyCallbackFunction

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Pathfinding {
    /// <summary>
    /// ������� ��� ���� � �������� ��������� ���������� �� ������.
    /// ���� ����� ����� ��������� ����� ������� �� ��������� ����� � ������ ��� ����, ��� ���������� ������� ��������� ������, ��� ConstantPath.maxGScore, ������ ��� �� �� �����, ��� ���������� �� ���, ���������� �� 1000
    ///
    /// ���� ����� ���� ������ ��������� �������:
    /// <code>
    /// // ����� �� �������� ����� ���� � ��������������, ��� ������ �� ������ ����� �����. �������� Null ������������� ��� ��������� ������, �� �������� ��������� � ����
    /// ConstantPath cpath = ConstantPath.Construct(transform.position, 2000, null);
    /// // ���������� �������� ��� ������ ���� (��� my Seeker - ��� ����������, ����������� �� ��������� Seeker)
    /// mySeeker.StartPath(cpath, myCallbackFunction);
    /// </code>
    ///
    /// �����, ��� ��������� ��������� ������, ��� ���� ����� ��������� � ���������� Constant Path.all Nodes (�������, ��� ��� ����� ������� ������������� �� �� Path � ConstantPath, ����� �������� ����������).
    ///
    /// ���� ������ ����� ������������ �� ��������� ������� � ����� ���� (����� ���������, �� ����� G, ���� �� ������� � ������������� ���������� ������).
    /// [�������� ������-������������, ����� ����������� �����������]
    /// </summary>
    public class ConstantPath : Path {
		public GraphNode startNode;
		public Vector3 startPoint;
		public Vector3 originalStartPoint;

        /// <summary>
        ///�������� ��� ����, � ������� ������ ����.
        /// ���� ������ ����� ������������ �� ����� G (���������/���������� �� ����).
        /// </summary>
        public List<GraphNode> allNodes;

        /// <summary>
        /// ����������, ����� ���� ������ �����������.
        /// ��� ������������� ������������� � ������������ ��� ���������� Pathfinding.����� ���������� ��������� ������� � maxGScore ������ � ������������.
        /// ���� �� ������ ������������ ������ �������� �������.
        /// ��������: ����� ����.������� ��������� ���� ��� �������� (Pathfinding.Path Ending Condition for examples)
        /// </summary>
        public PathEndingCondition endingCondition;

		public override bool FloodingPath {
			get {
				return true;
			}
		}

        /// <summary>
        /// ������� ConstantPath, ������������ � ��������� �����.
        ///
        /// ����� ����� ����������, ����� � ���� ����� ���������� G (��������� ��� ����������), ����������� ��� ������ maxGScore
        /// ������� �������, �� ����� ��������� ����� �� ���� �����, ��������� ������� � ������� ������, ��� maxGScore.
        /// </summary>
        /// <param name="start">� ���� �����, ������ ����� ���������� ���� (����� �������������� ��������� � ���� ����� ����)</param>
        /// <param name="maxGScore">����� ����� ����������, ����� � ���� ����� ������ G, ����������� ���</param>
        /// <param name="callback">����� ������, ����� ���� ����� ��������, �������� ��� �������� ������ null, ���� �� ����������� �������� ��� ��������� ������</param>
        public static ConstantPath Construct (Vector3 start, int maxGScore, OnPathDelegate callback = null) {
			var p = PathPool.GetPath<ConstantPath>();

			p.Setup(start, maxGScore, callback);
			return p;
		}

        /// <summary>������������� ConstantPath, ������������ � ��������� �����</summary>
        protected void Setup (Vector3 start, int maxGScore, OnPathDelegate callback) {
			this.callback = callback;
			startPoint = start;
			originalStartPoint = startPoint;

			endingCondition = new EndingConditionDistance(this, maxGScore);
		}

		protected override void OnEnterPool () {
			base.OnEnterPool();
			if (allNodes != null) Util.ListPool<GraphNode>.Release(ref allNodes);
		}

		/// <summary>
		/// Reset the path to default values.
		/// Clears the <see cref="allNodes"/> list.
		/// Note: This does not reset the <see cref="endingCondition"/>.
		///
		/// Also sets <see cref="heuristic"/> to Heuristic.None as it is the default value for this path type
		/// </summary>
		protected override void Reset () {
			base.Reset();
			allNodes = Util.ListPool<GraphNode>.Claim();
			endingCondition = null;
			originalStartPoint = Vector3.zero;
			startPoint = Vector3.zero;
			startNode = null;
			heuristic = Heuristic.None;
		}

		protected override void Prepare () {
			nnConstraint.tags = enabledTags;
			var startNNInfo  = AstarPath.active.GetNearest(startPoint, nnConstraint);

			startNode = startNNInfo.node;
			if (startNode == null) {
				FailWithError("Could not find close node to the start point");
				return;
			}
		}

        /// <summary>
        /// �������������� ����.
        /// ����������� �������� ������ � ��������� � ���� ������ ����
        /// </summary>
        protected override void Initialize () {
			PathNode startRNode = pathHandler.GetPathNode(startNode);

			startRNode.node = startNode;
			startRNode.pathID = pathHandler.PathID;
			startRNode.parent = null;
			startRNode.cost = 0;
			startRNode.G = GetTraversalCost(startNode);
			startRNode.H = CalculateHScore(startNode);

			startNode.Open(this, startRNode, pathHandler);

			searchedNodes++;

			startRNode.flag1 = true;
			allNodes.Add(startNode);

			//any nodes left to search?
			if (pathHandler.heap.isEmpty) {
				CompleteState = PathCompleteState.Complete;
				return;
			}

			currentR = pathHandler.heap.Remove();
		}

		protected override void Cleanup () {
			int c = allNodes.Count;

			for (int i = 0; i < c; i++) pathHandler.GetPathNode(allNodes[i]).flag1 = false;
		}

		protected override void CalculateStep (long targetTick) {
			int counter = 0;

            //����������� ����� �� ��� ���, ���� �� �� ���������� � ������� � �� ������ ������ ������
            while (CompleteState == PathCompleteState.NotCalculated) {
				searchedNodes++;

//--- ��� ������ ��������
                //�������� ������� ����, ���� ������� ���� ������������� ��������� �������, ���� ��������
                if (endingCondition.TargetFound(currentR)) {
					CompleteState = PathCompleteState.Complete;
					break;
				}

				if (!currentR.flag1) {
                    //�������� ���� �� ���� �����
                    allNodes.Add(currentR.node);
					currentR.flag1 = true;
				}

#if ASTARDEBUG
				Debug.DrawRay((Vector3)currentR.node.position, Vector3.up*5, Color.cyan);
#endif

//--- �� ���� ������ ���� �������������

                AstarProfiler.StartFastProfile(4);
				//Debug.DrawRay ((Vector3)currentR.node.Position, Vector3.up*2,Color.red);

				//Loop through all walkable neighbours of the node and add them to the open list.
				currentR.node.Open(this, currentR, pathHandler);

				AstarProfiler.EndFastProfile(4);

				//any nodes left to search?
				if (pathHandler.heap.isEmpty) {
					CompleteState = PathCompleteState.Complete;
					break;
				}


				//Select the node with the lowest F score and remove it from the open list
				AstarProfiler.StartFastProfile(7);
				currentR = pathHandler.heap.Remove();
				AstarProfiler.EndFastProfile(7);

				//Check for time every 500 nodes, roughly every 0.5 ms usually
				if (counter > 500) {
					//Have we exceded the maxFrameTime, if so we should wait one frame before continuing the search since we don't want the game to lag
					if (DateTime.UtcNow.Ticks >= targetTick) {
						//Return instead of yield'ing, a separate function handles the yield (CalculatePaths)
						return;
					}
					counter = 0;

					if (searchedNodes > 1000000) {
						throw new Exception("Probable infinite loop. Over 1,000,000 nodes searched");
					}
				}

				counter++;
			}
		}
	}

    /// <summary>
    /// ���� ��������������, ����� ���� ������� ��������� ��������.
    /// �� ����� ���� ��� ������������ ��� ����� G-������ �������� ���� ����� >= �������� �������� (�������� ������� Distance.maxGScore).
    /// ���� G - ��� ��������� �� ���������� ���� �� �������� ����, ������� ������� � ����� ������� ������� (�����) ������� ������ � ����� G.
    /// ������ ������ G ������ ������������ ����� ������ ���������� ���������� �� ������ �� �������� ����.
    ///
    /// See: Pathfinding.ConstantPath which uses this ending condition
    /// </summary>
    public class EndingConditionDistance : PathEndingCondition {
		/// <summary>Max G score a node may have</summary>
		public int maxGScore = 100;

		//public EndingConditionDistance () {}
		public EndingConditionDistance (Path p, int maxGScore) : base(p) {
			this.maxGScore = maxGScore;
		}

		public override bool TargetFound (PathNode node) {
			return node.G >= maxGScore;
		}
	}
}
