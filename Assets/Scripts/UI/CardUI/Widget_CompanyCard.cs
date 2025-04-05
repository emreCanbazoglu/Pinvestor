using UnityEngine;
using UnityEngine.EventSystems;

namespace Pinvestor.UI
{
    public class Widget_CompanyCard : MonoBehaviour
    {
        [field: SerializeField] public EventTrigger ButtonEventTrigger { get; private set; } = null;
    }
}
