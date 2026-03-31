"use client";

import {
  BarChart as RechartsBarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  Cell,
} from "recharts";
import { ChartTooltip } from "./chart-tooltip";

interface BarChartItem {
  name: string;
  value: number;
  color?: string;
}

interface BarChartProps {
  data: BarChartItem[];
  layout?: "horizontal" | "vertical";
  color?: string;
  height?: number;
  formatter?: (value: number) => string;
}

export function BarChartComponent({
  data,
  layout = "vertical",
  color = "var(--color-primary)",
  height = 280,
  formatter,
}: BarChartProps) {
  return (
    <ResponsiveContainer width="100%" height={height}>
      <RechartsBarChart
        data={data}
        layout={layout === "horizontal" ? "vertical" : "horizontal"}
        margin={{ top: 5, right: 20, bottom: 5, left: 0 }}
      >
        <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
        {layout === "horizontal" ? (
          <>
            <XAxis type="number" hide />
            <YAxis
              type="category"
              dataKey="name"
              width={100}
              tick={{ fontSize: 12 }}
              className="text-muted-foreground"
            />
          </>
        ) : (
          <>
            <XAxis
              dataKey="name"
              tick={{ fontSize: 12 }}
              className="text-muted-foreground"
            />
            <YAxis hide />
          </>
        )}
        <Tooltip
          content={({ active, payload, label }) => (
            <ChartTooltip
              active={active}
              label={label as string}
              payload={payload?.map((p) => ({
                name: String(p.name ?? ""),
                value: p.value as number,
                color: (p.payload as BarChartItem).color ?? color,
              }))}
              formatter={formatter}
            />
          )}
        />
        <Bar dataKey="value" radius={[4, 4, 0, 0]}>
          {data.map((entry, i) => (
            <Cell key={i} fill={entry.color ?? color} />
          ))}
        </Bar>
      </RechartsBarChart>
    </ResponsiveContainer>
  );
}
