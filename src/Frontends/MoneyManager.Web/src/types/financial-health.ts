export interface FinancialHealthSettings {
  id: string;
  userId: string;
  modeName: string;
  investPercent: number;
  reserveMonths: number;
  fireMultiplier: number;
  fixedExpensePercent: number;
  installmentPercent: number;
  createdAt: string;
  updatedAt: string;
}

export interface PatrimonyBucket {
  id: string;
  userId: string;
  type: "emergency_reserve" | "fire_investment";
  initialBalance: number;
  initialBalanceDate: string;
  trackedCategoryIds: string[];
  expectedAnnualRate: number;
  createdAt: string;
  updatedAt: string;
}

export interface PendingBucketStatus {
  bucketId: string;
  bucketType: string;
  estimatedBalance: number;
  trackedContributions: number;
  estimatedYield: number;
}

export interface SnapshotStatus {
  hasConfiguration: boolean;
  showBanner: boolean;
  referenceMonth: string | null;
  pendingBuckets: PendingBucketStatus[];
}

export interface MonthlySnapshot {
  id: string;
  userId: string;
  bucketId: string;
  referenceMonth: string;
  openingBalance: number;
  trackedContributions: number;
  estimatedYield: number;
  estimatedClosingBalance: number;
  confirmedClosingBalance: number | null;
  trackedCategoryIds: string[];
  unconfirmed: boolean;
  dismissedByUser: boolean;
  confirmedAt: string | null;
  createdAt: string;
  updatedAt: string;
  effectiveBalance: number;
}

export interface MetricScore {
  currentValue: number;
  targetValue: number;
  progressPercent: number;
  status: "on_track" | "at_risk" | "off_track";
}

export interface FireProjection {
  fireTarget: number;
  reserveTarget: number;
  currentFireBalance: number;
  currentReserveBalance: number;
  estimatedMonthsToFire: number | null;
}

export interface HealthScore {
  hasData: boolean;
  overallScore: number;
  referenceMonth: string;
  totalIncome: number;
  totalExpenses: number;
  totalInvestments: number;
  investmentMetric: MetricScore;
  reserveMetric: MetricScore;
  fireMetric: MetricScore;
  expenseMetric: MetricScore;
  projection: FireProjection;
}

export interface UpsertFinancialHealthSettingsRequest {
  modeName: string;
  investPercent: number;
  reserveMonths: number;
  fireMultiplier: number;
  fixedExpensePercent: number;
  installmentPercent: number;
}

export interface UpsertPatrimonyBucketRequest {
  type: "emergency_reserve" | "fire_investment";
  initialBalance: number;
  initialBalanceDate: string;
  trackedCategoryIds: string[];
  expectedAnnualRate: number;
}

export interface BucketConfirmation {
  bucketId: string;
  confirmedBalance: number;
}

export interface ConfirmSnapshotRequest {
  buckets: BucketConfirmation[];
}
