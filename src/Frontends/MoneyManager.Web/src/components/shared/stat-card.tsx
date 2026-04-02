import type { LucideIcon } from "lucide-react";
import { ArrowDown, ArrowUp } from "lucide-react";
import { cn } from "@/lib/utils";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface StatCardProps {
  title: string;
  value: string;
  icon: LucideIcon;
  trend?: {
    value: number;
    label: string;
  };
  variant?: "default" | "income" | "expense" | "warning";
}

const variantTopBar: Record<string, string> = {
  default: "before:bg-primary",
  income: "before:bg-income",
  expense: "before:bg-expense",
  warning: "before:bg-warning",
};

const variantIcon: Record<string, string> = {
  default: "bg-primary/10 text-primary",
  income: "bg-income/10 text-income",
  expense: "bg-expense/10 text-expense",
  warning: "bg-warning/10 text-warning",
};

export function StatCard({
  title,
  value,
  icon: Icon,
  trend,
  variant = "default",
}: StatCardProps) {
  return (
    <Card
      className={cn(
        "relative overflow-hidden rounded-xl shadow-sm hover:shadow-md transition-shadow before:absolute before:top-0 before:left-0 before:right-0 before:h-[3px]",
        variantTopBar[variant]
      )}
    >
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-xs font-medium text-muted-foreground">
          {title}
        </CardTitle>
        <div
          className={cn(
            "flex h-8 w-8 items-center justify-center rounded-full",
            variantIcon[variant]
          )}
        >
          <Icon className="h-4 w-4" />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-[26px] font-semibold font-heading">{value}</div>
        {trend && (
          <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
            {trend.value >= 0 ? (
              <ArrowUp className="h-3 w-3 text-income" />
            ) : (
              <ArrowDown className="h-3 w-3 text-expense" />
            )}
            <span
              className={cn(
                trend.value >= 0 ? "text-income" : "text-expense"
              )}
            >
              {Math.abs(trend.value).toFixed(1)}%
            </span>
            <span>{trend.label}</span>
          </p>
        )}
      </CardContent>
    </Card>
  );
}
