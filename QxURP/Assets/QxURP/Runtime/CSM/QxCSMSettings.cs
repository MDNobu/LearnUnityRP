
[System.Serializable]
public class QxCSMSettings
{
    public float maxDistance = 200.0f;
    public bool usingShadowMask = false;
    public QxShadowSettings level0;
    public QxShadowSettings level1;
    public QxShadowSettings level2;
    public QxShadowSettings level3;

    public void Set()
    {
        QxShadowSettings[] levels =
        {
            level0, level1, level2, level3
        };

        for (int i = 0; i < 4; i++)
        {
            
        }
    }
}