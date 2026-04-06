using UnityEngine;

public class Respawn : MonoBehaviour
{

    public Vector3 SpawnPoint;
    void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Player")
        {
            col.transform.position = SpawnPoint;
        }
    }


}
