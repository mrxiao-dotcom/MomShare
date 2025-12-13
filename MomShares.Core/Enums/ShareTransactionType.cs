namespace MomShares.Core.Enums;

/// <summary>
/// 份额操作类型
/// </summary>
public enum ShareTransactionType
{
    /// <summary>
    /// 初始出资
    /// </summary>
    InitialInvestment = 1,
    
    /// <summary>
    /// 转让
    /// </summary>
    Transfer = 2,
    
    /// <summary>
    /// 新增份额
    /// </summary>
    Add = 3,
    
    /// <summary>
    /// 减持份额
    /// </summary>
    Reduce = 4
}

