'use client';

import { useEffect, useMemo, useRef } from 'react';
import { BaselineSeries } from 'lightweight-charts';
import { useChart } from '@/hooks/use-chart';

interface Props {
  data: { time: string; value: number }[];
  budget: number;
  height?: number;
}

const formatBRL = (value: number) =>
  `R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;

export function SpendingBaselineChart({ data, budget, height = 180 }: Props) {
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

    const baselineSeries = chart.addSeries(BaselineSeries, {
      baseValue: { type: 'price', price: budget },
      topLineColor: '#22c55e',
      bottomLineColor: '#ef4444',
      topFillColor1: 'rgba(34,197,94,0.3)',
      topFillColor2: 'rgba(34,197,94,0.02)',
      bottomFillColor1: 'rgba(239,68,68,0.02)',
      bottomFillColor2: 'rgba(239,68,68,0.3)',
      lineWidth: 2,
    });

    baselineSeries.setData(data as Array<{ time: string; value: number }>);
    chart.timeScale().fitContent();

    return () => {
      chart.removeSeries(baselineSeries);
    };
  }, [chart, data, budget]);

  return <div ref={containerRef} style={{ height, width: '100%' }} />;
}
