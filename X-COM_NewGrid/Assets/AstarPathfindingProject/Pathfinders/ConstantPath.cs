//#define ASTARDEBUG //Draws a ray for each node visitedmyCallbackFunction

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Pathfinding {
    /// <summary>
    /// Находит все узлы в пределах заданного расстояния от начала.
    /// Этот класс будет выполнять поиск снаружи от начальной точки и найдет все узлы, для достижения которых требуется меньше, чем ConstantPath.maxGScore, обычно это то же самое, что расстояние до них, умноженное на 1000
    ///
    /// Путь может быть вызван следующим образом:
    /// <code>
    /// // Здесь вы создаете новый путь и устанавливаете, как далеко он должен вести поиск. Значение Null предназначено для обратного вызова, но искатель справится с этим
    /// ConstantPath cpath = ConstantPath.Construct(transform.position, 2000, null);
    /// // Установите искатель для поиска пути (где my Seeker - это переменная, ссылающаяся на компонент Seeker)
    /// mySeeker.StartPath(cpath, myCallbackFunction);
    /// </code>
    ///
    /// Затем, при получении обратного вызова, все узлы будут сохранены в переменной Constant Path.all Nodes (помните, что вам нужно сначала преобразовать ее из Path в ConstantPath, чтобы получить переменную).
    ///
    /// Этот список будет отсортирован по стоимости доступа к этому узлу (более конкретно, по баллу G, если вы знакомы с терминологией алгоритмов поиска).
    /// [Откройте онлайн-документацию, чтобы просмотреть изображения]
    /// </summary>
    public class ConstantPath : Path {
		public GraphNode startNode;
		public Vector3 startPoint;
		public Vector3 originalStartPoint;

        /// <summary>
        ///Содержит все узлы, к которым найден путь.
        /// Этот список будет отсортирован по баллу G (стоимость/расстояние до узла).
        /// </summary>
        public List<GraphNode> allNodes;

        /// <summary>
        /// Определяет, когда путь должен завершиться.
        /// Это настраивается автоматически в конструкторе для экземпляра Pathfinding.Класс расстояния конечного условия с maxGScore указан в конструкторе.
        /// Если вы хотите использовать другое конечное условие.
        /// Смотрите: Поиск пути.Условие окончания пути для примеров (Pathfinding.Path Ending Condition for examples)
        /// </summary>
        public PathEndingCondition endingCondition;

		public override bool FloodingPath {
			get {
				return true;
			}
		}

        /// <summary>
        /// Создает ConstantPath, начинающийся с указанной точки.
        ///
        /// Поиск будет остановлен, когда у узла будет показатель G (стоимость его достижения), превышающий или равный maxGScore
        /// другими словами, он будет выполнять поиск по всем узлам, стоимость доступа к которым меньше, чем maxGScore.
        /// </summary>
        /// <param name="start">С того места, откуда будет начинаться путь (будет использоваться ближайший к этой точке узел)</param>
        /// <param name="maxGScore">Поиск будет остановлен, когда у узла будет оценка G, превышающая эту</param>
        /// <param name="callback">Будет вызван, когда путь будет завершен, оставьте это значение равным null, если вы используете искатель для обработки вызово</param>
        public static ConstantPath Construct (Vector3 start, int maxGScore, OnPathDelegate callback = null) {
			var p = PathPool.GetPath<ConstantPath>();

			p.Setup(start, maxGScore, callback);
			return p;
		}

        /// <summary>Устанавливает ConstantPath, начинающийся с указанной точки</summary>
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
        /// Инициализирует путь.
        /// Настраивает открытый список и добавляет в него первый узел
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

            //Продолжайте поиск до тех пор, пока мы не столкнемся с ошибкой и не найдем нужный объект
            while (CompleteState == PathCompleteState.NotCalculated) {
				searchedNodes++;

//--- Вот важный материал
                //Закройте текущий узел, если текущий узел удовлетворяет конечному условию, путь завершен
                if (endingCondition.TargetFound(currentR)) {
					CompleteState = PathCompleteState.Complete;
					break;
				}

				if (!currentR.flag1) {
                    //Добавить узел ко всем узлам
                    allNodes.Add(currentR.node);
					currentR.flag1 = true;
				}

#if ASTARDEBUG
				Debug.DrawRay((Vector3)currentR.node.position, Vector3.up*5, Color.cyan);
#endif

//--- На этом важные вещи заканчиваются

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
    /// Цель обнаруживается, когда путь длиннее заданного значения.
    /// На самом деле это определяется как когда G-оценка текущего узла равна >= заданной величине (конечное условие Distance.maxGScore).
    /// Балл G - это стоимость от начального узла до текущего узла, поэтому область с более высоким штрафом (весом) добавит больше к баллу G.
    /// Однако оценка G обычно представляет собой просто кратчайшее расстояние от начала до текущего узла.
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
