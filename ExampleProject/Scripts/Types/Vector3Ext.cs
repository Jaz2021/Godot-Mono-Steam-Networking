using Godot;

public static class Vector3Ext
{
    private static float GetLengthSq(Vector3 vec)
    {
        return (vec.X * vec.X) + (vec.Z * vec.Z);
    }
    public static Vector3 Clamp(this Vector3 vec, float maxLength)
    {
        if(GetLengthSq(vec) > maxLength * maxLength)
        {
            Vector2 v = new(vec.X, vec.Z);
            v = v.Normalized() * maxLength;
            vec = new(v.X, vec.Y, v.Y);
            return vec;
        } else
        {
            return vec;
        }
    }
}