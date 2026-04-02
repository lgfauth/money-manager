"use client";

import {
  AreaChart as RechartsAreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  Legend,
} from "recharts";
import { ChartTooltip } from "./chart-tooltip";

interface AreaSeries {
  dataKey: string;
  name: string;
  color: string;
}

interface AreaChartProps {
  data: Record<string, unknown>[];
  series: AreaSeries[];
  xAxisKey?: string;
  height?: number;
  formatter?: (value: number) => string;
}

export function AreaChartComponent({
  data,
  series,
  xAxisKey = "name",
  height = 280,
  formatter,
}: AreaChartProps) {
  return (
    <ResponsiveContainer width="100%" height={height}>
      <RechartsAreaChart
        data={data}
        margin={{ top: 5, right: 20, bottom: 5, left: 0 }}
      >
        <defs>
          {series.map((s) => (
            <linearGradient
              key={s.dataKey}
              id={`gradient-${s.dataKey}`}
              x1="0"
              y1="0"
              x2="0"
              y2="1"
            >
              <stop offset="5%" stopColor={s.color} stopOpacity={0.2} />
              <stop offset="95%" stopColor={s.color} stopOpacity={0} />
            </linearGradient>
          ))}
        </defs>
        <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
        <XAxis
          dataKey={xAxisKey}
          tick={{ fontSize: 12 }}
          className="text-muted-foreground"
        />
        <YAxis hide />
        <Tooltip
          content={({ active, payload, label }) => (
            <ChartTooltip
              active={active}
              label={label as string}
              payload={payload?.map((p) => ({
                name: String(p.name ?? ""),
                value: p.value as number,
                color: p.color ?? "",
              }))}
              formatter={formatter}
            />
          )}
        />
        <Legend />
        {series.map((s) => (
          <Area
            key={s.dataKey}
            type="monotone"
            dataKey={s.dataKey}
            name={s.name}
            stroke={s.color}
            strokeWidth={2}
            fill={`url(#gradient-${s.dataKey})`}
          />
        ))}
      </RechartsAreaChart>
    </ResponsiveContainer>
  );
}
