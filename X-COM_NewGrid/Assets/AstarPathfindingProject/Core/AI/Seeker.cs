using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Pathfinding {
    /// <summary>
    /// Обрабатывает вызовы пути для одного модуля.
    ///
    /// Это компонент, который предназначен для подключения к одному устройству (ИИ, роботу, игроку, чему угодно) для обработки его вызовов поиска пути.
    /// Он также обрабатывает постобработку путей с использованием модификаторов.
    ///
    /// [Откройте онлайн-документацию, чтобы увидеть изображения]
    ///
    /// Смотрите: вызов-поиск пути (рабочие ссылки смотрите в онлайн-документации)
    /// Смотрите: модификаторы (рабочие ссылки смотрите в онлайн-документации)
    /// </summary>
    [AddComponentMenu("Pathfinding/Seeker")]
	[HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_seeker.php")]
	public class Seeker : VersionedMonoBehaviour {
        /// <summary>
        /// Позволяет отрисовывать последний вычисленный путь с помощью Gizmos.
        /// Путь будет выделен зеленым цветом.
        ///
        /// Смотрите: OnDrawGizmos
        /// </summary>
        public bool drawGizmos = true;

        /// <summary>
        /// Позволяет отрисовывать необработанный контур с помощью Gizmos.
        /// Путь будет выделен оранжевым цветом.
        ///
        /// /// Требуется, чтобы <см. cref="drawGizmos"/> было true.
        ///
        /// Это покажет путь до того, как будет применена любая последующая обработка, такая как сглаживание.
        ///
        /// Смотрите: drawGizmos
        /// Смотрите: OnDrawGizmos
        /// </summary>
        public bool detailedGizmos;

        /// <summary>Модификатор пути, который изменяет начальную и конечную точки пути</summary>
        [HideInInspector]
		public StartEndModifier startEndModifier = new StartEndModifier();

        /// <summary>
        /// Метки, по которым может перемещаться искатель.
        ///
        /// Примечание: Это поле является битовой маской.
        /// Смотрите: битовые маски (рабочие ссылки смотрите в онлайн-документации)
        /// </summary>
        [HideInInspector]
		public int traversableTags = -1;

        /// <summary>
        /// Штрафы за каждый тег.
        /// К тегу 0, который является тегом по умолчанию, будет добавлен штраф в размере Tagpenalty[0].
        /// Это должны быть только положительные значения, поскольку алгоритм A * не может обрабатывать отрицательные штрафы.
        ///
        /// Примечание: Этот массив всегда должен иметь длину 32, в противном случае система проигнорирует его.
        ///
        /// Смотрите: Поиск пути.Path.Tagpenalities
        /// </summary>
        [HideInInspector]
		public int[] tagPenalties = new int[32];

        /// <summary>
        /// Графики, которые может использовать этот искатель.
        /// Это поле определяет, какие графики будут учитываться при поиске начального и конечного узлов пути.
        /// Это полезно во многих ситуациях, например, если вы хотите создать один график для небольших единиц измерения и один график для больших единиц измерения.
        ///
        /// Это битовая маска, поэтому, если вы, например, хотите заставить агента использовать только индекс графика 3, вы можете установить это значение в:
        /// <код> seeker.graphMask = 1 << 3; </код>
        ///
        /// Смотрите: битовые маски (рабочие ссылки смотрите в онлайн-документации)
        ///
        /// Обратите внимание, что в этом поле хранятся только те индексы графика, которые разрешены. Это означает, что если графики изменят свой порядок
        /// тогда эта маска может больше не быть правильной.
        ///
        /// Если вы знаете название графика, вы можете использовать <см. cref="Поиск пути.GraphMask.FromGraphName"/> метод:
        /// <код>
        /// GraphMask mask1 = GraphMask.FromGraphName("Мой сеточный график");
        /// GraphMask mask2 = GraphMask.FromGraphName("Мой другой сеточный график");
        ///
        /// NNConstraint nn = NNConstraint.По умолчанию;
        ///
        /// nn.graphMask = mask1 | mask2;
        ///
        /// // Найдите узел, ближайший к некоторой точке, которая находится либо в "Моем сеточном графике", либо в "Моем другом сеточном графике".
        /// var info = AstarPath.active.Получить ближайший(somePoint, nn);
        /// </код>
        ///
        /// Некоторые перегрузки методов <см. cref="StartPath"/> принимают параметр graphMask. Если используются эти перегрузки, то они
        /// переопределит маску графика для этого запроса пути.
        ///
        /// [Откройте онлайн-документацию, чтобы увидеть изображения]
        ///
        /// Смотрите: типы нескольких агентов (рабочие ссылки смотрите в онлайн-документации)
        /// </summary>
        [HideInInspector]
		public GraphMask graphMask = GraphMask.everything;

        /// <summary>Используется для обратной совместимости с сериализацией</summary>
        [UnityEngine.Serialization.FormerlySerializedAs("graphMask")]
		int graphMaskCompatibility = -1;

        /// <summary>
        /// Обратный вызов для завершения пути.
        /// Скрипты перемещения должны регистрироваться для этого делегата.
        /// Временный обратный вызов также может быть установлен при вызове Start Path, но этот делегат будет вызываться только для этого пути
        /// </summary>
        public OnPathDelegate pathCallback;

        /// <summary>Вызывается перед запуском поиска пути</summary>
        public OnPathDelegate preProcessPath;

        /// <summary>Вызывается после вычисления пути, непосредственно перед выполнением модификаторов.</summary>
        public OnPathDelegate postProcessPath;

        /// <summary>Используется для рисования вещиц gizmos</summary>
        [System.NonSerialized]
		List<Vector3> lastCompletedVectorPath;

        /// <summary>Используется для рисования вещиц gizmos</summary>
        [System.NonSerialized]
		List<GraphNode> lastCompletedNodePath;

        /// <summary>Текущий путь</summary>
        [System.NonSerialized]
		protected Path path;

        /// <summary>Предыдущий путь. Раньше рисовал gizmos</summary>
        [System.NonSerialized]
		private Path prevPath;

        /// <summary>Кэшированный делегат, чтобы избежать выделения одного делегата при каждом запуске пути</summary>
        private readonly OnPathDelegate onPathDelegate;
        /// <summary>Кэшированный делегат, чтобы избежать выделения одного делегата при каждом запуске пути</summary>
        private readonly OnPathDelegate onPartialPathDelegate;

        /// <summary>Временный обратный вызов вызывается только для текущего пути. Это значение устанавливается функциями начального пути StartPath functions</summary>
        private OnPathDelegate tmpPathCallback;

        /// <summary>Идентификатор пути последнего запрошенного пути</summary>
        protected uint lastPathID;

        /// <summary>Внутренний список всех модификаторов</summary>
        readonly List<IPathModifier> modifiers = new List<IPathModifier>();

		public enum ModifierPass {
			PreProcess,
            // Устаревший элемент ранее занимал индекс 1
            PostProcess = 2,
		}

		public Seeker () {
			onPathDelegate = OnPathComplete;
			onPartialPathDelegate = OnPartialPathComplete;
		}

        /// <summary>Инициализирует несколько переменных</summary>
        protected override void Awake () {
			base.Awake();
			startEndModifier.Awake(this);
		}

        /// <summary>
        /// Путь, который вычисляется в данный момент или был вычислен последним.
        /// Вам редко придется этим пользоваться. Вместо этого получите путь при вызове обратного вызова path.
        /// 
        /// Смотрите: обратный вызов пути  path Callback
        /// </summary>
        public Path GetCurrentPath () {
			return path;
		}

        /// <summary>
        /// Прекратите вычисление текущего запроса пути.
        /// Если этот искатель в данный момент вычисляет путь, он будет отменен.
        /// Вскоре будет вызван обратный вызов (обычно для метода с именем OnPathComplete)
        /// с путем, для поля "ошибка" которого установлено значение true.
        ///
        /// Это не останавливает перемещение персонажа, оно просто прерывается
        /// вычисление пути.
        /// </summary>
        /// <param name="pool">Если true, то путь будет объединен в пул, когда система поиска путей завершит работу с ним.</param>
        public void CancelCurrentPathRequest (bool pool = true) {
			if (!IsDone()) {
				path.FailWithError("Canceled by script (Seeker.CancelCurrentPathRequest)");
				if (pool) {
                    // Убедитесь, что количество ссылок на путь было увеличено и уменьшено один раз.
                    // Если это не будет сделано, система подумает, что пул вообще не используется, и не будет объединять путь.
                    // Конкретный объект, который используется в качестве параметра (в данном случае 'path'), вообще не имеет значения
                    // это просто должен быть *какой-то* объект.
                    path.Claim(path);
					path.Release(path);
				}
			}
		}

        /// <summary>
        /// Очищает некоторые переменные.
        /// Освобождает любые в конечном итоге заявленные пути.
        /// Calls OnDestroy on the <see cref="startEndModifier"/>.
        ///
        /// See: <see cref="ReleaseClaimedPath"/>
        /// See: <see cref="startEndModifier"/>
        /// </summary>
        public void OnDestroy () {
			ReleaseClaimedPath();
			startEndModifier.OnDestroy(this);
		}

        /// <summary>
        /// Освобождает путь, используемый для gizmos (если таковые имеются).
        /// Искатель сохраняет последний заявленный путь, чтобы он мог рисовать вещицы.
        /// /// В некоторых случаях это может быть нежелательно, и вы хотите, чтобы оно было выпущено.
        /// В этом случае вы можете вызвать этот метод, чтобы освободить его (не то, чтобы путевые штуковины тогда не рисовались).
        ///
        /// Если вы ничего не поняли из приведенного выше описания, вам, вероятно, не нужно использовать этот метод.
        ///
        /// Смотрите: объединение pooling (рабочие ссылки смотрите в онлайн-документации)
        /// </summary>
        void ReleaseClaimedPath () {
			if (prevPath != null) {
				prevPath.Release(this, true);
				prevPath = null;
			}
		}

        /// <summary>Вызывается модификаторами для регистрации самих себя</summary>
        public void RegisterModifier (IPathModifier modifier) {
			modifiers.Add(modifier);

            // Отсортируйте модификаторы в соответствии с их указанным порядком
            modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
		}

        /// <summary>Вызывается модификаторами, когда они отключены или уничтожены</summary>
        public void DeregisterModifier (IPathModifier modifier) {
			modifiers.Remove(modifier);
		}

		/// <summary>
		/// Post Processes the path.
		/// This will run any modifiers attached to this GameObject on the path.
		/// This is identical to calling RunModifiers(ModifierPass.PostProcess, path)
		/// See: RunModifiers
		/// Since: Added in 3.2
		/// </summary>
		public void PostProcess (Path path) {
			RunModifiers(ModifierPass.PostProcess, path);
		}

		/// <summary>Runs modifiers on a path</summary>
		public void RunModifiers (ModifierPass pass, Path path) {
			if (pass == ModifierPass.PreProcess) {
				if (preProcessPath != null) preProcessPath(path);

				for (int i = 0; i < modifiers.Count; i++) modifiers[i].PreProcess(path);
			} else if (pass == ModifierPass.PostProcess) {
				Profiler.BeginSample("Running Path Modifiers");
				// Call delegates if they exist
				if (postProcessPath != null) postProcessPath(path);

				// Loop through all modifiers and apply post processing
				for (int i = 0; i < modifiers.Count; i++) modifiers[i].Apply(path);
				Profiler.EndSample();
			}
		}

        /// <summary>
        /// Выполнен ли расчет текущего пути.
        /// Возвращает значение true, если был возвращен текущий <see cref="path"/>  или если был возвращен <see cref="path"/> равный null.
        ///
        /// Примечание: Не путайте это с Pathfinding.Path.IsDone. Обычно они возвращают одно и то же значение, но не всегда,
		/// поскольку путь может быть полностью вычислен, но он еще не был обработан искателем. Seeker.
        ///
        /// Since: Added in 3.0.8
        /// Version: Behaviour changed in 3.2
        /// </summary>
        public bool IsDone () {
			return path == null || path.PipelineState >= PathState.Returned;
		}

        /// <summary>
        /// Вызывается, когда путь завершен.
        /// Это должно было быть реализовано в виде необязательных значений параметров, но, похоже, это не очень хорошо работало с делегатами (значения не были заданы по умолчанию)
        /// See: OnPathComplete(Path,bool,bool)
        /// </summary>
        void OnPathComplete (Path path) {
			OnPathComplete(path, true, true);
		}

        /// <summary>
        /// Вызывается, когда путь завершен.
        /// Обработает его post и вернет, вызвав <see cref="tmpPathCallback"/> и <see cref="pathCallback"/>
        /// </summary>
        void OnPathComplete (Path p, bool runModifiers, bool sendCallbacks) {
			if (p != null && p != path && sendCallbacks) {
				return;
			}

			if (this == null || p == null || p != path)
				return;

			if (!path.error && runModifiers) {
				// This will send the path for post processing to modifiers attached to this Seeker
				RunModifiers(ModifierPass.PostProcess, path);
			}

			if (sendCallbacks) {
				p.Claim(this);

				lastCompletedNodePath = p.path;
				lastCompletedVectorPath = p.vectorPath;

				// This will send the path to the callback (if any) specified when calling StartPath
				if (tmpPathCallback != null) {
					tmpPathCallback(p);
				}

				// This will send the path to any script which has registered to the callback
				if (pathCallback != null) {
					pathCallback(p);
				}

				// Note: it is important that #prevPath is kept alive (i.e. not pooled)
				// if we are drawing gizmos.
				// It is also important that #path is kept alive since it can be returned
				// from the GetCurrentPath method.
				// Since #path will be copied to #prevPath it is sufficient that #prevPath
				// is kept alive until it is replaced.

				// Recycle the previous path to reduce the load on the GC
				if (prevPath != null) {
					prevPath.Release(this, true);
				}

				prevPath = p;
			}
		}

		/// <summary>
		/// Called for each path in a MultiTargetPath.
		/// Only post processes the path, does not return it.
		/// </summary>
		void OnPartialPathComplete (Path p) {
			OnPathComplete(p, true, false);
		}

		/// <summary>Called once for a MultiTargetPath. Only returns the path, does not post process.</summary>
		void OnMultiPathComplete (Path p) {
			OnPathComplete(p, false, true);
		}

		/// <summary>
		/// Returns a new path instance.
		/// The path will be taken from the path pool if path recycling is turned on.
		/// This path can be sent to <see cref="StartPath(Path,OnPathDelegate,int)"/> with no change, but if no change is required <see cref="StartPath(Vector3,Vector3,OnPathDelegate)"/> does just that.
		/// <code>
		/// var seeker = GetComponent<Seeker>();
		/// Path p = seeker.GetNewPath (transform.position, transform.position+transform.forward*100);
		/// // Disable heuristics on just this path for example
		/// p.heuristic = Heuristic.None;
		/// seeker.StartPath (p, OnPathComplete);
		/// </code>
		/// Deprecated: Use ABPath.Construct(start, end, null) instead.
		/// </summary>
		[System.Obsolete("Use ABPath.Construct(start, end, null) instead")]
		public ABPath GetNewPath (Vector3 start, Vector3 end) {
			// Construct a path with start and end points
			return ABPath.Construct(start, end, null);
		}

        /// <summary>
        /// Вызовите эту функцию, чтобы начать вычисление пути.
        /// Поскольку этот метод не принимает параметр обратного вызова, вы должны установить <see cref="pathCallback"/> поле перед вызовом этого метода.
        /// </summary>
        /// <param name="start">Начальная точка пути</param>
        /// <param name="end">Конечная точка пути</param>
        public Path StartPath (Vector3 start, Vector3 end) {
			return StartPath(start, end, null);
		}

		/// <summary>
		/// Call this function to start calculating a path.
		///
		/// The callback will be called when the path has been calculated (which may be several frames into the future).
		/// Callback will not be called if the path is canceled (e.g when a new path is requested before the previous one has completed)
		/// </summary>
		/// <param name="start">The start point of the path</param>
		/// <param name="end">The end point of the path</param>
		/// <param name="callback">The function to call when the path has been calculated</param>
		public Path StartPath (Vector3 start, Vector3 end, OnPathDelegate callback) {
			return StartPath(ABPath.Construct(start, end, null), callback);
		}

        /// <summary>
        /// Вызовите эту функцию, чтобы начать вычисление пути.
        ///
        /// Обратный вызов будет вызван, когда путь будет вычислен (что может произойти через несколько кадров в будущем).
        /// Обратный вызов не будет вызван, если путь отменен (например, когда запрашивается новый путь до завершения предыдущего)
        /// </summary>
        /// <param name="start">The start point of the path</param>
        /// <param name="end">The end point of the path</param>
        /// <param name="callback">The function to call when the path has been calculated</param>
        /// <param name="graphMask">Mask used to specify which graphs should be searched for close nodes. See #Pathfinding.NNConstraint.graphMask. This will override #graphMask for this path request.</param>
        public Path StartPath (Vector3 start, Vector3 end, OnPathDelegate callback, GraphMask graphMask) {
			return StartPath(ABPath.Construct(start, end, null), callback, graphMask);
		}

        /// <summary>
        /// Вызовите эту функцию, чтобы начать вычисление пути.
        ///
        /// Обратный вызов будет вызван, когда путь будет вычислен (что может произойти через несколько кадров в будущем).
        /// Обратный вызов не будет вызван, если новый запрос пути запущен до того, как этот запрос пути был вычислен.
        ///
        /// Версия: Начиная с 3.8.3, этот метод работает должным образом, если используется MultiTargetPath.
        /// Теперь он ведет себя идентично методу Start MultiTargetPath(MultiTargetPath).
        ///
        /// Версия: Начиная с 4.1.x этот метод больше не будет перезаписывать маску графика в пути, если она явно не передана в качестве параметра (см. другие перегрузки этого метода).
        /// </summary>
        /// <param name="p">Путь для начала вычисления</param>
        /// <param name="callback">Функция, вызываемая после вычисления пути</param>
        public Path StartPath (Path p, OnPathDelegate callback = null) {
            // Устанавливайте маску графика только в том случае, если пользователь не изменил ее по сравнению со значением по умолчанию.
            // Это не идеально, так как пользователь, возможно, хотел, чтобы оно было точно равно -1
            // однако это лучшее обнаружение, которое я могу сделать.
            // // Проверка не по умолчанию проводится в первую очередь по соображениям совместимости, чтобы избежать нарушения существующего кода пользователей.
            // // Вместо этого для установки маски графика следует использовать начальный путь, перегруженный явным полем graph Mask.
            if (p.nnConstraint.graphMask == -1) p.nnConstraint.graphMask = graphMask;
			StartPathInternal(p, callback);
			return p;
		}

        /// <summary>
        /// Вызовите эту функцию, чтобы начать вычисление пути.
        ///
        /// Обратный вызов будет вызван, когда путь будет вычислен (что может произойти через несколько кадров в будущем).
        /// Обратный вызов не будет вызван, если новый запрос пути запущен до того, как этот запрос пути был вычислен.
        ///
        /// Версия: Начиная с 3.8.3, этот метод работает должным образом, если используется MultiTargetPath.
        /// Теперь он ведет себя идентично методу Start MultiTargetPath(MultiTargetPath).
        /// </summary>
        /// <param name="p">The path to start calculating</param>
        /// <param name="callback">The function to call when the path has been calculated</param>
        /// <param name="graphMask">Mask used to specify which graphs should be searched for close nodes. See #Pathfinding.GraphMask. This will override #graphMask for this path request.</param>
        public Path StartPath (Path p, OnPathDelegate callback, GraphMask graphMask) {
			p.nnConstraint.graphMask = graphMask;
			StartPathInternal(p, callback);
			return p;
		}

		/// <summary>Internal method to start a path and mark it as the currently active path</summary>
		void StartPathInternal (Path p, OnPathDelegate callback) {
			var mtp = p as MultiTargetPath;
			if (mtp != null) {
				// TODO: Allocation, cache
				var callbacks = new OnPathDelegate[mtp.targetPoints.Length];

				for (int i = 0; i < callbacks.Length; i++) {
					callbacks[i] = onPartialPathDelegate;
				}

				mtp.callbacks = callbacks;
				p.callback += OnMultiPathComplete;
			} else {
				p.callback += onPathDelegate;
			}

			p.enabledTags = traversableTags;
			p.tagPenalties = tagPenalties;

			// Cancel a previously requested path is it has not been processed yet and also make sure that it has not been recycled and used somewhere else
			if (path != null && path.PipelineState <= PathState.Processing && path.CompleteState != PathCompleteState.Error && lastPathID == path.pathID) {
				path.FailWithError("Canceled path because a new one was requested.\n"+
					"This happens when a new path is requested from the seeker when one was already being calculated.\n" +
					"For example if a unit got a new order, you might request a new path directly instead of waiting for the now" +
					" invalid path to be calculated. Which is probably what you want.\n" +
					"If you are getting this a lot, you might want to consider how you are scheduling path requests.");
				// No callback will be sent for the canceled path
			}

			// Set p as the active path
			path = p;
			tmpPathCallback = callback;

			// Save the path id so we can make sure that if we cancel a path (see above) it should not have been recycled yet.
			lastPathID = path.pathID;

			// Pre process the path
			RunModifiers(ModifierPass.PreProcess, path);

			// Send the request to the pathfinder
			AstarPath.StartPath(path);
		}

		/// <summary>
		/// Starts a Multi Target Path from one start point to multiple end points.
		/// A Multi Target Path will search for all the end points in one search and will return all paths if pathsForAll is true, or only the shortest one if pathsForAll is false.
		///
		/// callback and <see cref="pathCallback"/> will be called when the path has completed. Callback will not be called if the path is canceled (e.g when a new path is requested before the previous one has completed)
		///
		/// See: Pathfinding.MultiTargetPath
		/// See: MultiTargetPathExample.cs (view in online documentation for working links) "Example of how to use multi-target-paths"
		/// </summary>
		/// <param name="start">The start point of the path</param>
		/// <param name="endPoints">The end points of the path</param>
		/// <param name="pathsForAll">Indicates whether or not a path to all end points should be searched for or only to the closest one</param>
		/// <param name="callback">The function to call when the path has been calculated</param>
		/// <param name="graphMask">Mask used to specify which graphs should be searched for close nodes. See Pathfinding.NNConstraint.graphMask.</param>
		public MultiTargetPath StartMultiTargetPath (Vector3 start, Vector3[] endPoints, bool pathsForAll, OnPathDelegate callback = null, int graphMask = -1) {
			MultiTargetPath p = MultiTargetPath.Construct(start, endPoints, null, null);

			p.pathsForAll = pathsForAll;
			StartPath(p, callback, graphMask);
			return p;
		}

		/// <summary>
		/// Starts a Multi Target Path from multiple start points to a single target point.
		/// A Multi Target Path will search from all start points to the target point in one search and will return all paths if pathsForAll is true, or only the shortest one if pathsForAll is false.
		///
		/// callback and <see cref="pathCallback"/> will be called when the path has completed. Callback will not be called if the path is canceled (e.g when a new path is requested before the previous one has completed)
		///
		/// See: Pathfinding.MultiTargetPath
		/// See: MultiTargetPathExample.cs (view in online documentation for working links) "Example of how to use multi-target-paths"
		/// </summary>
		/// <param name="startPoints">The start points of the path</param>
		/// <param name="end">The end point of the path</param>
		/// <param name="pathsForAll">Indicates whether or not a path from all start points should be searched for or only to the closest one</param>
		/// <param name="callback">The function to call when the path has been calculated</param>
		/// <param name="graphMask">Mask used to specify which graphs should be searched for close nodes. See Pathfinding.NNConstraint.graphMask.</param>
		public MultiTargetPath StartMultiTargetPath (Vector3[] startPoints, Vector3 end, bool pathsForAll, OnPathDelegate callback = null, int graphMask = -1) {
			MultiTargetPath p = MultiTargetPath.Construct(startPoints, end, null, null);

			p.pathsForAll = pathsForAll;
			StartPath(p, callback, graphMask);
			return p;
		}

        /// <summary>
        /// Запускает многоцелевой путь.
        /// Принимает MultiTargetPath и подключает все для отправки обратных вызовов искателю для последующей обработки.
        ///
        /// обратный вызов и <see cref="pathCallback"/>будет вызван, когда путь будет завершен. Обратный вызов не будет вызван, если путь отменен (например, когда запрашивается новый путь до завершения предыдущего).
        ///
        /// See: Pathfinding.MultiTargetPath
        /// See: MultiTargetPathExample.cs (рабочие ссылки смотрите в онлайн-документации) "Пример использования многоцелевых путей"
        ///
        /// Version: Since 3.8.3 calling this method behaves identically to calling StartPath with a MultiTargetPath.
        /// Version: Since 3.8.3 this method also sets enabledTags and tagPenalties on the path object.
        ///
        /// Deprecated: You can use StartPath instead of this method now. It will behave identically.
        /// </summary>
        /// <param name="p">The path to start calculating</param>
        /// <param name="callback">The function to call when the path has been calculated</param>
        /// <param name="graphMask">Mask used to specify which graphs should be searched for close nodes. See Pathfinding.NNConstraint.graphMask.</param>
        [System.Obsolete("You can use StartPath instead of this method now. It will behave identically.")]
		public MultiTargetPath StartMultiTargetPath (MultiTargetPath p, OnPathDelegate callback = null, int graphMask = -1) {
			StartPath(p, callback, graphMask);
			return p;
		}

		/// <summary>Draws gizmos for the Seeker</summary>
		public void OnDrawGizmos () {
			if (lastCompletedNodePath == null || !drawGizmos) {
				return;
			}

			if (detailedGizmos) {
				Gizmos.color = new Color(0.7F, 0.5F, 0.1F, 0.5F);

				if (lastCompletedNodePath != null) {
					for (int i = 0; i < lastCompletedNodePath.Count-1; i++) {
						Gizmos.DrawLine((Vector3)lastCompletedNodePath[i].position, (Vector3)lastCompletedNodePath[i+1].position);
					}
				}
			}

			Gizmos.color = new Color(0, 1F, 0, 1F);

			if (lastCompletedVectorPath != null) {
				for (int i = 0; i < lastCompletedVectorPath.Count-1; i++) {
					Gizmos.DrawLine(lastCompletedVectorPath[i], lastCompletedVectorPath[i+1]);
				}
			}
		}

		protected override int OnUpgradeSerializedData (int version, bool unityThread) {
			if (graphMaskCompatibility != -1) {
				Debug.Log("Loaded " + graphMaskCompatibility + " " + graphMask.value);
				graphMask = graphMaskCompatibility;
				graphMaskCompatibility = -1;
			}
			return base.OnUpgradeSerializedData(version, unityThread);
		}
	}
}
