using UnityEngine;

namespace Pathfinding {
    /// <summary>Содержит координату в целых числах</summary>
    public struct Int3 : System.IEquatable<Int3> {
		public int x;
		public int y;
		public int z;

        //Для них должно быть установлено одинаковое значение (только коэффициент точности PrecisionFactor должен быть равен 1, деленный на точность)

        /// <summary>
        /// Точность для целочисленных координат.
        /// Одна мировая единица делится на [стоимостные] части. Значение 1000 будет означать точность в миллиметрах, значение 1 будет означать точность в метрах (при условии, что 1 мировая единица измерения = 1 метр).
        /// Это значение влияет на максимальные координаты узлов, а также на то, насколько велики значения затрат на перемещение между двумя узлами.
        /// Более высокое значение означает, что вы также должны установить для всех штрафных значений более высокое значение для компенсации, поскольку обычная стоимость перемещения будет выше.
        /// </summary>
        public const int Precision = 1000;

		/// <summary><see cref="Precision"/> as a float</summary>
		public const float FloatPrecision = 1000F;

		/// <summary>1 divided by <see cref="Precision"/></summary>
		public const float PrecisionFactor = 0.001F;

		public static Int3 zero { get { return new Int3(); } }

		public Int3 (Vector3 position) {
			x = (int)System.Math.Round(position.x*FloatPrecision);
			y = (int)System.Math.Round(position.y*FloatPrecision);
			z = (int)System.Math.Round(position.z*FloatPrecision);
		}

		public Int3 (int _x, int _y, int _z) {
			x = _x;
			y = _y;
			z = _z;
		}

		public static bool operator == (Int3 lhs, Int3 rhs) {
			return lhs.x == rhs.x &&
				   lhs.y == rhs.y &&
				   lhs.z == rhs.z;
		}

		public static bool operator != (Int3 lhs, Int3 rhs) {
			return lhs.x != rhs.x ||
				   lhs.y != rhs.y ||
				   lhs.z != rhs.z;
		}

		public static explicit operator Int3 (Vector3 ob) {
			return new Int3(
				(int)System.Math.Round(ob.x*FloatPrecision),
				(int)System.Math.Round(ob.y*FloatPrecision),
				(int)System.Math.Round(ob.z*FloatPrecision)
				);
		}

		public static explicit operator Vector3 (Int3 ob) {
			return new Vector3(ob.x*PrecisionFactor, ob.y*PrecisionFactor, ob.z*PrecisionFactor);
		}

		public static Int3 operator - (Int3 lhs, Int3 rhs) {
			lhs.x -= rhs.x;
			lhs.y -= rhs.y;
			lhs.z -= rhs.z;
			return lhs;
		}

		public static Int3 operator - (Int3 lhs) {
			lhs.x = -lhs.x;
			lhs.y = -lhs.y;
			lhs.z = -lhs.z;
			return lhs;
		}

		public static Int3 operator + (Int3 lhs, Int3 rhs) {
			lhs.x += rhs.x;
			lhs.y += rhs.y;
			lhs.z += rhs.z;
			return lhs;
		}

		public static Int3 operator * (Int3 lhs, int rhs) {
			lhs.x *= rhs;
			lhs.y *= rhs;
			lhs.z *= rhs;

			return lhs;
		}

		public static Int3 operator * (Int3 lhs, float rhs) {
			lhs.x = (int)System.Math.Round(lhs.x * rhs);
			lhs.y = (int)System.Math.Round(lhs.y * rhs);
			lhs.z = (int)System.Math.Round(lhs.z * rhs);

			return lhs;
		}

		public static Int3 operator * (Int3 lhs, double rhs) {
			lhs.x = (int)System.Math.Round(lhs.x * rhs);
			lhs.y = (int)System.Math.Round(lhs.y * rhs);
			lhs.z = (int)System.Math.Round(lhs.z * rhs);

			return lhs;
		}

		public static Int3 operator / (Int3 lhs, float rhs) {
			lhs.x = (int)System.Math.Round(lhs.x / rhs);
			lhs.y = (int)System.Math.Round(lhs.y / rhs);
			lhs.z = (int)System.Math.Round(lhs.z / rhs);
			return lhs;
		}

		public int this[int i] {
			get {
				return i == 0 ? x : (i == 1 ? y : z);
			}
			set {
				if (i == 0) x = value;
				else if (i == 1) y = value;
				else z = value;
			}
		}

        /// <summary>Угол между векторами в радианах</summary>
        public static float Angle (Int3 lhs, Int3 rhs) {
			double cos = Dot(lhs, rhs)/ ((double)lhs.magnitude*(double)rhs.magnitude);

			cos = cos < -1 ? -1 : (cos > 1 ? 1 : cos);
			return (float)System.Math.Acos(cos);
		}

		public static int Dot (Int3 lhs, Int3 rhs) {
			return
				lhs.x * rhs.x +
				lhs.y * rhs.y +
				lhs.z * rhs.z;
		}

		public static long DotLong (Int3 lhs, Int3 rhs) {
			return
				(long)lhs.x * (long)rhs.x +
				(long)lhs.y * (long)rhs.y +
				(long)lhs.z * (long)rhs.z;
		}

        /// <summary>
        ///Обычный в двумерном пространстве (XZ).
        /// Эквивалент Cross(this, Int 3(0,1,0) )
        /// за исключением того, что при выполнении этой операции координата Y остается неизменной.
        /// </summary>
        public Int3 Normal2D () {
			return new Int3(z, y, -x);
		}

        /// <summary>
        /// Возвращает величину вектора. Величина - это "длина" вектора от 0,0,0 до этой точки. Может использоваться для расчета расстояния:
        /// <code> Debug.Log ("Distance between 3,4,5 and 6,7,8 is: "+(new Int3(3,4,5) - new Int3(6,7,8)).magnitude); </code>
        /// </summary>
        public float magnitude {
			get {
                //Оказывается, использование double так же быстро, как и использование ints с Mathf.Sqrt. И это также может обрабатывать большие числа (возможно, с небольшими ошибками при использовании огромных чисел)!

                double _x = x;
				double _y = y;
				double _z = z;

				return (float)System.Math.Sqrt(_x*_x+_y*_y+_z*_z);
			}
		}

        /// <summary>
        /// Величина, используемая для определения стоимости между двумя узлами. Стоимость по умолчанию между двумя узлами может быть рассчитана следующим образом:
        /// <code> int cost = (node1.position-node2.position).costMagnitude; </code>
        ///
        /// This is simply the magnitude, rounded to the nearest integer
        /// </summary>
        public int costMagnitude {
			get {
				return (int)System.Math.Round(magnitude);
			}
		}

        /// <summary>Квадрат величины вектора</summary>
        public float sqrMagnitude {
			get {
				double _x = x;
				double _y = y;
				double _z = z;
				return (float)(_x*_x+_y*_y+_z*_z);
			}
		}

        /// <summary>Квадрат величины вектора</summary>
        public long sqrMagnitudeLong {
			get {
				long _x = x;
				long _y = y;
				long _z = z;
				return (_x*_x+_y*_y+_z*_z);
			}
		}

		public static implicit operator string (Int3 obj) {
			return obj.ToString();
		}

        /// <summary>Возвращает красиво отформатированную строку, представляющую вектор</summary>
        public override string ToString () {
			return "( "+x+", "+y+", "+z+")";
		}

		public override bool Equals (System.Object obj) {
			if (obj == null) return false;

			var rhs = (Int3)obj;

			return x == rhs.x &&
				   y == rhs.y &&
				   z == rhs.z;
		}

		#region IEquatable implementation

		public bool Equals (Int3 other) {
			return x == other.x && y == other.y && z == other.z;
		}

		#endregion

		public override int GetHashCode () {
			return x*73856093 ^ y*19349669 ^ z*83492791;
		}
	}

    /// <summary>Двумерная целочисленная пара координат</summary>
    public struct Int2 : System.IEquatable<Int2> {
		public int x;
		public int y;

		public Int2 (int x, int y) {
			this.x = x;
			this.y = y;
		}

		public long sqrMagnitudeLong {
			get {
				return (long)x*(long)x+(long)y*(long)y;
			}
		}

		public static Int2 operator - (Int2 lhs) {
			lhs.x = -lhs.x;
			lhs.y = -lhs.y;
			return lhs;
		}

		public static Int2 operator + (Int2 a, Int2 b) {
			return new Int2(a.x+b.x, a.y+b.y);
		}

		public static Int2 operator - (Int2 a, Int2 b) {
			return new Int2(a.x-b.x, a.y-b.y);
		}

		public static bool operator == (Int2 a, Int2 b) {
			return a.x == b.x && a.y == b.y;
		}

		public static bool operator != (Int2 a, Int2 b) {
			return a.x != b.x || a.y != b.y;
		}

        /// <summary>Точечное произведение двух координат</summary>
        public static long DotLong (Int2 a, Int2 b) {
			return (long)a.x*(long)b.x + (long)a.y*(long)b.y;
		}

		public override bool Equals (System.Object o) {
			if (o == null) return false;
			var rhs = (Int2)o;

			return x == rhs.x && y == rhs.y;
		}

		#region IEquatable implementation

		public bool Equals (Int2 other) {
			return x == other.x && y == other.y;
		}

		#endregion

		public override int GetHashCode () {
			return x*49157+y*98317;
		}

		public static Int2 Min (Int2 a, Int2 b) {
			return new Int2(System.Math.Min(a.x, b.x), System.Math.Min(a.y, b.y));
		}

		public static Int2 Max (Int2 a, Int2 b) {
			return new Int2(System.Math.Max(a.x, b.x), System.Math.Max(a.y, b.y));
		}

		public static Int2 FromInt3XZ (Int3 o) {
			return new Int2(o.x, o.z);
		}

		public static Int3 ToInt3XZ (Int2 o) {
			return new Int3(o.x, 0, o.y);
		}

		public override string ToString () {
			return "("+x+", " +y+")";
		}
	}
}
