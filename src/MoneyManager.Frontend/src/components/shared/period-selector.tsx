"use client";

import { useMemo, useState } from "react";
import { format, subMonths, startOfMonth, endOfMonth } from "date-fns";
import { ptBR } from "date-fns/locale";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";

type Preset = "current" | "previous" | "3m" | "6m" | "year" | "custom";

interface PeriodSelectorProps {
  startDate: string;
  endDate: string;
  onChange: (start: string, end: string) => void;
  showPresets?: boolean;
}

export function PeriodSelector({
  startDate,
  endDate,
  onChange,
  showPresets = true,
}: PeriodSelectorProps) {
  const [preset, setPreset] = useState<Preset>("current");

  const currentMonth = useMemo(() => {
    const date = new Date(startDate + "T00:00:00");
    return format(date, "MMMM yyyy", { locale: ptBR });
  }, [startDate]);

  const applyPreset = (p: Preset) => {
    setPreset(p);
    const now = new Date();
    let start: Date;
    let end: Date;

    switch (p) {
      case "current":
        start = startOfMonth(now);
        end = endOfMonth(now);
        break;
      case "previous":
        start = startOfMonth(subMonths(now, 1));
        end = endOfMonth(subMonths(now, 1));
        break;
      case "3m":
        start = startOfMonth(subMonths(now, 2));
        end = endOfMonth(now);
        break;
      case "6m":
        start = startOfMonth(subMonths(now, 5));
        end = endOfMonth(now);
        break;
      case "year":
        start = new Date(now.getFullYear(), 0, 1);
        end = endOfMonth(now);
        break;
      default:
        return;
    }

    onChange(format(start, "yyyy-MM-dd"), format(end, "yyyy-MM-dd"));
  };

  const navigateMonth = (direction: -1 | 1) => {
    const current = new Date(startDate + "T00:00:00");
    const next =
      direction === 1
        ? new Date(current.getFullYear(), current.getMonth() + 1, 1)
        : new Date(current.getFullYear(), current.getMonth() - 1, 1);
    onChange(
      format(startOfMonth(next), "yyyy-MM-dd"),
      format(endOfMonth(next), "yyyy-MM-dd")
    );
    setPreset("custom");
  };

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      {/* Month navigation */}
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="icon"
          className="h-8 w-8"
          onClick={() => navigateMonth(-1)}
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <span className="min-w-[140px] text-center text-sm font-medium capitalize">
          {currentMonth}
        </span>
        <Button
          variant="outline"
          size="icon"
          className="h-8 w-8"
          onClick={() => navigateMonth(1)}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>

      {/* Preset tabs */}
      {showPresets && (
        <Tabs value={preset} onValueChange={(v) => applyPreset(v as Preset)}>
          <TabsList className="h-8">
            <TabsTrigger value="current" className="text-xs px-2 h-6">
              Atual
            </TabsTrigger>
            <TabsTrigger value="previous" className="text-xs px-2 h-6">
              Anterior
            </TabsTrigger>
            <TabsTrigger value="3m" className="text-xs px-2 h-6">
              3M
            </TabsTrigger>
            <TabsTrigger value="6m" className="text-xs px-2 h-6">
              6M
            </TabsTrigger>
            <TabsTrigger value="year" className="text-xs px-2 h-6">
              Ano
            </TabsTrigger>
          </TabsList>
        </Tabs>
      )}
    </div>
  );
}
