'use client';

import { useEffect, useMemo, useRef } from 'react';
import { HistogramSeries } from 'lightweight-charts';
import { useChart } from '@/hooks/use-chart';

interface Props {
  data: {
    time: string;
    income: number;
    expense: number;
  }[];
  height?: number;
}

const formatBRL = (value: number) =>
  `R$ ${Math.abs(value).toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;

export function MonthlyHistogramChart({ data, height = 180 }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chart = useChart({
    containerRef,
    options: useMemo(
      () => ({
        localization: {
          priceFormatter: formatBRL,
        },
      }),
      []
    ),
  });

  useEffect(() => {
    if (!chart) {
      return;
    }

    const incomeSeries = chart.addSeries(HistogramSeries, {
      color: '#22c55e',
    });

    const expenseSeries = chart.addSeries(HistogramSeries, {
      color: '#ef4444',
    });

    incomeSeries.setData(
      data.map((point) => ({
        time: point.time,
        value: point.income,
      })) as Array<{ time: string; value: number }>
    );

    expenseSeries.setData(
      data.map((point) => ({
        time: point.time,
        value: -Math.abs(point.expense),
      })) as Array<{ time: string; value: number }>
    );

    chart.timeScale().fitContent();

    return () => {
      chart.removeSeries(incomeSeries);
      chart.removeSeries(expenseSeries);
    };
  }, [chart, data]);

  return <div ref={containerRef} style={{ height, width: '100%' }} />;
}
