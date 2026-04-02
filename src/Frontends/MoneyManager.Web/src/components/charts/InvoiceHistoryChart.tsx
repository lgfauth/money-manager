'use client';

import { useEffect, useMemo, useRef } from 'react';
import {
  AreaSeries,
  createSeriesMarkers,
  LineSeries,
  LineStyle,
} from 'lightweight-charts';
import { useChart } from '@/hooks/use-chart';

interface Props {
  data: {
    time: string;
    invoiceTotal: number;
    paidAmount: number;
  }[];
  height?: number;
}

const formatBRL = (value: number) =>
  `R$ ${value.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;

export function InvoiceHistoryChart({ data, height = 240 }: Props) {
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

    const totalSeries = chart.addSeries(AreaSeries, {
      lineColor: '#3b82f6',
      topColor: 'rgba(59,130,246,0.25)',
      bottomColor: 'rgba(59,130,246,0.03)',
      lineWidth: 2,
    });

    const paidSeries = chart.addSeries(LineSeries, {
      color: '#a855f7',
      lineWidth: 2,
      lineStyle: LineStyle.Dashed,
    });

    totalSeries.setData(
      data.map((point) => ({ time: point.time, value: point.invoiceTotal })) as Array<{
        time: string;
        value: number;
      }>
    );

    const paidData = data.map((point) => ({ time: point.time, value: point.paidAmount }));
    paidSeries.setData(paidData as Array<{ time: string; value: number }>);

    const lastPaid = [...data].reverse().find((point) => point.paidAmount > 0);
    if (lastPaid) {
      createSeriesMarkers(paidSeries, [
        {
          time: lastPaid.time as never,
          position: 'aboveBar',
          color: '#a855f7',
          shape: 'circle',
          text: 'Último pago',
        },
      ]);
    }

    chart.timeScale().fitContent();

    return () => {
      try {
        chart.removeSeries(totalSeries);
      } catch {
        // Chart may already be disposed during route transitions.
      }
      try {
        chart.removeSeries(paidSeries);
      } catch {
        // Chart may already be disposed during route transitions.
      }
    };
  }, [chart, data]);

  return <div ref={containerRef} style={{ height, width: '100%' }} />;
}
