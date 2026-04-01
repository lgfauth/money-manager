"use client";

import { CreditCard } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { RadialChart } from "@/components/charts/radial-chart";
import type { AccountResponseDto } from "@/types/account";
import Link from "next/link";

interface CreditCardSummaryProps {
  cards: AccountResponseDto[];
}

export function CreditCardSummary({ cards }: CreditCardSummaryProps) {
  if (cards.length === 0) return null;

  return (
    <Card className="col-span-full">
      <CardHeader>
        <CardTitle className="text-sm font-medium flex items-center gap-2">
          <CreditCard className="h-4 w-4" />
          Cartoes de Credito
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex gap-6 overflow-x-auto pb-2">
          {cards.map((card) => {
            const used = card.creditLimit
              ? (Math.abs(card.balance) / card.creditLimit) * 100
              : 0;
            return (
              <Link
                key={card.id}
                href={`/credit-cards/${card.id}`}
                className="flex items-center gap-4 min-w-[220px] rounded-[14px] px-[1.4rem] py-[1.2rem] transition-shadow hover:shadow-lg"
                style={{ background: "linear-gradient(135deg, #0D1117, #1A2D40)" }}
              >
                <RadialChart
                  value={Math.min(used, 100)}
                  size={52}
                  color={card.color || "#00C896"}
                  trackColor="rgba(255,255,255,0.08)"
                  textClassName="text-white font-heading text-[13px] font-semibold"
                />
                <div className="min-w-0">
                  <p className="text-[15px] font-semibold truncate text-white">{card.name}</p>
                  <p className="text-xs mt-0.5" style={{ color: "#8B9AB0" }}>
                    {new Intl.NumberFormat("pt-BR", {
                      style: "currency",
                      currency: card.currency,
                    }).format(Math.abs(card.balance))}{" "}
                    /{" "}
                    {new Intl.NumberFormat("pt-BR", {
                      style: "currency",
                      currency: card.currency,
                    }).format(card.creditLimit ?? 0)}
                  </p>
                </div>
              </Link>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
}
