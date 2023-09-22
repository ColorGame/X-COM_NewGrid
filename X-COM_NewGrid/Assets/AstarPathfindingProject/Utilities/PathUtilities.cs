using Pathfinding.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding {
    /// <summary>
    /// Содержит полезные функции для работы с путями и узлами.
    /// This class works a lot with the <see cref="Pathfinding.GraphNode"/> class, a useful function to get nodes is AstarPath.GetNearest.
    /// See: <see cref="AstarPath.GetNearest"/>
    /// See: <see cref="Pathfinding.GraphUpdateUtilities"/>
    /// See: <see cref="Pathfinding.GraphUtilities"/>
    /// </summary>
    public static class PathUtilities {
        /// <summary>
        /// Возвращает, если существует проходимый путь от узла 1 к узлу 2.
        /// Этот метод чрезвычайно быстр, поскольку он использует только предварительно вычисленную информацию.
        ///
        /// <code>
        /// GraphNode node1 = AstarPath.active.GetNearest(point1, NNConstraint.Default).node;
        /// GraphNode node2 = AstarPath.active.GetNearest(point2, NNConstraint.Default).node;
        ///
        /// if (PathUtilities.IsPathPossible(node1, node2)) {
        ///     // Да, между этими двумя узлами есть путь
        /// }
        /// </code>
        ///
        /// Эквивалентно вызову<see cref="IsPathPossible(List<GraphNode>)"/> со списком, содержащим node1 и node2.
        ///
        /// See: graph-updates (view in online documentation for working links)
        /// See: <see cref="AstarPath.GetNearest"/>
        /// </summary>
        public static bool IsPathPossible (GraphNode node1, GraphNode node2) {
			return node1.Walkable && node2.Walkable && node1.Area == node2.Area;
		}

        /// <summary>
        /// Возвращает, если между всеми узлами имеются проходимые пути.
        ///
        /// Смотрите: график-обновления (рабочие ссылки смотрите в онлайн-документации)
        ///
        /// Возвращает значение true для пустых списков.
        ///
        /// See: <see cref="AstarPath.GetNearest"/>
        /// </summary>
        public static bool IsPathPossible (List<GraphNode> nodes) {
			if (nodes.Count == 0) return true;

			uint area = nodes[0].Area;
			for (int i = 0; i < nodes.Count; i++) if (!nodes[i].Walkable || nodes[i].Area != area) return false;
			return true;
		}

        /// <summary>
        /// Возвращает, если между всеми узлами имеются проходимые пути.
        /// Смотрите: graph-updates  график-обновления (рабочие ссылки смотрите в онлайн-документации)
        ///
        /// Этот метод фактически будет проверять только то, может ли первый узел достичь всех остальных узлов. Однако это
        /// эквивалентно в 99% случаев, поскольку почти всегда графические соединения являются двунаправленными.
        /// Если вам не известно о каких-либо случаях, когда вы явно создаете однонаправленные соединения
        /// этот метод можно использовать без опасений.
        ///
        /// Возвращает значение true для пустых списков
        ///
        /// Предупреждение: Этот метод значительно медленнее, чем метод IsPathPossible, который не принимает tagMask
        ///
        /// See: <see cref="AstarPath.GetNearest"/>
        /// </summary>
        public static bool IsPathPossible (List<GraphNode> nodes, int tagMask) {
			if (nodes.Count == 0) return true;

			// Make sure that the first node has a valid tag
			if (((tagMask >> (int)nodes[0].Tag) & 1) == 0) return false;

			// Fast check first
			if (!IsPathPossible(nodes)) return false;

			// Make sure that the first node can reach all other nodes
			var reachable = GetReachableNodes(nodes[0], tagMask);
			bool result = true;

			// Make sure that the first node can reach all other nodes
			for (int i = 1; i < nodes.Count; i++) {
				if (!reachable.Contains(nodes[i])) {
					result = false;
					break;
				}
			}

			// Pool the temporary list
			ListPool<GraphNode>.Release(ref reachable);

			return result;
		}

        /// <summary>
        /// Возвращает все узлы, доступные из начального узла.
        /// Эта функция выполняет DFS (поиск в глубину) или заливку графика потоком и возвращает все узлы, до которых можно добраться из начального узла.
		/// Почти во всех случаях это будет идентично возврату всех узлов, которые имеют ту же область, что и начальный узел.
        /// В редакторе области отображаются в виде узлов разного цвета.
        /// Единственный случай, когда этого не будет, - это когда есть односторонний путь от некоторой части области к начальному узлу,
		/// но нет пути от начального узла к этой части графика.
        ///
        /// Возвращаемый список не сортируется каким-либо определенным образом.
        ///
        /// В зависимости от количества доступных узлов вычисление этой функции может занять довольно много времени, поэтому не используйте ее слишком часто, иначе это может повлиять на частоту кадров вашей игры.
        ///
        /// Смотрите: битовые маски (рабочие ссылки смотрите в онлайн-документации).
        ///
        /// Returns: A List<Node> containing all nodes reachable from the seed node.
        /// For better memory management the returned list should be pooled, see Pathfinding.Util.ListPool.
        /// </summary>
        /// <param name="seed">Узел, с которого нужно начать поиск.</param>
        /// <param name="tagMask">Необязательная маска для тегов. Это битовая маска.</param>
        /// <param name="filter">Необязательный фильтр, по которому выполняется поиск узлов. Вы можете объединить это с tagMask = -1, чтобы фильтр определял все.
        /// Независимо от фильтра выполняется поиск только по проходимым узлам. Если функция фильтра возвращает значение false, узел будет рассматриваться как недоступный.</param>
        public static List<GraphNode> GetReachableNodes (GraphNode seed, int tagMask = -1, System.Func<GraphNode, bool> filter = null) {
			Stack<GraphNode> dfsStack = StackPool<GraphNode>.Claim();
			List<GraphNode> reachable = ListPool<GraphNode>.Claim();

			/// <summary>TODO: Pool</summary>
			var map = new HashSet<GraphNode>();

			System.Action<GraphNode> callback;
			// Check if we can use the fast path
			if (tagMask == -1 && filter == null) {
				callback = (GraphNode node) => {
					if (node.Walkable && map.Add(node)) {
						reachable.Add(node);
						dfsStack.Push(node);
					}
				};
			} else {
				callback = (GraphNode node) => {
					if (node.Walkable && ((tagMask >> (int)node.Tag) & 0x1) != 0 && map.Add(node)) {
						if (filter != null && !filter(node)) return;

						reachable.Add(node);
						dfsStack.Push(node);
					}
				};
			}

			callback(seed);

			while (dfsStack.Count > 0) {
				dfsStack.Pop().GetConnections(callback);
			}

			StackPool<GraphNode>.Release(dfsStack);
			return reachable;
		}

		static Queue<GraphNode> BFSQueue;
		static Dictionary<GraphNode, int> BFSMap;

        /// <summary>
        ///Возвращает все узлы вплоть до заданного расстояния от начального узла.
        /// Эта функция выполняет BFS (<a href="https://en.wikipedia.org/wiki/Breadth-first_search">breadth-first search</a>) или заполняет график потоком и возвращает 
		/// все узлы в пределах заданного расстояния между узлами, которое может быть достигнуто от начального узла. 
		/// Почти во всех случаях, когда глубина достаточно велика, это будет идентично возврату всех узлов, которые имеют ту же область, что и начальный узел.
        /// В редакторе области отображаются в виде узлов разного цвета.
        /// Единственный случай, когда этого не будет, - это когда есть односторонний путь от некоторой части области к начальному узлу, но нет пути от начального узла к этой части графика.
        ///
        /// Возвращаемый список сортируется по расстоянию узла от начального узла, т.е. расстояние измеряется в количестве узлов, через которые прошел бы кратчайший путь от начального узла к этому узлу.
        /// Обратите внимание, что при измерении расстояния не учитываются эвристические методы, штрафы или пометки штрафных санкций.
        ///
        /// В зависимости от количества узлов вычисление этой функции может занять довольно много времени, поэтому не используйте ее слишком часто, иначе это может повлиять на частоту кадров вашей игры.
        ///
        /// Возвращает: Список<GraphNode>, содержащий все узлы, доступные на указанном расстоянии от начального узла.
        ///Для лучшего управления памятью возвращаемый список должен быть объединен в пул, смотрите Pathfinding.Util.ListPool
        ///
        /// Предупреждение: Этот метод не является потокобезопасным. Используйте его только из потока Unity (т.е. обычного игрового кода).
        ///
        /// На видео ниже показан результат BFS с различными значениями глубины. Точки отбираются на узлах с использованием<see cref="GetPointsOnNodes"/>.
        /// [Open online documentation to see videos]
        ///
        /// <code>
        /// var seed = AstarPath.active.GetNearest(transform.position, NNConstraint.Default).node;
        /// var nodes = PathUtilities.BFS(seed, 10);
        /// foreach (var node in nodes) {
        ///     Debug.DrawRay((Vector3)node.position, Vector3.up, Color.red, 10);
        /// }
        /// </code>
        /// </summary>
        /// <param name="seed">Узел, с которого нужно начать поиск.</param>
        /// <param name="depth">Максимальное расстояние в узлах от начального узла.</param>
        /// <param name="tagMask">Optional mask for tags. This is a bitmask.</param>
        /// <param name="filter">Optional filter for which nodes to search. You can combine this with depth = int.MaxValue and tagMask = -1 to make the filter determine everything.
        ///      Only walkable nodes are searched regardless of the filter. If the filter function returns false the node will be treated as unwalkable.</param>
        public static List<GraphNode> BFS (GraphNode seed, int depth, int tagMask = -1, System.Func<GraphNode, bool> filter = null) {
#if ASTAR_PROFILE
			System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			watch.Start();
#endif

			BFSQueue = BFSQueue ?? new Queue<GraphNode>();
			var que = BFSQueue;

			BFSMap = BFSMap ?? new Dictionary<GraphNode, int>();
			var map = BFSMap;

            // Несмотря на то, что мы очищаем в конце этой функции, полезно
            // сделайте это и здесь, в случае, если предыдущий вызов метода
            // по какой-то причине выдало исключение
            // и не очистил очередь и карту
            que.Clear();
			map.Clear();

			List<GraphNode> result = ListPool<GraphNode>.Claim();

			int currentDist = -1;
			System.Action<GraphNode> callback;
			if (tagMask == -1) {
				callback = node => {
					if (node.Walkable && !map.ContainsKey(node)) {
						if (filter != null && !filter(node)) return;

						map.Add(node, currentDist+1);
						result.Add(node);
						que.Enqueue(node);
					}
				};
			} else {
				callback = node => {
					if (node.Walkable && ((tagMask >> (int)node.Tag) & 0x1) != 0 && !map.ContainsKey(node)) {
						if (filter != null && !filter(node)) return;

						map.Add(node, currentDist+1);
						result.Add(node);
						que.Enqueue(node);
					}
				};
			}

			callback(seed);

			while (que.Count > 0) {
				GraphNode n = que.Dequeue();
				currentDist = map[n];

				if (currentDist >= depth) break;

				n.GetConnections(callback);
			}

			que.Clear();
			map.Clear();

#if ASTAR_PROFILE
			watch.Stop();
			Debug.Log((1000*watch.Elapsed.TotalSeconds).ToString("0.0 ms"));
#endif
			return result;
		}

        /// <summary>
        ///Возвращает точки по спирали с центром вокруг начала координат с минимальным расстоянием от других точек.
        /// Точки расположены на эвольвенте окружности
        /// See: http://en.wikipedia.org/wiki/Involute
        /// Which has some nice properties.
        /// All points are separated by clearance world units.
        /// This method is O(n), yes if you read the code you will see a binary search, but that binary search
        /// has an upper bound on the number of steps, so it does not yield a log factor.
        ///
        /// Note: Consider recycling the list after usage to reduce allocations.
        /// See: Pathfinding.Util.ListPool
        /// </summary>
        public static List<Vector3> GetSpiralPoints (int count, float clearance) {
			List<Vector3> pts = ListPool<Vector3>.Claim(count);

			// The radius of the smaller circle used for generating the involute of a circle
			// Calculated from the separation distance between the turns
			float a = clearance/(2*Mathf.PI);
			float t = 0;


			pts.Add(InvoluteOfCircle(a, t));

			for (int i = 0; i < count; i++) {
				Vector3 prev = pts[pts.Count-1];

				// d = -t0/2 + sqrt( t0^2/4 + 2d/a )
				// Minimum angle (radians) which would create an arc distance greater than clearance
				float d = -t/2 + Mathf.Sqrt(t*t/4 + 2*clearance/a);

				// Binary search for separating this point and the previous one
				float mn = t + d;
				float mx = t + 2*d;
				while (mx - mn > 0.01f) {
					float mid = (mn + mx)/2;
					Vector3 p = InvoluteOfCircle(a, mid);
					if ((p - prev).sqrMagnitude < clearance*clearance) {
						mn = mid;
					} else {
						mx = mid;
					}
				}

				pts.Add(InvoluteOfCircle(a, mx));
				t = mx;
			}

			return pts;
		}

        /// <summary>
        /// Возвращает координату XZ эвольвенты окружности.
        /// See: http://en.wikipedia.org/wiki/Involute
        /// </summary>
        private static Vector3 InvoluteOfCircle (float a, float t) {
			return new Vector3(a*(Mathf.Cos(t) + t*Mathf.Sin(t)), 0, a*(Mathf.Sin(t) - t*Mathf.Cos(t)));
		}

        /// <summary>
        /// Вычислит количество точек вокруг p, которые находятся на графике и отделены друг от друга зазором.
        /// Это похоже на получение точек вокруг точки, за исключением того, что предыдущие точки рассматриваются как находящиеся в мировом пространстве.
        /// Будет найдено среднее значение баллов, и затем оно будет рассматриваться как центр группы.
        /// </summary>
        /// <param name="p">Точка для создания точек вокруг</param>
        /// <param name="g">График, который будет использоваться для приведения линий. Если вы используете только один график, вы можете получить его с помощью AstarPath.active.graphs[0] как IRaycastableGraph.
        /// Обратите внимание, что не все графики являются raycastable, recast, navmesh и grid-графики являются raycastable. В recast и navmesh это работает лучше всего.</param>
        /// <param name="previousPoints">Точки для использования в качестве ориентира. Обратите внимание, что они находятся в мировом пространстве.
        /// Новые точки перезапишут существующие точки в списке. Результат будет в мировом пространстве.</param>
        /// <param name="radius">Конечные точки будут находиться самое большее на таком расстоянии от p.</param>
        /// <param name="clearanceRadius">Точки, по возможности, должны находиться по крайней мере на таком расстоянии друг от друга.</param>
        public static void GetPointsAroundPointWorld (Vector3 p, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius) {
			if (previousPoints.Count == 0) return;

			Vector3 avg = Vector3.zero;
			for (int i = 0; i < previousPoints.Count; i++) avg += previousPoints[i];
			avg /= previousPoints.Count;

			for (int i = 0; i < previousPoints.Count; i++) previousPoints[i] -= avg;

			GetPointsAroundPoint(p, g, previousPoints, radius, clearanceRadius);
		}

        /// <summary>
        ///Вычислит количество точек вокруг центра, которые находятся на графике и отделены друг от друга зазором.
        /// Максимальным расстоянием от центра до любой точки будет радиус.
        /// Сначала будет предпринята попытка разместить точки так же, как предыдущие точки, и если это не удастся, будут выбраны случайные точки.
        /// Это здорово, если вы хотите выбрать несколько целевых точек для группового перемещения. Если вы передадите все текущие баллы агента, например, со средней позиции группы
        /// этот метод возвращает целевые точки, так что объекты очень мало перемещаются внутри группы, это часто эстетично и уменьшает дрожание при использовании
        /// своего рода локальное избегание.
        ///
        /// TODO: Написать модульные тесты
        /// </summary>
        /// <param name="center">The point to generate points around</param>
        /// <param name="g">The graph to use for linecasting. If you are only using one graph, you can get this by AstarPath.active.graphs[0] as IRaycastableGraph.
        /// Note that not all graphs are raycastable, recast, navmesh and grid graphs are raycastable. On recast and navmesh it works the best.</param>
        /// <param name="previousPoints">The points to use for reference. Note that these should not be in world space. They are treated as relative to center.
        ///      The new points will overwrite the existing points in the list. The result will be in world space, not relative to center.</param>
        /// <param name="radius">The final points will be at most this distance from center.</param>
        /// <param name="clearanceRadius">The points will if possible be at least this distance from each other.</param>
        public static void GetPointsAroundPoint (Vector3 center, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius) {
			if (g == null) throw new System.ArgumentNullException("g");

			var graph = g as NavGraph;

			if (graph == null) throw new System.ArgumentException("g is not a NavGraph");

			NNInfoInternal nn = graph.GetNearestForce(center, NNConstraint.Default);
			center = nn.clampedPosition;

			if (nn.node == null) {
				// No valid point to start from
				return;
			}


			// Make sure the enclosing circle has a radius which can pack circles with packing density 0.5
			radius = Mathf.Max(radius, 1.4142f*clearanceRadius*Mathf.Sqrt(previousPoints.Count)); //Mathf.Sqrt(previousPoints.Count*clearanceRadius*2));
			clearanceRadius *= clearanceRadius;

			for (int i = 0; i < previousPoints.Count; i++) {
				Vector3 dir = previousPoints[i];
				float magn = dir.magnitude;

				if (magn > 0) dir /= magn;

				float newMagn = radius;//magn > radius ? radius : magn;
				dir *= newMagn;

				GraphHitInfo hit;

				int tests = 0;
				while (true) {
					Vector3 pt = center + dir;

					if (g.Linecast(center, pt, out hit)) {
						if (hit.point == Vector3.zero) {
							// Oops, linecast actually failed completely
							// try again unless we have tried lots of times
							// then we just continue anyway
							tests++;
							if (tests > 8) {
								previousPoints[i] = pt;
								break;
							}
						} else {
							pt = hit.point;
						}
					}

					bool worked = false;

					for (float q = 0.1f; q <= 1.0f; q += 0.05f) {
						Vector3 qt = Vector3.Lerp(center, pt, q);
						worked = true;
						for (int j = 0; j < i; j++) {
							if ((previousPoints[j] - qt).sqrMagnitude < clearanceRadius) {
								worked = false;
								break;
							}
						}

						// Abort after 8 tests or when we have found a valid point
						if (worked || tests > 8) {
							worked = true;
							previousPoints[i] = qt;
							break;
						}
					}

					// Break out of nested loop
					if (worked) {
						break;
					}

					// If we could not find a valid point, reduce the clearance radius slightly to improve
					// the chances next time
					clearanceRadius *= 0.9f;
					// This will pick points in 2D closer to the edge of the circle with a higher probability
					dir = Random.onUnitSphere * Mathf.Lerp(newMagn, radius, tests / 5);
					dir.y = 0;
					tests++;
				}
			}
		}

        /// <summary>
        /// Возвращает случайно выбранные точки на указанных узлах, причем каждая точка отделена друг от друга "clearanceRadius".
        ///Выбор точек НА узлах работает только для TriangleMeshNode (используется Recast Graph и Navmesh Graph) и Grid Node (используется GridGraph).
        /// Для других типов узлов будут использоваться только положения узлов.
        ///
        /// /// "радиус зазора" будет уменьшен, если не удастся найти допустимые точки.
        ///
        /// Примечание: Этот метод предполагает, что узлы в списке имеют одинаковый тип для некоторых особых случаев.
        /// Более конкретно, если первый узел не является TriangleMeshNode или GridNode, он будет использовать быстрый путь, который предполагает, 
		/// что все узлы в списке имеют одинаковую площадь поверхности (которая обычно равна нулю, а все узлы являются точечными узлами).
        /// </summary>
        public static List<Vector3> GetPointsOnNodes (List<GraphNode> nodes, int count, float clearanceRadius = 0) {
			if (nodes == null) throw new System.ArgumentNullException("nodes");
			if (nodes.Count == 0) throw new System.ArgumentException("no nodes passed");

			List<Vector3> pts = ListPool<Vector3>.Claim(count);

			// Square
			clearanceRadius *= clearanceRadius;

			if (clearanceRadius > 0 || nodes[0] is TriangleMeshNode
#if !ASTAR_NO_GRID_GRAPH
				|| nodes[0] is GridNode
#endif
				) {
				// Accumulated area of all nodes
				List<float> accs = ListPool<float>.Claim(nodes.Count);

				// Total area of all nodes so far
				float tot = 0;

				for (int i = 0; i < nodes.Count; i++) {
					var surfaceArea = nodes[i].SurfaceArea();
					// Ensures that even if the nodes have a surface area of 0, a random one will still be picked
					// instead of e.g always picking the first or the last one.
					surfaceArea += 0.001f;
					tot += surfaceArea;
					accs.Add(tot);
				}

				for (int i = 0; i < count; i++) {
					//Pick point
					int testCount = 0;
					int testLimit = 10;
					bool worked = false;

					while (!worked) {
						worked = true;

						// If no valid points could be found, progressively lower the clearance radius until such a point is found
						if (testCount >= testLimit) {
							// Note that clearanceRadius is a squared radius
							clearanceRadius *= 0.9f*0.9f;
							testLimit += 10;
							if (testLimit > 100) clearanceRadius = 0;
						}

						// Pick a random node among the ones in the list weighted by their area
						float tg = Random.value*tot;
						int v = accs.BinarySearch(tg);
						if (v < 0) v = ~v;

						if (v >= nodes.Count) {
							// Cover edge cases
							worked = false;
							continue;
						}

						var node = nodes[v];
						var p = node.RandomPointOnSurface();

						// Test if it is some distance away from the other points
						if (clearanceRadius > 0) {
							for (int j = 0; j < pts.Count; j++) {
								if ((pts[j]-p).sqrMagnitude < clearanceRadius) {
									worked = false;
									break;
								}
							}
						}

						if (worked) {
							pts.Add(p);
							break;
						}
						testCount++;
					}
				}

				ListPool<float>.Release(ref accs);
			} else {
				// Fast path, assumes all nodes have the same area (usually zero)
				for (int i = 0; i < count; i++) {
					pts.Add((Vector3)nodes[Random.Range(0, nodes.Count)].RandomPointOnSurface());
				}
			}

			return pts;
		}
	}
}
