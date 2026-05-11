export interface ReceiptAnalysisResult {
  description: string;
  amount: number;
  date: string; // YYYY-MM-DD
  transactionType: "expense" | "income";
  categoryHint: string | null;
  paymentMethod: string | null;
  installments: number | null;
  notes: string | null;
  confidence: number;
}
