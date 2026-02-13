// investmentCharts.js
// Funções para renderizar gráficos de investimentos usando Chart.js

// Configuração de cores padrão
const CHART_COLORS = {
    stock: 'rgba(54, 162, 235, 0.8)',
    fixedIncome: 'rgba(75, 192, 192, 0.8)',
    realEstate: 'rgba(255, 159, 64, 0.8)',
    crypto: 'rgba(153, 102, 255, 0.8)',
    fund: 'rgba(255, 206, 86, 0.8)',
    etf: 'rgba(255, 99, 132, 0.8)',
    other: 'rgba(201, 203, 207, 0.8)'
};

// Mapeamento de tipos de ativos para cores
function getAssetTypeColor(assetType) {
    const colorMap = {
        0: CHART_COLORS.stock,        // Stock
        1: CHART_COLORS.fixedIncome,  // FixedIncome
        2: CHART_COLORS.realEstate,   // RealEstate
        3: CHART_COLORS.crypto,        // Crypto
        4: CHART_COLORS.fund,          // Fund
        5: CHART_COLORS.etf,           // ETF
        6: CHART_COLORS.other          // Other
    };
    return colorMap[assetType] || CHART_COLORS.other;
}

// Mapeamento de nomes de tipos de ativos
function getAssetTypeName(assetType) {
    const nameMap = {
        0: 'Ações',
        1: 'Renda Fixa',
        2: 'Fundos Imobiliários',
        3: 'Criptomoedas',
        4: 'Fundos de Investimento',
        5: 'ETFs',
        6: 'Outros'
    };
    return nameMap[assetType] || 'Outros';
}

/**
 * Renderiza gráfico de pizza - Diversificação por Tipo
 * @param {string} canvasId - ID do elemento canvas
 * @param {Object} data - Dados agrupados por tipo { assetType: value }
 */
window.renderDiversificationByType = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;

    // Destruir gráfico existente se houver
    if (ctx.chart) {
        ctx.chart.destroy();
    }

    const labels = [];
    const values = [];
    const colors = [];

    for (const [assetType, value] of Object.entries(data)) {
        labels.push(getAssetTypeName(parseInt(assetType)));
        values.push(value);
        colors.push(getAssetTypeColor(parseInt(assetType)));
    }

    const chart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: colors,
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 15,
                        font: {
                            size: 12
                        }
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const label = context.label || '';
                            const value = context.parsed || 0;
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((value / total) * 100).toFixed(1);
                            return `${label}: R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: 2 })} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });

    ctx.chart = chart;
    return chart;
};

/**
 * Renderiza gráfico de barras - Top 10 Ativos por Valor
 * @param {string} canvasId - ID do elemento canvas
 * @param {Array} assets - Array de objetos { name, value, assetType }
 */
window.renderDiversificationByAsset = function(canvasId, assets) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;

    if (ctx.chart) {
        ctx.chart.destroy();
    }

    // Ordenar e pegar top 10
    const topAssets = assets
        .sort((a, b) => b.value - a.value)
        .slice(0, 10);

    const labels = topAssets.map(a => a.name);
    const values = topAssets.map(a => a.value);
    const colors = topAssets.map(a => getAssetTypeColor(a.assetType));

    const chart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Valor Atual (R$)',
                data: values,
                backgroundColor: colors,
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            indexAxis: 'y',
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `R$ ${context.parsed.x.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return 'R$ ' + value.toLocaleString('pt-BR');
                        }
                    }
                }
            }
        }
    });

    ctx.chart = chart;
    return chart;
};

/**
 * Renderiza gráfico de linha - Evolução Patrimonial
 * @param {string} canvasId - ID do elemento canvas
 * @param {Array} data - Array de objetos { date, value }
 */
window.renderEvolutionChart = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;

    if (ctx.chart) {
        ctx.chart.destroy();
    }

    const sortedData = data.sort((a, b) => new Date(a.date) - new Date(b.date));
    const labels = sortedData.map(d => {
        const date = new Date(d.date);
        return date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' });
    });
    const values = sortedData.map(d => d.value);

    const chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Valor Total',
                data: values,
                borderColor: 'rgba(54, 162, 235, 1)',
                backgroundColor: 'rgba(54, 162, 235, 0.1)',
                borderWidth: 3,
                fill: true,
                tension: 0.4,
                pointRadius: 4,
                pointHoverRadius: 6,
                pointBackgroundColor: 'rgba(54, 162, 235, 1)',
                pointBorderColor: '#fff',
                pointBorderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `R$ ${context.parsed.y.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return 'R$ ' + value.toLocaleString('pt-BR');
                        }
                    }
                }
            }
        }
    });

    ctx.chart = chart;
    return chart;
};

/**
 * Renderiza gráfico de barras horizontais - Rendimentos Mensais
 * @param {string} canvasId - ID do elemento canvas
 * @param {Array} data - Array de objetos { month, value }
 */
window.renderMonthlyYields = function(canvasId, data) {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return null;

    if (ctx.chart) {
        ctx.chart.destroy();
    }

    const labels = data.map(d => d.month);
    const values = data.map(d => d.value);

    const chart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Rendimentos (R$)',
                data: values,
                backgroundColor: 'rgba(75, 192, 192, 0.8)',
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `R$ ${context.parsed.y.toLocaleString('pt-BR', { minimumFractionDigits: 2 })}`;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function(value) {
                            return 'R$ ' + value.toLocaleString('pt-BR');
                        }
                    }
                }
            }
        }
    });

    ctx.chart = chart;
    return chart;
};

/**
 * Destrói todos os gráficos na página
 */
window.destroyAllCharts = function() {
    const canvases = document.querySelectorAll('canvas');
    canvases.forEach(canvas => {
        if (canvas.chart) {
            canvas.chart.destroy();
            canvas.chart = null;
        }
    });
};
