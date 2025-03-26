using MMFramework.CameraSystem;
using UnityEngine;

namespace Pinvestor.CameraSystem
{
    public enum ECamera
    {
        None = 0,
        Main = 1,
    }
    
    [CreateAssetMenu(
        menuName = "Pinvestor/Camera System/Camera Type",
        fileName = "CameraType")]
    public class CameraTypeSO : CameraTypeSOBase
    {
        [SerializeField] private ECamera _cameraType = default;

        public override System.Enum GetID()
        {
            return _cameraType;
        }
    }
}
