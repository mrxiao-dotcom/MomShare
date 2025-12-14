# 服务器连接诊断脚本
# 以管理员身份运行此脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  服务器连接诊断" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. 检查端口监听状态
Write-Host "`n[1] 检查端口 5000 监听状态:" -ForegroundColor Yellow
$listening = netstat -ano | findstr ":5000" | findstr "LISTENING"
if ($listening) {
    Write-Host "✓ 端口 5000 正在监听" -ForegroundColor Green
    $listening | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host "✗ 端口 5000 未监听" -ForegroundColor Red
}

# 2. 检查防火墙规则
Write-Host "`n[2] 检查防火墙规则:" -ForegroundColor Yellow
$firewallRules = Get-NetFirewallRule -DisplayName "*5000*" -ErrorAction SilentlyContinue
if ($firewallRules) {
    Write-Host "✓ 找到防火墙规则:" -ForegroundColor Green
    $firewallRules | ForEach-Object {
        $rule = $_
        $filters = Get-NetFirewallPortFilter -AssociatedNetFirewallRule $rule
        Write-Host "  规则名称: $($rule.DisplayName)" -ForegroundColor White
        Write-Host "  状态: $($rule.Enabled)" -ForegroundColor $(if ($rule.Enabled -eq $true) { "Green" } else { "Red" })
        Write-Host "  方向: $($rule.Direction)" -ForegroundColor White
        Write-Host "  操作: $($rule.Action)" -ForegroundColor White
        if ($filters.LocalPort) {
            Write-Host "  端口: $($filters.LocalPort)" -ForegroundColor White
        }
        Write-Host ""
    }
} else {
    Write-Host "✗ 未找到端口 5000 的防火墙规则" -ForegroundColor Red
    Write-Host "  需要创建防火墙规则" -ForegroundColor Yellow
}

# 3. 检查本机IP地址
Write-Host "[3] 检查本机IP地址:" -ForegroundColor Yellow
$ipAddresses = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" }
if ($ipAddresses) {
    Write-Host "✓ 本机IP地址:" -ForegroundColor Green
    $ipAddresses | ForEach-Object {
        Write-Host "  $($_.IPAddress) (接口: $($_.InterfaceAlias))" -ForegroundColor White
    }
} else {
    Write-Host "✗ 未找到有效的IP地址" -ForegroundColor Red
}

# 4. 测试本地连接
Write-Host "`n[4] 测试本地连接:" -ForegroundColor Yellow
try {
    $test = Test-NetConnection -ComputerName localhost -Port 5000 -WarningAction SilentlyContinue
    if ($test.TcpTestSucceeded) {
        Write-Host "✓ 本地连接成功" -ForegroundColor Green
    } else {
        Write-Host "✗ 本地连接失败" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ 测试失败: $_" -ForegroundColor Red
}

# 5. 检查是否有其他防火墙软件
Write-Host "`n[5] 检查Windows Defender防火墙状态:" -ForegroundColor Yellow
$firewallProfiles = Get-NetFirewallProfile
$firewallProfiles | ForEach-Object {
    $status = if ($_.Enabled) { "启用" } else { "禁用" }
    $color = if ($_.Enabled) { "Yellow" } else { "Green" }
    Write-Host "  $($_.Name): $status" -ForegroundColor $color
}

# 6. 检查路由表（确认是否有NAT）
Write-Host "`n[6] 检查默认网关:" -ForegroundColor Yellow
$defaultGateway = Get-NetRoute -DestinationPrefix "0.0.0.0/0" | Where-Object { $_.NextHop -ne "0.0.0.0" } | Select-Object -First 1
if ($defaultGateway) {
    Write-Host "✓ 默认网关: $($defaultGateway.NextHop)" -ForegroundColor Green
    Write-Host "  接口: $($defaultGateway.InterfaceAlias)" -ForegroundColor White
} else {
    Write-Host "✗ 未找到默认网关" -ForegroundColor Red
}

# 7. 提供建议
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  诊断完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n建议操作:" -ForegroundColor Yellow
Write-Host "1. 如果防火墙规则不存在或未启用，运行以下命令创建规则:" -ForegroundColor White
Write-Host "   New-NetFirewallRule -DisplayName 'MomShares Web Server (Port 5000)' -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow -Profile Domain,Private,Public" -ForegroundColor Cyan

Write-Host "`n2. 如果是云服务器，请在云控制台配置安全组:" -ForegroundColor White
Write-Host "   - 添加入站规则: 端口 5000, 协议 TCP, 来源 0.0.0.0/0" -ForegroundColor Cyan

Write-Host "`n3. 确认公网IP和内网IP的映射关系:" -ForegroundColor White
Write-Host "   - 公网IP: 82.157.28.78" -ForegroundColor Cyan
Write-Host "   - 内网IP: 172.21.0.4" -ForegroundColor Cyan
Write-Host "   - 如果使用NAT，确保端口映射正确" -ForegroundColor Cyan

