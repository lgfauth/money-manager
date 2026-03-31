"use client";

import {
  RadialBarChart,
  RadialBar,
  ResponsiveContainer,
  PolarAngleAxis,
} from "recharts";
import { cn } from "@/lib/utils";

interface RadialChartProps {
  value: number; // 0-100
  label?: string;
  color?: string;
  trackColor?: string;
  textClassName?: string;
  size?: number;
  className?: string;
}

function getColor(value: number, customColor?: string) {
  if (customColor) return customColor;
  if (value <= 75) return "var(--color-income)";
  if (value <= 90) return "var(--color-warning)";
  return "var(--color-expense)";
}

export function RadialChart({
  value,
  label,
  color,
  trackColor,
  textClassName,
  size = 120,
  className,
}: RadialChartProps) {
  const clamped = Math.min(Math.max(value, 0), 100);
  const fillColor = getColor(clamped, color);
  const bgFill = trackColor ?? "var(--color-muted)";

  const data = [{ value: clamped, fill: fillColor }];

  return (
    <div
      className={cn("relative", className)}
      style={{ width: size, height: size }}
    >
      <ResponsiveContainer width="100%" height="100%">
        <RadialBarChart
          cx="50%"
          cy="50%"
          innerRadius="70%"
          outerRadius="90%"
          startAngle={90}
          endAngle={-270}
          data={data}
        >
          <PolarAngleAxis
            type="number"
            domain={[0, 100]}
            angleAxisId={0}
            tick={false}
          />
          <RadialBar
            background={{ fill: bgFill }}
            dataKey="value"
            cornerRadius={10}
          />
        </RadialBarChart>
      </ResponsiveContainer>

      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span className={cn("text-lg font-bold", textClassName)}>{clamped.toFixed(0)}%</span>
        {label && (
          <span className={cn("text-[10px]", textClassName ? textClassName : "text-muted-foreground")}>{label}</span>
        )}
      </div>
    </div>
  );
}
