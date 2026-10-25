public static class BitDirectionExtensions
{
    public static BitDirection Rotate(this BitDirection direction, int count)
    {
        count = (count % 4 + 4) % 4;
        int num = (int)direction << count;
        int num2 = num & 0xF;
        int num3 = num & 0xF0;
        return (BitDirection)(num2 | (num3 >> 4));
    }

    public static BitDirection Reverse(this BitDirection direction)
    {
        return (BitDirection)(((int)(direction & (BitDirection)3) << 2) | ((int)(direction & (BitDirection)12) >> 2));
    }

    public static int BitCount(this BitDirection direction)
    {
        int num = (int)direction;
        int num2 = 0;
        while (num != 0)
        {
            num2++;
            num &= num - 1;
        }

        return num2;
    }

    public static BasePart.GridRotation ToGridRotation(this BitDirection direction)
    {
        return ((direction == BitDirection.Right) ? BasePart.GridRotation.Deg_0 : (direction == BitDirection.Up) ? BasePart.GridRotation.Deg_90 : (direction == BitDirection.Left) ? BasePart.GridRotation.Deg_180 : (direction == BitDirection.Down) ? BasePart.GridRotation.Deg_270 : BasePart.GridRotation.Deg_0);
    }
}