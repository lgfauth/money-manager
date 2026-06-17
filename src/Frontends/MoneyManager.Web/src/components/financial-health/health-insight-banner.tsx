import { Lightbulb } from "lucide-react";
import type { HealthScore } from "@/types/financial-health";

interface HealthInsightBannerProps {
  score: HealthScore;
}

function getWorstMetric(score: HealthScore) {
  const metrics = [
    { label: "Aporte mensal", pct: score.investmentMetric.progressPercent },
    { label: "Reserva de emergência", pct: score.reserveMetric.progressPercent },
    { label: "Meta FIRE", pct: score.fireMetric.progressPercent },
    { label: "Controle de gastos", pct: score.expenseMetric.progressPercent },
  ];
  return metrics.reduce((prev, curr) => (curr.pct < prev.pct ? curr : prev));
}

function getInsightMessage(metricLabel: string): string {
  switch (metricLabel) {
    case "Aporte mensal":
      return "Seu aporte mensal está abaixo da meta. Tente reservar uma parte da renda logo no início do mês para garantir a consistência nos investimentos.";
    case "Reserva de emergência":
      return "Sua reserva de emergência precisa de atenção. Uma reserva sólida é a base de uma vida financeira tranquila — priorize reforçá-la antes de novos investimentos.";
    case "Meta FIRE":
      return "Sua meta de independência financeira ainda está longe. Aumentar o aporte mensal e reduzir despesas são os caminhos mais rápidos para acelerar o prazo.";
    case "Controle de gastos":
      return "Suas despesas estão acima da meta. Revise os gastos variáveis e identifique onde é possível cortar sem comprometer a qualidade de vida.";
    default:
      return "Continue acompanhando suas métricas para manter a saúde financeira em dia.";
  }
}

export function HealthInsightBanner({ score }: HealthInsightBannerProps) {
  const worst = getWorstMetric(score);
  const message = getInsightMessage(worst.label);

  return (
    <div className="flex gap-3 rounded-xl bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800 p-4">
      <div className="mt-0.5 shrink-0">
        <Lightbulb className="h-5 w-5 text-blue-500" />
      </div>
      <div>
        <p className="text-sm font-medium text-blue-900 dark:text-blue-200">
          Insight — {worst.label}
        </p>
        <p className="mt-1 text-sm text-blue-700 dark:text-blue-300">{message}</p>
      </div>
    </div>
  );
}
