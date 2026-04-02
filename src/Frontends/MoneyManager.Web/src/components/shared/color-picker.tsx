"use client";

import { useState } from "react";
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";
import { COLOR_PRESETS } from "@/config/constants";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

interface ColorPickerProps {
  value: string;
  onChange: (color: string) => void;
}

export function ColorPicker({ value, onChange }: ColorPickerProps) {
  const [customColor, setCustomColor] = useState(value);

  return (
    <Popover>
      <PopoverTrigger>
        <div
          className="h-8 w-8 rounded-lg border cursor-pointer hover:ring-2 hover:ring-ring transition-all"
          style={{ backgroundColor: value }}
        />
      </PopoverTrigger>
      <PopoverContent className="w-64" align="start">
        <div className="space-y-3">
          <div className="grid grid-cols-8 gap-1.5">
            {COLOR_PRESETS.map((color) => (
              <button
                key={color}
                type="button"
                className={cn(
                  "h-7 w-7 rounded-md border transition-all hover:scale-110",
                  value === color && "ring-2 ring-ring ring-offset-2"
                )}
                style={{ backgroundColor: color }}
                onClick={() => onChange(color)}
              >
                {value === color && (
                  <Check className="h-3 w-3 mx-auto text-white drop-shadow" />
                )}
              </button>
            ))}
          </div>

          <div className="flex gap-2">
            <Input
              type="text"
              value={customColor}
              onChange={(e) => setCustomColor(e.target.value)}
              placeholder="#000000"
              className="h-8 text-xs font-mono"
              maxLength={7}
            />
            <Button
              type="button"
              size="sm"
              variant="outline"
              className="h-8 px-2"
              onClick={() => {
                if (/^#[0-9a-fA-F]{6}$/.test(customColor)) {
                  onChange(customColor);
                }
              }}
            >
              OK
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
