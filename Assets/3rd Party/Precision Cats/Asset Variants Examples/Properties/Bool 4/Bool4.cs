using UnityEngine;
using System;

[Serializable]
public struct Bool4
{
    public bool x, y, z, w;

    public Color Invert(Color c)
    {
        if (x)
            c.r = 1 - c.r;
        if (y)
            c.g = 1 - c.g;
        if (z)
            c.b = 1 - c.b;
        if (w)
            c.a = 1 - c.a;
        return c;
    }

    public static implicit operator Bool4(bool b)
    {
        return new Bool4(b);
    }
    public Bool4(bool b)
    {
        x = y = z = w = b;
    }
}

[Serializable]
public struct Bool3
{
    public bool x, y, z;

    public Color Invert(Color c)
    {
        if (x)
            c.r = 1 - c.r;
        if (y)
            c.g = 1 - c.g;
        if (z)
            c.b = 1 - c.b;
        return c;
    }

    public static implicit operator Bool3(bool b)
    {
        return new Bool3(b);
    }
    public Bool3(bool b)
    {
        x = y = z = b;
    }
}