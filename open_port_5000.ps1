# Windows 防火墙开放端口 5000 的 PowerShell 脚本
# 需要以管理员权限运行

Write-Host "正在为 MomShares Web Server 开放端口 5000..." -ForegroundColor Green

# 创建入站规则（允许 TCP 5000 端口）
New-NetFirewallRule -DisplayName "MomShares Web Server (Port 5000)" `
    -Direction Inbound `
    -LocalPort 5000 `
    -Protocol TCP `
    -Action Allow `
    -Profile Domain,Private,Public

if ($?) {
    Write-Host "端口 5000 已成功开放！" -ForegroundColor Green
    Write-Host "规则名称: MomShares Web Server (Port 5000)" -ForegroundColor Yellow
} else {
    Write-Host "操作失败，请确保以管理员权限运行此脚本！" -ForegroundColor Red
}

# 显示当前规则
Write-Host "`n当前端口 5000 的防火墙规则：" -ForegroundColor Cyan
Get-NetFirewallRule -DisplayName "*5000*" | Format-Table DisplayName, Enabled, Direction, Action -AutoSize

