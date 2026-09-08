using UnityEngine;

public static class CabinOccupancy
{
    public static bool FullyContains(BoxCollider cabin, CharacterController passenger)
    {
        if (cabin == null || passenger == null || !passenger.enabled) return false;
        Bounds body = passenger.bounds;
        Bounds space = cabin.bounds;
        return body.min.x >= space.min.x && body.max.x <= space.max.x
            && body.min.z >= space.min.z && body.max.z <= space.max.z
            && space.Contains(body.center);
    }
}
