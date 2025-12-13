using MomShares.Core.Enums;

namespace MomShares.Core.Entities;

/// <summary>
/// 操作日志实体
/// </summary>
public class OperationLog
{
    public int Id { get; set; }
    
    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime OperationTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 操作人类型
    /// </summary>
    public OperatorType OperatorType { get; set; }
    
    /// <summary>
    /// 操作人ID
    /// </summary>
    public int OperatorId { get; set; }
    
    /// <summary>
    /// 操作类型
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作详情（JSON格式）
    /// </summary>
    public string? OperationDetails { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

