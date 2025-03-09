#if UNITY_EDITOR
using System;

namespace EditorReplacement
{
    public struct EditorCreationScope : IDisposable
    {
        private readonly bool prevOriginal;
        
        public EditorCreationScope(bool original)
        {
            prevOriginal = ERManager.IsOriginal();
            if (original != prevOriginal)
            {
                if (original)
                    ERManager.SetOriginal();
                else
                    ERManager.SetReplaced();
            }
        }

        public void Dispose()
        {
            if (prevOriginal)
                ERManager.SetOriginal();
            else
                ERManager.SetReplaced();
        }
    }
}
#endif
