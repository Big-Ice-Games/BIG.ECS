using System;
using System.Runtime.CompilerServices;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace TestLibrary.Types;

public struct Vector2
{
    public static Vector2 Zero = new Vector2();
    public static Vector2 One = new Vector2(1, 1);
    public float X;
    public float Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    #region Utils
    public override string ToString() => $"{X:F}:{Y:F}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Normalized()
    {
        float magnitude = MathF.Sqrt(X * X + Y * Y);
        return new Vector2(X / magnitude, Y / magnitude);
    }

    public bool IsZero => X == 0 && Y == 0;

    #endregion
    #region Equals
    public bool Equals(Vector2 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector2 other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return X.GetHashCode() * 397 ^ Y.GetHashCode();
        }
    }
    #endregion
    #region Operators
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator -(Vector2 vec1, Vector2 vec2)
    {
        return new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator +(Vector2 vec1, Vector2 vec2)
    {
        return new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Vector2 vec1, Vector2 vec2)
    {
        return new Vector2(vec1.X * vec2.X, vec1.Y * vec2.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Vector2 vec1, float value)
    {
        return new Vector2(vec1.X * value, vec1.Y * value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator /(Vector2 vec1, float value)
    {
        if (value == 0)
        {
            throw new DivideByZeroException();
        }
        return new Vector2(vec1.X / value, vec1.Y / value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector2 vec1, Vector2 vec2)
    {
        return vec1.X == vec2.X && vec1.Y == vec2.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector2 vec1, Vector2 vec2)
    {
        return !(vec1 == vec2);
    }
    #endregion
}