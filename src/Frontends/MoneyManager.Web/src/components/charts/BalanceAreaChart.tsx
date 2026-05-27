'use client';

import { useEffect, useMemo, useRef } from 'react';
import { AreaSeries } from 'lightweight-charts';
import { useChart } from '@/hooks/use-chart';
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";

interface Props {
  data: { time: string; value: number }[];
  height?: number;
}

export function BalanceAreaChart({ data, height = 220 }: Props) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const containerRef = useRef<HTMLDivElement>(null);
  const chart = useChart({
    containerRef,
    options: useMemo(
      () => ({
        localization: {
          priceFormatter: formatMonetaryValue,
        },
      }),
      [formatMonetaryValue]
    ),
  });

  useEffect(() => {
    if (!chart) {
      return;
    }

    const areaSeries = chart.addSeries(AreaSeries, {
      lineColor: '#3b82f6',
      topColor: 'rgba(59,130,246,0.3)',
      bottomColor: 'rgba(59,130,246,0.02)',
      lineWidth: 2,
    });

    areaSeries.setData(data as Array<{ time: string; value: number }>);
    chart.timeScale().fitContent();

    return () => {
      try {
        chart.removeSeries(areaSeries);
      } catch {
        // Chart may already be disposed during route transitions.
      }
    };
  }, [chart, data]);

  return <div ref={containerRef} style={{ height, width: '100%' }} />;
}
