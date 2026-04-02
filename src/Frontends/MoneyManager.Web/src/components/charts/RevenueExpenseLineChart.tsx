'use client';

import { useEffect, useRef } from 'react';
import {
  ColorType,
  createChart,
  CrosshairMode,
  type DeepPartial,
  LineSeries,
  type IChartApi,
  type TimeChartOptions,
} from 'lightweight-charts';

interface RevenueExpenseLineChartProps {
  data: {
    time: string;
    income: number;
    expense: number;
  }[];
  height?: number;
}

const formatCurrency = (value: number) =>
  `R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: 0 })}`;

export function RevenueExpenseLineChart({ data, height = 220 }: RevenueExpenseLineChartProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) {
      return;
    }

    const chartOptions: DeepPartial<TimeChartOptions> = {
      width: container.clientWidth,
      height,
      layout: {
        background: { type: ColorType.Solid, color: '#ffffff' },
        textColor: '#8892a4',
        attributionLogo: false,
      },
      grid: {
        vertLines: { color: '#f0f2f8' },
        horzLines: { color: '#f0f2f8' },
      },
      rightPriceScale: { borderColor: '#e8eaf0' },
      timeScale: { borderColor: '#e8eaf0', timeVisible: false },
      crosshair: { mode: CrosshairMode.Normal },
    };

    const chart: IChartApi = createChart(container, chartOptions as DeepPartial<TimeChartOptions>);

    const incomeSeries = chart.addSeries(LineSeries, {
      color: '#16a34a',
      lineWidth: 2,
      pointMarkersVisible: true,
      pointMarkersRadius: 4,
      priceFormat: {
        type: 'custom',
        formatter: formatCurrency,
      },
    });

    const expenseSeries = chart.addSeries(LineSeries, {
      color: '#dc2626',
      lineWidth: 2,
      pointMarkersVisible: true,
      pointMarkersRadius: 4,
      priceFormat: {
        type: 'custom',
        formatter: formatCurrency,
      },
    });

    incomeSeries.setData(data.map((item) => ({ time: item.time, value: item.income })));
    expenseSeries.setData(data.map((item) => ({ time: item.time, value: item.expense })));

    chart.timeScale().fitContent();

    const resizeObserver = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (!entry) {
        return;
      }

      chart.applyOptions({
        width: Math.floor(entry.contentRect.width),
        height,
      });
      chart.timeScale().fitContent();
    });

    resizeObserver.observe(container);

    return () => {
      resizeObserver.disconnect();
      chart.remove();
    };
  }, [data, height]);

  const lastIncome = data.at(-1)?.income ?? 0;
  const lastExpense = data.at(-1)?.expense ?? 0;

  return (
    <div>
      <div ref={containerRef} />

      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: '8px 20px',
          marginTop: '14px',
          paddingTop: '14px',
          borderTop: '1px solid #e8eaf0',
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <div style={{ width: 20, height: 3, background: '#16a34a', borderRadius: 2, flexShrink: 0 }} />
          <span style={{ fontSize: 12, color: '#8892a4' }}>Receitas</span>
          <span style={{ fontSize: 12, color: '#0f172a', fontWeight: 600 }}>{formatCurrency(lastIncome)}</span>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <div style={{ width: 20, height: 3, background: '#dc2626', borderRadius: 2, flexShrink: 0 }} />
          <span style={{ fontSize: 12, color: '#8892a4' }}>Despesas</span>
          <span style={{ fontSize: 12, color: '#0f172a', fontWeight: 600 }}>{formatCurrency(lastExpense)}</span>
        </div>
      </div>
    </div>
  );
}
