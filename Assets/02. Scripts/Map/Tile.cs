using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector3 TileSize
    {
        get
        {
            return gameObject.GetComponent<Renderer>().bounds.size;
        }
    }

    [SerializeField] private float speed;

    
}
