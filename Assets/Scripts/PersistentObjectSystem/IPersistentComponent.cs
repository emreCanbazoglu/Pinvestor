using System;
using Boomlagoon.JSON;
using Cysharp.Threading.Tasks;

namespace MMFramework.PersistentObjectSystem
{
    public interface IPersistentComponent
    {
        PersistentObject PersistentObject { get; set; }
        
        Action<IPersistentComponent, bool> OnUpdate { get; set; }
        Action<IPersistentComponent> OnReset { get; set; }

        JSONObject Serialize();
        
        UniTask InitializeAsync(JSONObject data);
    }
}