namespace Pinvestor.Game
{
    public class Stage
    {
        public int StageIndex { get; private set; } = 0;
        public StageSettings Settings { get; private set; } = null;
        
        public Stage(
            int stageIndex,
            StageSettings settings)
        {
            StageIndex = stageIndex;
            Settings = settings;
        }
    }
}