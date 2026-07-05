/// <summary>
/// エレベーターの稼働状態を外部に公開するインターフェース。
/// 将来のElevatorMovementSystemがこれを実装する。
/// </summary>
public interface IElevatorStatus
{
    /// <summary>エレベーターが移動中かどうか</summary>
    bool IsMoving { get; }
}