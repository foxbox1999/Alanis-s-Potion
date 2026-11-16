
using UnityEngine;

public class DistanceChecker : MonoBehaviour
{
    public Transform target;  

    // Start is called before the first frame update
    public bool CheckDistance(float distance)
    {
        float distance2D = Vector2.Distance(transform.position, target.position);
        if (distance2D < distance)
        {
            return true;
        }
        return false;
    }
}
