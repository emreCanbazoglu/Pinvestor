using System.Collections.Generic;

namespace MMFramework.MMUI
{
    public class WorldInputBlockerCanvasController : Singleton<WorldInputBlockerCanvasController>
    {
        private Dictionary<WorldInputBlockerCanvas, List<int>> _worldInputBlockerMap =
            new Dictionary<WorldInputBlockerCanvas, List<int>>();

        public bool IsInputBlockedByUICanvas(
            int fingerIndex)
        {
            foreach (var indexLists in _worldInputBlockerMap.Values)
            {
                if (indexLists.Contains(fingerIndex))
                    return true;
            }

            return false;
        }

        public void RegisterBlockRequest(
            WorldInputBlockerCanvas worldInputBlockerCanvas,
            int fingerIndex)
        {
            _worldInputBlockerMap.TryAdd(
                worldInputBlockerCanvas, 
                new List<int>());

            if(_worldInputBlockerMap[worldInputBlockerCanvas]
               .Contains(fingerIndex))
                return;
            
            _worldInputBlockerMap[worldInputBlockerCanvas].Add(fingerIndex);
        }
        
        public void UnregisterBlockRequest(
            WorldInputBlockerCanvas worldInputBlockerCanvas,
            int fingerIndex)
        {
            if (!_worldInputBlockerMap.ContainsKey(worldInputBlockerCanvas))
                return;

            if (!_worldInputBlockerMap[worldInputBlockerCanvas]
                    .Contains(fingerIndex))
                return;
            
            _worldInputBlockerMap[worldInputBlockerCanvas].Remove(fingerIndex);
        }

        public void UnregisterBlockRequest(
            WorldInputBlockerCanvas worldInputBlockerCanvas)
        {
            _worldInputBlockerMap.Remove(worldInputBlockerCanvas);
        }
    }
}