using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Pathfinding {
    /// <summary>
    /// ������������ ������ ���� ��� ������ ������.
    ///
    /// ��� ���������, ������� ������������ ��� ����������� � ������ ���������� (��, ������, ������, ���� ������) ��� ��������� ��� ������� ������ ����.
    /// �� ����� ������������ ������������� ����� � �������������� �������������.
    ///
    /// [�������� ������-������������, ����� ������� �����������]
    ///
    /// ��������: �����-����� ���� (������� ������ �������� � ������-������������)
    /// ��������: ������������ (������� ������ �������� � ������-������������)
    /// </summary>
    [AddComponentMenu("Pathfinding/Seeker")]
	[HelpURL("http://arongranberg.com/astar/documentation/stable/class_pathfinding_1_1_seeker.php")]
	public class Seeker : VersionedMonoBehaviour {
        /// <summary>
        /// ��������� ������������ ��������� ����������� ���� � ������� Gizmos.
        /// ���� ����� ������� ������� ������.
        ///
        /// ��������: OnDrawGizmos
        /// </summary>
        public bool drawGizmos = true;

        /// <summary>
        /// ��������� ������������ �������������� ������ � ������� Gizmos.
        /// ���� ����� ������� ��������� ������.
        ///
        /// /// ���������, ����� <��. cref="drawGizmos"/> ���� true.
        ///
        /// ��� ������� ���� �� ����, ��� ����� ��������� ����� ����������� ���������, ����� ��� �����������.
        ///
        /// ��������: drawGizmos
        /// ��������: OnDrawGizmos
        /// </summary>
        public bool detailedGizmos;

        /// <summary>����������� ����, ������� �������� ��������� � �������� ����� ����</summary>
        [HideInInspector]
		public StartEndModifier startEndModifier = new StartEndModifier();

        /// <summary>
        /// �����, �� ������� ����� ������������ ��������.
        ///
        /// ����������: ��� ���� �������� ������� ������.
        /// ��������: ������� ����� (������� ������ �������� � ������-������������)
        /// </summary>
        [HideInInspector]
		public int traversableTags = -1;

        /// <summary>
        /// ������ �� ������ ���.
        /// � ���� 0, ������� �������� ����� �� ���������, ����� �������� ����� � ������� Tagpenalty[0].
        /// ��� ������ ���� ������ ������������� ��������, ��������� �������� A * �� ����� ������������ ������������� ������.
        ///
        /// ����������: ���� ������ ������ ������ ����� ����� 32, � ��������� ������ ������� ������������� ���.
        ///
        /// ��������: ����� ����.Path.Tagpenalities
        /// </summary>
        [HideInInspector]
		public int[] tagPenalties = new int[32];

        /// <summary>
        /// �������, ������� ����� ������������ ���� ��������.
        /// ��� ���� ����������, ����� ������� ����� ����������� ��� ������ ���������� � ��������� ����� ����.
        /// ��� ������� �� ������ ���������, ��������, ���� �� ������ ������� ���� ������ ��� ��������� ������ ��������� � ���� ������ ��� ������� ������ ���������.
        ///
        /// ��� ������� �����, �������, ���� ��, ��������, ������ ��������� ������ ������������ ������ ������ ������� 3, �� ������ ���������� ��� �������� �:
        /// <���> seeker.graphMask = 1 << 3; </���>
        ///
        /// ��������: ������� ����� (������� ������ �������� � ������-������������)
        ///
        /// �������� ��������, ��� � ���� ���� �������� ������ �� ������� �������, ������� ���������. ��� ��������, ��� ���� ������� ������� ���� �������
        /// ����� ��� ����� ����� ������ �� ���� ����������.
        ///
        /// ���� �� ������ �������� �������, �� ������ ������������ <��. cref="����� ����.GraphMask.FromGraphName"/> �����:
        /// <���>
        /// GraphMask mask1 = GraphMask.FromGraphName("��� �������� ������");
        /// GraphMask mask2 = GraphMask.FromGraphName("��� ������ �������� ������");
        ///
        /// NNConstraint nn = NNConstraint.�� ���������;
        ///
        /// nn.graphMask = mask1 | mask2;
        ///
        /// // ������� ����, ��������� � ��������� �����, ������� ��������� ���� � "���� �������� �������", ���� � "���� ������ �������� �������".
        /// var info = AstarPath.active.�������� ���������(somePoint, nn);
        /// </���>
        ///
        /// ��������� ���������� ������� <��. cref="StartPath"/> ��������� �������� graphMask. ���� ������������ ��� ����������, �� ���
        /// ������������� ����� ������� ��� ����� ������� ����.
        ///
        /// [�������� ������-������������, ����� ������� �����������]
        ///
        /// ��������: ���� ���������� ������� (������� ������ �������� � ������-������������)
        /// </summary>
        [HideInInspector]
		public GraphMask graphMask = GraphMask.everything;

        /// <summary>������������ ��� �������� ������������� � �������������</summary>
        [UnityEngine.Serialization.FormerlySerializedAs("graphMask")]
		int graphMaskCompatibility = -1;

        /// <summary>
        /// �������� ����� ��� ���������� ����.
        /// ������� ����������� ������ ���������������� ��� ����� ��������.
        /// ��������� �������� ����� ����� ����� ���� ���������� ��� ������ Start Path, �� ���� ������� ����� ���������� ������ ��� ����� ����
        /// </summary>
        public OnPathDelegate pathCallback;

        /// <summary>���������� ����� �������� ������ ����</summary>
        public OnPathDelegate preProcessPath;

        /// <summary>���������� ����� ���������� ����, ��������������� ����� ����������� �������������.</summary>
        public OnPathDelegate postProcessPath;

        /// <summary>������������ ��� ��������� ����� gizmos</summary>
        [System.NonSerialized]
		List<Vector3> lastCompletedVectorPath;

        /// <summary>������������ ��� ��������� ����� gizmos</summary>
        [System.NonSerialized]
		List<GraphNode> lastCompletedNodePath;

        /// <summary>������� ����</summary>
        [System.NonSerialized]
		protected Path path;

        /// <summary>���������� ����. ������ ������� gizmos</summary>
        [System.NonSerialized]
		private Path prevPath;

        /// <summary>������������ �������, ����� �������� ��������� ������ �������� ��� ������ ������� ����</summary>
        private readonly OnPathDelegate onPathDelegate;
        /// <summary>������������ �������, ����� �������� ��������� ������ �������� ��� ������ ������� ����</summary>
        private readonly OnPathDelegate onPartialPathDelegate;

        /// <summary>��������� �������� ����� ���������� ������ ��� �������� ����. ��� �������� ��������������� ��������� ���������� ���� StartPath functions</summary>
        private OnPathDelegate tmpPathCallback;

        /// <summary>������������� ���� ���������� ������������ ����</summary>
        protected uint lastPathID;

        /// <summary>���������� ������ ���� �������������</summary>
        readonly List<IPathModifier> modifiers = new List<IPathModifier>();

		public enum ModifierPass {
			PreProcess,
            // ���������� ������� ����� ������� ������ 1
            PostProcess = 2,
		}

		public Seeker () {
			onPathDelegate = OnPathComplete;
			onPartialPathDelegate = OnPartialPathComplete;
		}

        /// <summary>�������������� ��������� ����������</summary>
        protected override void Awake () {
			base.Awake();
			startEndModifier.Awake(this);
		}

        /// <summary>
        /// ����, ������� ����������� � ������ ������ ��� ��� �������� ���������.
        /// ��� ����� �������� ���� ������������. ������ ����� �������� ���� ��� ������ ��������� ������ path.
        /// 
        /// ��������: �������� ����� ����  path Callback
        /// </summary>
        public Path GetCurrentPath () {
			return path;
		}

        /// <summary>
        /// ���������� ���������� �������� ������� ����.
        /// ���� ���� �������� � ������ ������ ��������� ����, �� ����� �������.
        /// ������ ����� ������ �������� ����� (������ ��� ������ � ������ OnPathComplete)
        /// � �����, ��� ���� "������" �������� ����������� �������� true.
        ///
        /// ��� �� ������������� ����������� ���������, ��� ������ �����������
        /// ���������� ����.
        /// </summary>
        /// <param name="pool">���� true, �� ���� ����� ��������� � ���, ����� ������� ������ ����� �������� ������ � ���.</param>
        public void CancelCurrentPathRequest (bool pool = true) {
			if (!IsDone()) {
				path.FailWithError("Canceled by script (Seeker.CancelCurrentPathRequest)");
				if (pool) {
                    // ���������, ��� ���������� ������ �� ���� ���� ��������� � ��������� ���� ���.
                    // ���� ��� �� ����� �������, ������� ��������, ��� ��� ������ �� ������������, � �� ����� ���������� ����.
                    // ���������� ������, ������� ������������ � �������� ��������� (� ������ ������ 'path'), ������ �� ����� ��������
                    // ��� ������ ������ ���� *�����-��* ������.
                    path.Claim(path);
					path.Release(path);
				}
			}
		}

        /// <summary>
        /// ������� ��������� ����������.
        /// ����������� ����� � �������� ����� ���������� ����.
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
        /// ����������� ����, ������������ ��� gizmos (���� ������� �������).
        /// �������� ��������� ��������� ���������� ����, ����� �� ��� �������� ������.
        /// /// � ��������� ������� ��� ����� ���� ������������, � �� ������, ����� ��� ���� ��������.
        /// � ���� ������ �� ������ ������� ���� �����, ����� ���������� ��� (�� ��, ����� ������� ��������� ����� �� ����������).
        ///
        /// ���� �� ������ �� ������ �� ������������ ���� ��������, ���, ��������, �� ����� ������������ ���� �����.
        ///
        /// ��������: ����������� pooling (������� ������ �������� � ������-������������)
        /// </summary>
        void ReleaseClaimedPath () {
			if (prevPath != null) {
				prevPath.Release(this, true);
				prevPath = null;
			}
		}

        /// <summary>���������� �������������� ��� ����������� ����� ����</summary>
        public void RegisterModifier (IPathModifier modifier) {
			modifiers.Add(modifier);

            // ������������ ������������ � ������������ � �� ��������� ��������
            modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
		}

        /// <summary>���������� ��������������, ����� ��� ��������� ��� ����������</summary>
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
        /// �������� �� ������ �������� ����.
        /// ���������� �������� true, ���� ��� ��������� ������� <see cref="path"/>  ��� ���� ��� ��������� <see cref="path"/> ������ null.
        ///
        /// ����������: �� ������� ��� � Pathfinding.Path.IsDone. ������ ��� ���������� ���� � �� �� ��������, �� �� ������,
		/// ��������� ���� ����� ���� ��������� ��������, �� �� ��� �� ��� ��������� ���������. Seeker.
        ///
        /// Since: Added in 3.0.8
        /// Version: Behaviour changed in 3.2
        /// </summary>
        public bool IsDone () {
			return path == null || path.PipelineState >= PathState.Returned;
		}

        /// <summary>
        /// ����������, ����� ���� ��������.
        /// ��� ������ ���� ���� ����������� � ���� �������������� �������� ����������, ��, ������, ��� �� ����� ������ �������� � ���������� (�������� �� ���� ������ �� ���������)
        /// See: OnPathComplete(Path,bool,bool)
        /// </summary>
        void OnPathComplete (Path path) {
			OnPathComplete(path, true, true);
		}

        /// <summary>
        /// ����������, ����� ���� ��������.
        /// ���������� ��� post � ������, ������ <see cref="tmpPathCallback"/> � <see cref="pathCallback"/>
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
        /// �������� ��� �������, ����� ������ ���������� ����.
        /// ��������� ���� ����� �� ��������� �������� ��������� ������, �� ������ ���������� <see cref="pathCallback"/> ���� ����� ������� ����� ������.
        /// </summary>
        /// <param name="start">��������� ����� ����</param>
        /// <param name="end">�������� ����� ����</param>
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
        /// �������� ��� �������, ����� ������ ���������� ����.
        ///
        /// �������� ����� ����� ������, ����� ���� ����� �������� (��� ����� ��������� ����� ��������� ������ � �������).
        /// �������� ����� �� ����� ������, ���� ���� ������� (��������, ����� ������������� ����� ���� �� ���������� �����������)
        /// </summary>
        /// <param name="start">The start point of the path</param>
        /// <param name="end">The end point of the path</param>
        /// <param name="callback">The function to call when the path has been calculated</param>
        /// <param name="graphMask">Mask used to specify which graphs should be searched for close nodes. See #Pathfinding.NNConstraint.graphMask. This will override #graphMask for this path request.</param>
        public Path StartPath (Vector3 start, Vector3 end, OnPathDelegate callback, GraphMask graphMask) {
			return StartPath(ABPath.Construct(start, end, null), callback, graphMask);
		}

        /// <summary>
        /// �������� ��� �������, ����� ������ ���������� ����.
        ///
        /// �������� ����� ����� ������, ����� ���� ����� �������� (��� ����� ��������� ����� ��������� ������ � �������).
        /// �������� ����� �� ����� ������, ���� ����� ������ ���� ������� �� ����, ��� ���� ������ ���� ��� ��������.
        ///
        /// ������: ������� � 3.8.3, ���� ����� �������� ������� �������, ���� ������������ MultiTargetPath.
        /// ������ �� ����� ���� ��������� ������ Start MultiTargetPath(MultiTargetPath).
        ///
        /// ������: ������� � 4.1.x ���� ����� ������ �� ����� �������������� ����� ������� � ����, ���� ��� ���� �� �������� � �������� ��������� (��. ������ ���������� ����� ������).
        /// </summary>
        /// <param name="p">���� ��� ������ ����������</param>
        /// <param name="callback">�������, ���������� ����� ���������� ����</param>
        public Path StartPath (Path p, OnPathDelegate callback = null) {
            // �������������� ����� ������� ������ � ��� ������, ���� ������������ �� ������� �� �� ��������� �� ��������� �� ���������.
            // ��� �� ��������, ��� ��� ������������, ��������, �����, ����� ��� ���� ����� ����� -1
            // ������ ��� ������ �����������, ������� � ���� �������.
            // // �������� �� �� ��������� ���������� � ������ ������� �� ������������ �������������, ����� �������� ��������� ������������� ���� �������������.
            // // ������ ����� ��� ��������� ����� ������� ������� ������������ ��������� ����, ������������� ����� ����� graph Mask.
            if (p.nnConstraint.graphMask == -1) p.nnConstraint.graphMask = graphMask;
			StartPathInternal(p, callback);
			return p;
		}

        /// <summary>
        /// �������� ��� �������, ����� ������ ���������� ����.
        ///
        /// �������� ����� ����� ������, ����� ���� ����� �������� (��� ����� ��������� ����� ��������� ������ � �������).
        /// �������� ����� �� ����� ������, ���� ����� ������ ���� ������� �� ����, ��� ���� ������ ���� ��� ��������.
        ///
        /// ������: ������� � 3.8.3, ���� ����� �������� ������� �������, ���� ������������ MultiTargetPath.
        /// ������ �� ����� ���� ��������� ������ Start MultiTargetPath(MultiTargetPath).
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
        /// ��������� ������������ ����.
        /// ��������� MultiTargetPath � ���������� ��� ��� �������� �������� ������� �������� ��� ����������� ���������.
        ///
        /// �������� ����� � <see cref="pathCallback"/>����� ������, ����� ���� ����� ��������. �������� ����� �� ����� ������, ���� ���� ������� (��������, ����� ������������� ����� ���� �� ���������� �����������).
        ///
        /// See: Pathfinding.MultiTargetPath
        /// See: MultiTargetPathExample.cs (������� ������ �������� � ������-������������) "������ ������������� ������������ �����"
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
