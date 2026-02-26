namespace Pinvestor.GameConfigSystem
{
    public readonly struct GameConfigLoadedEvent : IEvent
    {
        public readonly int SchemaVersion;

        public GameConfigLoadedEvent(int schemaVersion)
        {
            SchemaVersion = schemaVersion;
        }
    }

    public readonly struct GameConfigLoadFailedEvent : IEvent
    {
        public readonly string Error;

        public GameConfigLoadFailedEvent(string error)
        {
            Error = error;
        }
    }
}

