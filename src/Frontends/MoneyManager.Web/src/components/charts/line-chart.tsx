"use client";

import {
  LineChart as RechartsLineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  Legend,
} from "recharts";
import { ChartTooltip } from "./chart-tooltip";

interface LineSeries {
  dataKey: string;
  name: string;
  color: string;
}

interface LineChartProps {
  data: Record<string, unknown>[];
  series: LineSeries[];
  xAxisKey?: string;
  height?: number;
  formatter?: (value: number) => string;
}

export function LineChartComponent({
  data,
  series,
  xAxisKey = "name",
  height = 280,
  formatter,
}: LineChartProps) {
  return (
    <ResponsiveContainer width="100%" height={height}>
      <RechartsLineChart
        data={data}
        margin={{ top: 5, right: 20, bottom: 5, left: 0 }}
      >
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
          <Line
            key={s.dataKey}
            type="monotone"
            dataKey={s.dataKey}
            name={s.name}
            stroke={s.color}
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 4 }}
          />
        ))}
      </RechartsLineChart>
    </ResponsiveContainer>
  );
}
