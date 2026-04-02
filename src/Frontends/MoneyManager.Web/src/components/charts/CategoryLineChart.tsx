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

export function CategoryLineChart({ series, height = 420 }: Props) {
  const containerRef = useRef<HTMLDivElement>(null);
  const chart = useChart({
    containerRef,
    options: useMemo(
      () => ({
        timeScale: {
          borderColor: '#2a2f45',
          timeVisible: false,
          rightOffset: 8,
          lockVisibleTimeRangeOnResize: false,
        },
        rightPriceScale: {
          borderColor: '#2a2f45',
          scaleMargins: { top: 0.05, bottom: 0.05 },
        },
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

  return (
    <div>
      <div ref={containerRef} style={{ height, width: '100%' }} />

      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: '8px 20px',
          marginTop: '14px',
          paddingTop: '14px',
          borderTop: '1px solid #2a2f45',
        }}
      >
        {series.map((entry) => (
          <div
            key={entry.name}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 8,
            }}
          >
            <div
              style={{
                width: 20,
                height: 3,
                background: entry.color,
                borderRadius: 2,
                flexShrink: 0,
              }}
            />
            <span style={{ fontSize: 12, color: '#8892a4' }}>{entry.name}</span>
            <span style={{ fontSize: 12, color: '#e2e8f0', fontWeight: 600 }}>
              {`R$ ${(entry.data.at(-1)?.value ?? 0).toLocaleString('pt-BR', {
                minimumFractionDigits: 0,
              })}`}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
