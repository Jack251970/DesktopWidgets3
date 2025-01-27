using Newtonsoft.Json;
using Windows.Foundation;

namespace DesktopWidgets3.Core.Models;

/// <summary>
/// Represents the a rectangle.
/// Codes are edited from: <see cref="Windows.Foundation.Rect">.
/// </summary>
public struct Rect3 : IFormattable
{
    [JsonIgnore]
    public float _x;

    [JsonIgnore]
    public float _y;

    [JsonIgnore]
    public float _width;

    [JsonIgnore]
    public float _height;

    private static readonly Rect3 s_empty = CreateEmptyRect();

    public double X
    {
        readonly get => _x;
        set => _x = (float)value;
    }

    public double Y
    {
        readonly get => _y;
        set => _y = (float)value;
    }

    public double Width
    {
        readonly get => _width;
        set
        {
            if (value < 0.0)
            {
                throw new ArgumentOutOfRangeException("Width", GSR.ArgumentOutOfRange_NeedNonNegNum);
            }

            _width = (float)value;
        }
    }

    public double Height
    {
        readonly get => _height;
        set
        {
            if (value < 0.0)
            {
                throw new ArgumentOutOfRangeException("Height", GSR.ArgumentOutOfRange_NeedNonNegNum);
            }

            _height = (float)value;
        }
    }

    public Rect3(float x, float y, float width, float height)
    {
        if (width < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width), GSR.ArgumentOutOfRange_NeedNonNegNum);
        }

        if (height < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(height), GSR.ArgumentOutOfRange_NeedNonNegNum);
        }

        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    [JsonIgnore]
    public readonly double Left => _x;

    [JsonIgnore]
    public readonly double Top => _y;

    [JsonIgnore]
    public readonly double Right
    {
        get
        {
            if (IsEmpty)
            {
                return double.NegativeInfinity;
            }

            return _x + _width;
        }
    }

    [JsonIgnore]
    public readonly double Bottom
    {
        get
        {
            if (IsEmpty)
            {
                return double.NegativeInfinity;
            }

            return _y + _height;
        }
    }

    [JsonIgnore]
    public static Rect3 Empty => s_empty;

    [JsonIgnore]
    public readonly bool IsEmpty => _width < 0f;

    public Rect3(Rect rect)
        : this(rect.X, rect.Y, rect.Width, rect.Height)
    {
    }

    public Rect3(double x, double y, double width, double height)
        : this((float)x, (float)y, (float)width, (float)height)
    {
    }

    public Rect3(Point point1, Point point2)
    {
        _x = Math.Min(point1._x, point2._x);
        _y = Math.Min(point1._y, point2._y);
        _width = Math.Max(Math.Max(point1._x, point2._x) - _x, 0f);
        _height = Math.Max(Math.Max(point1._y, point2._y) - _y, 0f);
    }

    public Rect3(Point location, Size size)
    {
        if (size.IsEmpty)
        {
            this = s_empty;
            return;
        }

        _x = location._x;
        _y = location._y;
        _width = size._width;
        _height = size._height;
    }

    public readonly bool Contains(Point point)
    {
        return ContainsInternal(point._x, point._y);
    }

    public void Intersect(Rect rect)
    {
        if (!IntersectsWith(rect))
        {
            this = s_empty;
            return;
        }

        var num = Math.Max(X, rect.X);
        var num2 = Math.Max(Y, rect.Y);
        Width = Math.Max(Math.Min(X + Width, rect.X + rect.Width) - num, 0.0);
        Height = Math.Max(Math.Min(Y + Height, rect.Y + rect.Height) - num2, 0.0);
        X = num;
        Y = num2;
    }

    public void Union(Rect3 rect)
    {
        if (IsEmpty)
        {
            this = rect;
        }
        else if (!rect.IsEmpty)
        {
            var num = Math.Min(Left, rect.Left);
            var num2 = Math.Min(Top, rect.Top);
            if (rect.Width == double.PositiveInfinity || Width == double.PositiveInfinity)
            {
                Width = double.PositiveInfinity;
            }
            else
            {
                var num3 = Math.Max(Right, rect.Right);
                Width = Math.Max(num3 - num, 0.0);
            }

            if (rect.Height == double.PositiveInfinity || Height == double.PositiveInfinity)
            {
                Height = double.PositiveInfinity;
            }
            else
            {
                var num4 = Math.Max(Bottom, rect.Bottom);
                Height = Math.Max(num4 - num2, 0.0);
            }

            X = num;
            Y = num2;
        }
    }

    public void Union(Point point)
    {
        Union(new Rect3(point, point));
    }

    private readonly bool ContainsInternal(float x, float y)
    {
        if (x >= _x && x - _width <= _x && y >= _y)
        {
            return y - _height <= _y;
        }

        return false;
    }

    internal readonly bool IntersectsWith(Rect rect)
    {
        if (_width < 0f || rect._width < 0f)
        {
            return false;
        }

        if (rect._x <= _x + _width && rect._x + rect._width >= _x && rect._y <= _y + _height)
        {
            return rect._y + rect._height >= _y;
        }

        return false;
    }

    private static Rect3 CreateEmptyRect()
    {
        var result = default(Rect3);
        result._x = float.PositiveInfinity;
        result._y = float.PositiveInfinity;
        result._width = float.NegativeInfinity;
        result._height = float.NegativeInfinity;
        return result;
    }

    public readonly override string ToString()
    {
        return ConvertToString(null, null);
    }

    public readonly string ToString(IFormatProvider provider)
    {
        return ConvertToString(null, provider);
    }

    readonly string IFormattable.ToString(string? format, IFormatProvider? provider)
    {
        return ConvertToString(format, provider);
    }

    internal readonly string ConvertToString(string? format, IFormatProvider? provider)
    {
        if (IsEmpty)
        {
            return "Empty.";
        }

        var numericListSeparator = TokenizerHelper.GetNumericListSeparator(provider);
        return string.Format(provider, "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}", numericListSeparator, _x, _y, _width, _height);
    }

    public readonly bool Equals(Rect3 value)
    {
        return this == value;
    }

    public static bool operator ==(Rect3 rect1, Rect3 rect2)
    {
        if (rect1._x == rect2._x && rect1._y == rect2._y && rect1._width == rect2._width)
        {
            return rect1._height == rect2._height;
        }

        return false;
    }

    public static bool operator !=(Rect3 rect1, Rect3 rect2)
    {
        return !(rect1 == rect2);
    }

    public readonly override bool Equals(object? o)
    {
        if (o is Rect3 rect)
        {
            return this == rect;
        }

        return false;
    }

    public readonly override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
    }

    internal static class GSR
    {
        public static string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";
    }
}
