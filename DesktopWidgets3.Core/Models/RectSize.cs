namespace DesktopWidgets3.Core.Models;

/// <summary>
/// Represents the size of a rectangle.
/// Codes are edited from: <see cref="Windows.Foundation.Size">.
/// </summary>
public struct RectSize
{
    public static RectSize NULL => new(null, null);

    private float? _width;

    private float? _height;

    public double? Width
    {
        readonly get => _width;
        set
        {
            if (value != null && value < 0.0)
            {
                throw new ArgumentOutOfRangeException("Width", GSR.ArgumentOutOfRange_NeedNonNegNum);
            }

            _width = (float?)value;
        }
    }

    public double? Height
    {
        readonly get => _height;
        set
        {
            if (value != null && value < 0.0)
            {
                throw new ArgumentOutOfRangeException("Height", GSR.ArgumentOutOfRange_NeedNonNegNum);
            }

            _height = (float?)value;
        }
    }

    public RectSize(float? width, float? height)
    {
        if (width != null && width < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width), GSR.ArgumentOutOfRange_NeedNonNegNum);
        }

        if (height != null && height < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(height), GSR.ArgumentOutOfRange_NeedNonNegNum);
        }

        _width = width;
        _height = height;
    }

    private readonly bool IsEmpty => Width == null && Height == null;

    public RectSize(double? width, double? height)
        : this((float?)width, (float?)height)
    {
    }

    public RectSize(Windows.Foundation.Rect size)
        : this(size.Width, size.Height)
    {
    }

    public RectSize(Windows.Foundation.Size size)
        : this (size.Width, size.Height)
    {

    }

    public static RectSize operator +(RectSize size1, RectSize size2)
    {
        return new RectSize(size1.Width + size2.Width, size1.Height + size2.Height);
    }

    public static RectSize operator -(RectSize size1, RectSize size2)
    {
        return new RectSize(size1.Width - size2.Width, size1.Height - size2.Height);
    }

    public static bool operator ==(RectSize size1, RectSize size2)
    {
        if (size1._width == size2._width)
        {
            return size1._height == size2._height;
        }

        return false;
    }

    public static bool operator !=(RectSize size1, RectSize size2)
    {
        return !(size1 == size2);
    }

    public readonly override bool Equals(object? o)
    {
        if (o is RectSize size)
        {
            return Equals(this, size);
        }

        return false;
    }

    public readonly bool Equals(RectSize value)
    {
        return Equals(this, value);
    }

    public readonly override int GetHashCode()
    {
        if (_width == null && _height == null)
        {
            return 0;
        }

        return Width.GetHashCode() ^ Height.GetHashCode();
    }

    private static bool Equals(RectSize size1, RectSize size2)
    {
        if (size1.IsEmpty)
        {
            return size2.IsEmpty;
        }

        if (size1._width.Equals(size2._width))
        {
            return size1._height.Equals(size2._height);
        }

        return false;
    }

    public readonly override string ToString()
    {
        if (IsEmpty)
        {
            return "Empty";
        }

        return $"{_width},{_height}";
    }

    internal static class GSR
    {
        public static string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";
    }
}
