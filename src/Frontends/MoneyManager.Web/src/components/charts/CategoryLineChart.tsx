'use client';

import { useEffect, useMemo, useRef } from 'react';
import { LineSeries } from 'lightweight-charts';
import { useChart } from '@/hooks/use-chart';

interface Props {
  series: {
    name: string;
    color: string;
    data: { time: string; value: number }[];
  }[];
  height?: number;
}

const formatBRL = (value: number) =>
  `R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;

export function CategoryLineChart({ series, height = 280 }: Props) {
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

    const createdSeries = series.map((entry) => {
      const lineSeries = chart.addSeries(LineSeries, {
        color: entry.color,
        lineWidth: 2,
        title: entry.name,
      });

      lineSeries.setData(entry.data as Array<{ time: string; value: number }>);
      return lineSeries;
    });

    chart.timeScale().fitContent();

    return () => {
      createdSeries.forEach((lineSeries) => {
        try {
          chart.removeSeries(lineSeries);
        } catch {
          // Chart may already be disposed during route transitions.
        }
      });
    };
  }, [chart, series]);

  return <div ref={containerRef} style={{ height, width: '100%' }} />;
}
