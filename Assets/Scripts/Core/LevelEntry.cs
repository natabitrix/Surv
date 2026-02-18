namespace Assets.Scripts.Core
{
    [System.Serializable]
    public class LevelEntry
    {
        public float xpToNext;
        public int engramPoints;
    }

    [System.Serializable]
    public class LevelTableData
    {
        public LevelEntry[] levels;
    }
}