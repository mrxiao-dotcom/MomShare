// 期货工具箱JavaScript
let currentTool = 'risk';

// 工具切换
function showTool(toolName) {
    // 更新按钮状态
    document.querySelectorAll('.tool-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    event.target.classList.add('active');

    // 隐藏所有工具内容
    document.querySelectorAll('.tool-content').forEach(content => {
        content.style.display = 'none';
    });

    // 显示选中的工具
    document.getElementById(toolName + '-calculator').style.display = 'block';
    document.getElementById(toolName + '-tool').style.display = 'block';

    currentTool = toolName;
}

// 计算风险金
function calculateRisk() {
    // 获取输入值
    const initialEquity = parseFloat(document.getElementById('initialEquity').value);
    const currentEquity = parseFloat(document.getElementById('currentEquity').value);
    let initialNetValue = parseFloat(document.getElementById('initialNetValue').value);
    const defenseNetValue = parseFloat(document.getElementById('defenseNetValue').value);
    const targetNetValue = parseFloat(document.getElementById('targetNetValue').value);
    const warningNetValue = parseFloat(document.getElementById('warningNetValue').value);
    const riskLots = parseInt(document.getElementById('riskLots').value);

    // 校验输入
    if (!initialEquity || initialEquity <= 0) {
        alert('请输入有效的账户初始权益');
        return;
    }

    if (!currentEquity || currentEquity <= 0) {
        alert('请输入有效的当前权益');
        return;
    }

    if (!defenseNetValue || defenseNetValue <= 0) {
        alert('请输入有效的防御净值');
        return;
    }

    if (!targetNetValue || targetNetValue <= 0) {
        alert('请输入有效的目标净值');
        return;
    }

    if (!warningNetValue || warningNetValue <= 0) {
        alert('请输入有效的预警净值');
        return;
    }

    if (!riskLots || riskLots <= 0) {
        alert('请输入有效的风险金笔数');
        return;
    }

    // 校验逻辑关系
    if (warningNetValue <= defenseNetValue) {
        alert('预警净值必须大于防御净值');
        return;
    }

    if (initialNetValue <= 0) {
        initialNetValue = 1.0; // 默认初始净值为1
    }

    // 计算当前净值
    const currentNetValue = currentEquity / initialEquity;

    // 更新当前净值显示
    document.getElementById('currentNetValue').value = currentNetValue.toFixed(3);

    // 计算建议单笔和防御单笔
    const suggestedLot = (currentEquity - initialEquity * warningNetValue) / riskLots;
    const minEquity = Math.min(currentEquity, warningNetValue * initialEquity);
    const defenseLot = (minEquity - defenseNetValue * initialEquity) / riskLots;

    // 显示结果
    document.getElementById('suggestedLot').textContent = suggestedLot.toFixed(2) + ' 元';
    document.getElementById('defenseLot').textContent = defenseLot.toFixed(2) + ' 元';
    document.getElementById('result-section').style.display = 'block';

    // 绘制图表
    drawRiskChart(initialNetValue, currentNetValue, defenseNetValue, targetNetValue, warningNetValue, riskLots);
}

// 清空输入
function clearInputs() {
    document.getElementById('initialEquity').value = '';
    document.getElementById('currentEquity').value = '';
    document.getElementById('initialNetValue').value = '1.000';
    document.getElementById('currentNetValue').value = '';
    document.getElementById('defenseNetValue').value = '';
    document.getElementById('targetNetValue').value = '';
    document.getElementById('warningNetValue').value = '';
    document.getElementById('riskLots').value = '';
    document.getElementById('result-section').style.display = 'none';

    // 清空图表
    const canvas = document.getElementById('riskChart');
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}

// 绘制风险图表
function drawRiskChart(initialNetValue, currentNetValue, defenseNetValue, targetNetValue, warningNetValue, riskLots) {
    const canvas = document.getElementById('riskChart');
    const ctx = canvas.getContext('2d');

    // 清空画布
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // 设置画布尺寸
    const width = canvas.width;
    const height = canvas.height;
    const padding = 60;

    // 计算净值范围
    const minNetValue = Math.min(defenseNetValue, initialNetValue) * 0.95;
    const maxNetValue = Math.max(targetNetValue, currentNetValue) * 1.05;
    const netValueRange = maxNetValue - minNetValue;

    // 绘制坐标轴
    ctx.strokeStyle = '#333';
    ctx.lineWidth = 2;

    // Y轴（净值）
    ctx.beginPath();
    ctx.moveTo(padding, padding);
    ctx.lineTo(padding, height - padding);
    ctx.stroke();

    // X轴（风险金）
    ctx.beginPath();
    ctx.moveTo(padding, height - padding);
    ctx.lineTo(width - padding, height - padding);
    ctx.stroke();

    // Y轴刻度标签
    ctx.fillStyle = '#666';
    ctx.font = '12px Arial';
    ctx.textAlign = 'right';
    const yTicks = 10;
    for (let i = 0; i <= yTicks; i++) {
        const y = padding + (height - 2 * padding) * (yTicks - i) / yTicks;
        const netValue = minNetValue + (netValueRange * i) / yTicks;
        ctx.fillText(netValue.toFixed(3), padding - 10, y + 4);
    }

    // X轴刻度标签
    ctx.textAlign = 'center';
    const xTicks = riskLots;
    for (let i = 0; i <= xTicks; i++) {
        const x = padding + (width - 2 * padding) * i / xTicks;
        ctx.fillText(i.toString(), x, height - padding + 20);
    }

    // 轴标签
    ctx.fillStyle = '#333';
    ctx.font = '14px Arial';
    ctx.textAlign = 'center';
    ctx.fillText('风险金笔数', width / 2, height - 10);
    ctx.save();
    ctx.translate(20, height / 2);
    ctx.rotate(-Math.PI / 2);
    ctx.fillText('净值', 0, 0);
    ctx.restore();

    // 绘制参考线
    const getY = (netValue) => padding + (height - 2 * padding) * (maxNetValue - netValue) / netValueRange;

    // 目标净值线（红色加粗）
    ctx.strokeStyle = '#ff4444';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(padding, getY(targetNetValue));
    ctx.lineTo(width - padding, getY(targetNetValue));
    ctx.stroke();

    // 当前净值线（蓝色加粗）
    ctx.strokeStyle = '#4444ff';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(padding, getY(currentNetValue));
    ctx.lineTo(width - padding, getY(currentNetValue));
    ctx.stroke();

    // 预警净值线（黄色）
    ctx.strokeStyle = '#ffaa00';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(padding, getY(warningNetValue));
    ctx.lineTo(width - padding, getY(warningNetValue));
    ctx.stroke();

    // 防御净值线（黑色加粗）
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(padding, getY(defenseNetValue));
    ctx.lineTo(width - padding, getY(defenseNetValue));
    ctx.stroke();

    // 初始净值线（灰色虚线）
    ctx.strokeStyle = '#999999';
    ctx.lineWidth = 1;
    ctx.setLineDash([5, 5]);
    ctx.beginPath();
    ctx.moveTo(padding, getY(initialNetValue));
    ctx.lineTo(width - padding, getY(initialNetValue));
    ctx.stroke();
    ctx.setLineDash([]);

    // 绘制方块
    const currentY = getY(currentNetValue);
    const warningY = getY(warningNetValue);
    const defenseY = getY(defenseNetValue);

    // 绿色方块（当前值到预警值之间）
    if (currentY > warningY) {
        drawBlocks(ctx, padding, currentY, warningY, riskLots, '#44aa44', 8);
    }

    // 浅红色方块（预警值到防御值之间）
    if (warningY > defenseY) {
        drawBlocks(ctx, padding, warningY, defenseY, riskLots, '#ff8888', 6);
    }

    // 图例
    drawLegend(ctx, width - 150, padding + 20);
}

// 绘制方块
function drawBlocks(ctx, startX, startY, endY, count, color, size) {
    const width = ctx.canvas.width;
    const padding = 60;
    const availableWidth = width - 2 * padding;
    const spacing = availableWidth / (count + 1);

    for (let i = 1; i <= count; i++) {
        const x = startX + spacing * i - size / 2;
        const y = startY + (endY - startY) * Math.random() * 0.8 - size / 2; // 随机分布在区域内

        ctx.fillStyle = color;
        ctx.fillRect(x, y, size, size);
    }
}

// 绘制图例
function drawLegend(ctx, x, y) {
    const lineHeight = 25;
    let currentY = y;

    // 目标净值
    ctx.strokeStyle = '#ff4444';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(x, currentY);
    ctx.lineTo(x + 30, currentY);
    ctx.stroke();
    ctx.fillStyle = '#333';
    ctx.font = '12px Arial';
    ctx.textAlign = 'left';
    ctx.fillText('目标净值', x + 40, currentY + 4);

    // 当前净值
    currentY += lineHeight;
    ctx.strokeStyle = '#4444ff';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(x, currentY);
    ctx.lineTo(x + 30, currentY);
    ctx.stroke();
    ctx.fillText('当前净值', x + 40, currentY + 4);

    // 预警净值
    currentY += lineHeight;
    ctx.strokeStyle = '#ffaa00';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(x, currentY);
    ctx.lineTo(x + 30, currentY);
    ctx.stroke();
    ctx.fillText('预警净值', x + 40, currentY + 4);

    // 防御净值
    currentY += lineHeight;
    ctx.strokeStyle = '#000000';
    ctx.lineWidth = 3;
    ctx.beginPath();
    ctx.moveTo(x, currentY);
    ctx.lineTo(x + 30, currentY);
    ctx.stroke();
    ctx.fillText('防御净值', x + 40, currentY + 4);

    // 建议风险金
    currentY += lineHeight + 10;
    ctx.fillStyle = '#44aa44';
    ctx.fillRect(x, currentY - 6, 12, 12);
    ctx.fillStyle = '#333';
    ctx.fillText('建议风险金', x + 20, currentY + 4);

    // 防御风险金
    currentY += lineHeight;
    ctx.fillStyle = '#ff8888';
    ctx.fillRect(x, currentY - 6, 12, 12);
    ctx.fillStyle = '#333';
    ctx.fillText('防御风险金', x + 20, currentY + 4);
}

// 页面加载初始化
document.addEventListener('DOMContentLoaded', function() {
    showTool('risk');
});


