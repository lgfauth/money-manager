import { cn } from "@/lib/utils";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

interface MetricCardProps {
  title: string;
  subtitle: string;
  currentValue: string;
  targetValue: string;
  progressPercent: number;
  status: "on_track" | "at_risk" | "off_track";
  accentColor?: string;
}

const statusLabel: Record<string, string> = {
  on_track: "No caminho",
  at_risk: "Em risco",
  off_track: "Fora da meta",
};

const statusBadge: Record<string, string> = {
  on_track: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400",
  at_risk: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
  off_track: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
};

const statusBar: Record<string, string> = {
  on_track: "bg-emerald-500",
  at_risk: "bg-amber-500",
  off_track: "bg-red-500",
};

export function MetricCard({
  title,
  subtitle,
  currentValue,
  targetValue,
  progressPercent,
  status,
}: MetricCardProps) {
  return (
    <Card className="relative overflow-hidden rounded-xl shadow-sm">
      <CardHeader className="pb-2">
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardTitle className="text-sm font-semibold">{title}</CardTitle>
            <p className="text-xs text-muted-foreground mt-0.5">{subtitle}</p>
          </div>
          <span
            className={cn(
              "shrink-0 rounded-full px-2 py-0.5 text-xs font-medium",
              statusBadge[status]
            )}
          >
            {statusLabel[status]}
          </span>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        <div className="flex items-end justify-between text-sm">
          <span className="text-lg font-bold">{currentValue}</span>
          <span className="text-xs text-muted-foreground">meta: {targetValue}</span>
        </div>
        <div className="h-2 w-full overflow-hidden rounded-full bg-muted/30">
          <div
            className={cn("h-full rounded-full transition-all", statusBar[status])}
            style={{ width: `${Math.min(100, progressPercent)}%` }}
          />
        </div>
        <p className="text-right text-xs text-muted-foreground">
          {progressPercent.toFixed(1)}%
        </p>
      </CardContent>
    </Card>
  );
}
