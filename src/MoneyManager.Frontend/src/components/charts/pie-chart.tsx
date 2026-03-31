"use client";

import {
  PieChart as RechartsPieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Tooltip,
} from "recharts";
import { ChartTooltip } from "./chart-tooltip";

interface PieChartItem {
  name: string;
  value: number;
  color: string;
}

interface DonutChartProps {
  data: PieChartItem[];
  centerLabel?: string;
  centerValue?: string;
  height?: number;
  formatter?: (value: number) => string;
}

export function DonutChart({
  data,
  centerLabel,
  centerValue,
  height = 280,
  formatter,
}: DonutChartProps) {
  const total = data.reduce((sum, d) => sum + d.value, 0);

  return (
    <div className="relative" style={{ height }}>
      <ResponsiveContainer width="100%" height="100%">
        <RechartsPieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius="60%"
            outerRadius="80%"
            dataKey="value"
            stroke="none"
          >
            {data.map((entry, i) => (
              <Cell key={i} fill={entry.color} />
            ))}
          </Pie>
          <Tooltip
            content={({ active, payload }) => (
              <ChartTooltip
                active={active}
                payload={payload?.map((p) => ({
                  name: String((p.payload as PieChartItem).name ?? ""),
                  value: p.value as number,
                  color: (p.payload as PieChartItem).color,
                }))}
                formatter={formatter}
              />
            )}
          />
        </RechartsPieChart>
      </ResponsiveContainer>

      {/* Center text */}
      {(centerLabel || centerValue) && (
        <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
          {centerValue && (
            <span className="text-lg font-bold">{centerValue}</span>
          )}
          {centerLabel && (
            <span className="text-xs text-muted-foreground">{centerLabel}</span>
          )}
        </div>
      )}
    </div>
  );
}
