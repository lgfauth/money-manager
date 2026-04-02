'use client';

import { useEffect, useState } from 'react';
import {
  ColorType,
  createChart,
  type DeepPartial,
  type IChartApi,
  type TimeChartOptions,
} from 'lightweight-charts';

const DEFAULT_OPTIONS: DeepPartial<TimeChartOptions> = {
  layout: {
    background: { type: ColorType.Solid, color: '#1a1d27' },
    textColor: '#8892a4',
    attributionLogo: false,
  },
  grid: {
    vertLines: { color: '#2a2f45' },
    horzLines: { color: '#2a2f45' },
  },
  rightPriceScale: { borderColor: '#2a2f45' },
  timeScale: { borderColor: '#2a2f45', timeVisible: false },
};

interface UseChartParams {
  containerRef: React.RefObject<HTMLDivElement | null>;
  options?: DeepPartial<TimeChartOptions>;
}

export function useChart({ containerRef, options }: UseChartParams): IChartApi | null {
  const [chart, setChart] = useState<IChartApi | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) {
      return;
    }

    const mergedOptions: DeepPartial<TimeChartOptions> = {
      ...DEFAULT_OPTIONS,
      ...(options ?? {}),
      width: container.clientWidth,
      height: container.clientHeight,
    };

    const chartApi = createChart(container, mergedOptions as DeepPartial<TimeChartOptions>);
    setChart(chartApi);

    const resizeObserver = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (!entry) {
        return;
      }

      const width = Math.floor(entry.contentRect.width);
      const height = Math.floor(entry.contentRect.height);
      chartApi.applyOptions({
        width,
        ...(height > 0 ? { height } : {}),
      });
      chartApi.timeScale().fitContent();
    });

    resizeObserver.observe(container);

    return () => {
      resizeObserver.disconnect();
      chartApi.remove();
      setChart(null);
    };
  }, [containerRef, options]);

  return chart;
}
