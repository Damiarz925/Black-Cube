// Developer map: Stores the pool key assigned by Pool.Get so Release returns scenery to the correct stack.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public string Key;
}
