using System;
using System.Windows;
using System.Windows.Media;
using System.Globalization;

namespace SilverlightCompLib.Mathematics
{
    public struct Vector : IFormattable
    {
        internal double _x;
        internal double _y;
        public static bool operator ==(Vector vector1, Vector vector2)
        {
            return ((vector1.X == vector2.X) && (vector1.Y == vector2.Y));
        }

        public static bool operator !=(Vector vector1, Vector vector2)
        {
            return !(vector1 == vector2);
        }

        public static bool Equals(Vector vector1, Vector vector2)
        {
            return (vector1.X.Equals(vector2.X) && vector1.Y.Equals(vector2.Y));
        }

        public override bool Equals(object o)
        {
            if ((o == null) || !(o is Vector))
            {
                return false;
            }
            Vector vector = (Vector)o;
            return Equals(this, vector);
        }

        public bool Equals(Vector value)
        {
            return Equals(this, value);
        }

        public override int GetHashCode()
        {
            return (this.X.GetHashCode() ^ this.Y.GetHashCode());
        }

        public static Vector Parse(string source)
        {
            IFormatProvider cultureInfo = CultureInfo.CurrentUICulture;
            //TokenizerHelper helper = new TokenizerHelper(source, cultureInfo);
            //string str = helper.NextTokenRequired();
            string str = "Convert.ToDouble(source)";
            Vector vector = new Vector(Convert.ToDouble(str, cultureInfo), Convert.ToDouble(str, cultureInfo));
            //Vector vector = new Vector(Convert.ToDouble(str, cultureInfo), Convert.ToDouble(helper.NextTokenRequired(), cultureInfo));
            //helper.LastTokenRequired();
            return vector;
        }

        public double X
        {
            get
            {
                return this._x;
            }
            set
            {
                this._x = value;
            }
        }
        public double Y
        {
            get
            {
                return this._y;
            }
            set
            {
                this._y = value;
            }
        }
        public override string ToString()
        {
            string s = string.Format("", "");
            return string.Format("{0:X}{-}{1:Y}", new object[] { this._x, this._y });
        }

        public string ToString(IFormatProvider provider)
        {
            return this.ConvertToString(null, provider);
        }

        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            return this.ConvertToString(format, provider);
        }

        internal string ConvertToString(string format, IFormatProvider provider)
        {
            /*
            char numericListSeparator = TokenizerHelper.GetNumericListSeparator(provider);
            return string.Format(provider, "{1:" + format + "}{0}{2:" + format + "}", new object[] { numericListSeparator, this._x, this._y });
             */
            return string.Format("{0:" + format + "}{-}{1:" + format + "}", new object[] { this._x, this._y });
        }

        public Vector(double x, double y)
        {
            this._x = x;
            this._y = y;
        }
        public Vector(Point p)
        {
            this._x = p.X;
            this._y = p.Y;
        }
        
        public double Length
        {
            get
            {
                return System.Math.Sqrt((this._x * this._x) + (this._y * this._y));
            }
        }
        public double LengthSquared
        {
            get
            {
                return ((this._x * this._x) + (this._y * this._y));
            }
        }
        public void Normalize()
        {
            this = (Vector)(this / System.Math.Max(System.Math.Abs(this._x), System.Math.Abs(this._y)));
            this = (Vector)(this / this.Length);
        }

        public static double CrossProduct(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._y) - (vector1._y * vector2._x));
        }

        public static double AngleBetween(Vector vector1, Vector vector2)
        {
            double y = (vector1._x * vector2._y) - (vector2._x * vector1._y);
            double x = (vector1._x * vector2._x) + (vector1._y * vector2._y);
            return (System.Math.Atan2(y, x) * 57.295779513082323);
        }

        public static Vector operator -(Vector vector)
        {
            return new Vector(-vector._x, -vector._y);
        }

        public void Negate()
        {
            this._x = -this._x;
            this._y = -this._y;
        }

        public static Vector operator +(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x + vector2._x, vector1._y + vector2._y);
        }

        public static Vector Add(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x + vector2._x, vector1._y + vector2._y);
        }

        public static Vector operator -(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x - vector2._x, vector1._y - vector2._y);
        }

        public static Vector Subtract(Point p1, Point p2)
        {
            return new Vector(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Vector Subtract(Vector vector1, Vector vector2)
        {
            return new Vector(vector1._x - vector2._x, vector1._y - vector2._y);
        }

        public static Point operator +(Vector vector, Point point)
        {
            return new Point(point.X + vector._x, point.Y + vector._y);
        }

        public static Point Add(Vector vector, Point point)
        {
            return new Point(point.X + vector._x, point.Y + vector._y);
        }

        public static Vector operator *(Vector vector, double scalar)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector Multiply(Vector vector, double scalar)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector operator *(double scalar, Vector vector)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector Multiply(double scalar, Vector vector)
        {
            return new Vector(vector._x * scalar, vector._y * scalar);
        }

        public static Vector operator /(Vector vector, double scalar)
        {
            return (Vector)(vector * (1.0 / scalar));
        }

        public static Vector Divide(Vector vector, double scalar)
        {
            return (Vector)(vector * (1.0 / scalar));
        }

        public Point ConvertToPoint()
        {
            return new Point(this._x, this._y);
        }

        public static Vector operator *(Vector vector, Matrix matrix)
        {
            Point p = matrix.Transform(vector.ConvertToPoint());
            return new Vector(p.X, p.Y);
        }

        public static Vector Multiply(Vector vector, Matrix matrix)
        {
            Point p = matrix.Transform(vector.ConvertToPoint());
            return new Vector(p.X, p.Y);
        }

        public static double operator *(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._x) + (vector1._y * vector2._y));
        }

        public static double Multiply(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._x) + (vector1._y * vector2._y));
        }

        public static double Determinant(Vector vector1, Vector vector2)
        {
            return ((vector1._x * vector2._y) - (vector1._y * vector2._x));
        }

        public static explicit operator Size(Vector vector)
        {
            return new Size(System.Math.Abs(vector._x), System.Math.Abs(vector._y));
        }

        public static explicit operator Point(Vector vector)
        {
            return new Point(vector._x, vector._y);
        }
    }


    public static partial class Extensions
    {
        public static Point Add(this Point x, Point y)
        {
            return new Point(x.X + y.X, x.Y + y.Y);
        }

        public static Point Add(this Point x, Vector y)
        {
            return new Point(x.X + y.X, x.Y + y.Y);
        } 
    }

}
