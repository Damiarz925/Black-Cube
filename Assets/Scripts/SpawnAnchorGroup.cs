// Developer map: Serialized world transforms for legacy 3D scenery categories. LevelGenerator samples these anchors; they do not control combat enemy spawns.
// See Docs/DEVELOPER_HANDOFF.md for system flow and validation.
using System.Collections.Generic;
using UnityEngine;

public class SpawnAnchorGroup : MonoBehaviour
{
    public List<Transform> ForegroundTreeAnchors;
    public List<Transform> MidgroundTreeAnchors;
    public List<Transform> BackgroundTreeAnchors;
    public List<Transform> ForegroundRockAnchors;
    public List<Transform> MidgroundRockAnchors;
    public List<Transform> CameraRockAnchors;
    public List<Transform> ForegroundTerrainAnchors;
    public List<Transform> BackgroundTerrainAnchors;
}
