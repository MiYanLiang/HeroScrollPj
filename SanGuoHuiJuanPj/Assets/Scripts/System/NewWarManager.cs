using Assets.System.WarModule;
using UnityEngine;

public class NewWarManager : MonoBehaviour
{
    #region UnityFields

    public ChessPos[] PlayerPoses;
    public ChessPos[] EnemyPoses;

    #endregion

    public ChessGrid<FightCardData> Grid;
    public ChessOperatorManager ChessOperator;
    void Start()
    {
        Grid = new ChessGrid<FightCardData>(PlayerPoses, EnemyPoses);
        ChessOperator = new ChessOperatorManager(true, Grid);
    }
}