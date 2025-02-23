using UnityEngine;

namespace Pinvestor.Game
{
    public class GameManager : Singleton<GameManager>
    {
        [field: SerializeField] public GameFSM GameFsm { get; private set; } = null;

    }
}
