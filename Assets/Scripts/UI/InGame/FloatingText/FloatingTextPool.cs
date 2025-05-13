using UnityEngine;
using UnityEngine.Pool;

namespace Pinvestor.UI
{
    
    public class FloatingTextPool : Singleton<FloatingTextPool>
    {
        [SerializeField] private Widget_FloatingText _widgetPrefab = null;
        
        [SerializeField] private bool _collectionCheck = true;
        [SerializeField] private int _defaultCapacity = 0;
        [SerializeField] private int _maxSize = 10;
        
        public IObjectPool<Widget_FloatingText> Pool { get; private set; }
        
        protected override void AwakeCore()
        {
            Pool = new ObjectPool<Widget_FloatingText>(
                CreateWidget,
                OnGetFromPool,
                OnReleaseToPool,
                OnDestroyPooledObject,
                _collectionCheck,
                _defaultCapacity,
                _maxSize);
        
            base.AwakeCore();
        }
    
        private void OnGetFromPool(Widget_FloatingText obj)
        {
            obj.gameObject.SetActive(true);
        }
    
        private void OnReleaseToPool(Widget_FloatingText obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(transform);
        }

        private void OnDestroyPooledObject(Widget_FloatingText obj)
        {
            if(obj == null)
                return;
            
            Destroy(obj.gameObject);
        }

        private Widget_FloatingText CreateWidget()
        {
            Widget_FloatingText widgetInstance = Instantiate(_widgetPrefab, transform);
            widgetInstance.transform.localPosition = Vector3.zero;
            widgetInstance.ObjectPool = Pool;

            return widgetInstance;
        }
    }
}