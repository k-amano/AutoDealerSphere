namespace AutoDealerSphere.Server.Exceptions;

/// <summary>
/// メッセージをそのままユーザーに表示してよい業務エラー。
/// ExceptionHandlingMiddleware が一括で 400 + メッセージに変換する。
/// 個別の try/catch は書かず、この例外を throw するだけにすること。
/// </summary>
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }
}
