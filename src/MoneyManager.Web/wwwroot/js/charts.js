window.chartInterop = {
    charts: {},

    createPieChart: function (canvasId, labels, data, colors) {
        console.log('Tentando criar gráfico de pizza:', canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas não encontrado:', canvasId);
            return;
        }

        if (this.charts[canvasId]) {
            console.log('Destruindo gráfico anterior:', canvasId);
            this.charts[canvasId].destroy();
        }

        console.log('Criando novo gráfico:', canvasId, 'Labels:', labels, 'Data:', data);
        this.charts[canvasId] = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
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
                            label: function (context) {
                                let label = context.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                const value = context.parsed;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((value / total) * 100).toFixed(1);
                                label += 'R$ ' + value.toFixed(2).replace('.', ',') + ' (' + percentage + '%)';
                                return label;
                            }
                        }
                    }
                }
            }
        });
    },

    createBarChart: function (canvasId, labels, data, colors) {
        console.log('Tentando criar gráfico de barras:', canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas não encontrado:', canvasId);
            return;
        }

        if (this.charts[canvasId]) {
            console.log('Destruindo gráfico anterior:', canvasId);
            this.charts[canvasId].destroy();
        }

        console.log('Criando novo gráfico:', canvasId, 'Labels:', labels, 'Data:', data);
        this.charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Saldo',
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 1,
                    borderColor: colors.map(c => c.replace('0.7', '1'))
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
                            label: function (context) {
                                let label = context.dataset.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                label += 'R$ ' + context.parsed.y.toFixed(2).replace('.', ',');
                                return label;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return 'R$ ' + value.toFixed(2).replace('.', ',');
                            }
                        }
                    }
                }
            }
        });
    },

    createLineChart: function (canvasId, labels, datasets) {
        console.log('Tentando criar gráfico de linhas:', canvasId);
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas não encontrado:', canvasId);
            return;
        }

        if (this.charts[canvasId]) {
            console.log('Destruindo gráfico anterior:', canvasId);
            this.charts[canvasId].destroy();
        }

        console.log('Criando novo gráfico:', canvasId, 'Labels:', labels, 'Datasets:', datasets);
        this.charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Receitas',
                        data: datasets.income,
                        borderColor: 'rgba(40, 167, 69, 1)',
                        backgroundColor: 'rgba(40, 167, 69, 0.1)',
                        tension: 0.4,
                        fill: true
                    },
                    {
                        label: 'Despesas',
                        data: datasets.expenses,
                        borderColor: 'rgba(220, 53, 69, 1)',
                        backgroundColor: 'rgba(220, 53, 69, 0.1)',
                        tension: 0.4,
                        fill: true
                    }
                ]
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
                            label: function (context) {
                                let label = context.dataset.label || '';
                                if (label) {
                                    label += ': ';
                                }
                                label += 'R$ ' + context.parsed.y.toFixed(2).replace('.', ',');
                                return label;
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                return 'R$ ' + value.toFixed(2).replace('.', ',');
                            }
                        }
                    }
                }
            }
        });
    },

    destroyChart: function (canvasId) {
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
            delete this.charts[canvasId];
        }
    },

    destroyAllCharts: function () {
        Object.keys(this.charts).forEach(id => {
            this.charts[id].destroy();
        });
        this.charts = {};
    }
};
