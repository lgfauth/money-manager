"use client";

import { clearAdminToken, getAdminToken } from "@/lib/admin-auth";

const rawApiUrl = process.env.NEXT_PUBLIC_ADMIN_API_URL?.trim();
const API_URL = rawApiUrl && !rawApiUrl.includes("__NEXT_PUBLIC_ADMIN_API_URL_PLACEHOLDER__")
  ? rawApiUrl
  : "/api/proxy";

async function request<T>(path: string): Promise<T> {
  const token = getAdminToken();
  const response = await fetch(`${API_URL}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    cache: "no-store",
  });

  if (response.status === 401) {
    clearAdminToken();
    window.location.href = "/login";
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `HTTP ${response.status}`);
  }

  return response.json() as Promise<T>;
}

async function postJson<TResponse, TBody>(path: string, body: TBody): Promise<TResponse> {
  const token = getAdminToken();
  const response = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(body),
  });

  if (response.status === 401) {
    clearAdminToken();
    window.location.href = "/login";
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `HTTP ${response.status}`);
  }

  return response.json() as Promise<TResponse>;
}

export type SystemStatus = {
  apiStatus: string;
  mongoStatus: string;
  workerStatus: string;
  timestampUtc: string;
  environment: string;
};

export type JobHistoryItem = {
  jobName: string;
  lastStatus: string;
  lastRunAtUtc?: string;
  lastDurationMs?: number;
  notes: string;
};

export type JobExecutionHistoryEntry = {
  correlationId: string;
  jobName: string;
  status: string;
  startedAtUtc: string;
  finishedAtUtc?: string;
  durationMs: number;
  workerName?: string;
  triggeredAtUtc?: string;
  errorMessage?: string;
};

export type JobCommandResponse = {
  commandId: string;
  jobName: string;
  commandType: string;
  status: string;
  requestedAtUtc: string;
  alreadyQueued: boolean;
};

export type UpdateJobScheduleRequest = {
  timeZoneId?: string;
  hour: number;
  minute: number;
  loopDelaySeconds: number;
  reason?: string;
};

export type JobScheduleResponse = {
  jobName: string;
  timeZoneId?: string;
  hour: number;
  minute: number;
  loopDelaySeconds: number;
  lastChangedAtUtc?: string;
  lastChangedBy?: string;
};

export type MetricsSummary = {
  windowStartedAtUtc: string;
  windowEndedAtUtc: string;
  http5xxCount: number;
  http4xxCount: number;
  apiP95Ms?: number;
  jobFailures: number;
};

export type AdminTargetUserRequest = {
  targetUserId: string;
  reason?: string;
};

export type AdminCommandResult<T> = {
  success: boolean;
  message: string;
  result: T;
};

export type AuditActionItem = {
  id: string;
  action: string;
  operatorUsername: string;
  targetUserId: string;
  isSuccess: boolean;
  errorMessage?: string;
  parametersJson?: string;
  resultJson?: string;
  createdAtUtc: string;
};

export type AdminMonthlyAuditReport = {
  year: number;
  month: number;
  totalActions: number;
  successfulActions: number;
  failedActions: number;
  successRate: number;
  actionCounts: Record<string, number>;
  roleCounts: Record<string, number>;
  uniqueOperators: number;
  uniqueTargetUsers: number;
  topActions: AuditActionItem[];
};

export type FinancialMaintenanceSummary = {
  recalculatedCount?: number;
  totalCards?: number;
  createdCount?: number;
  cardsProcessed?: number;
  invoicesCreated?: number;
  transactionsLinked?: number;
  errors?: string[];
  accountsProcessed?: number;
  invoicesRecalculated?: number;
};

export async function getSystemStatus(): Promise<SystemStatus> {
  return request<SystemStatus>("/api/admin/system-status");
}

export async function getJobsHistory(): Promise<JobHistoryItem[]> {
  return request<JobHistoryItem[]>("/api/admin/jobs/history");
}

export async function getJobHistory(jobName: string, limit = 10): Promise<JobExecutionHistoryEntry[]> {
  return request<JobExecutionHistoryEntry[]>(`/api/admin/jobs/${encodeURIComponent(jobName)}/history?limit=${limit}`);
}

export async function runJobNow(jobName: string, reason?: string): Promise<JobCommandResponse> {
  return postJson<JobCommandResponse, { reason?: string }>(
    `/api/admin/jobs/${encodeURIComponent(jobName)}/run-now`,
    { reason },
  );
}

export async function pauseJob(jobName: string, reason?: string): Promise<JobCommandResponse> {
  return postJson<JobCommandResponse, { reason?: string }>(
    `/api/admin/jobs/${encodeURIComponent(jobName)}/pause`,
    { reason },
  );
}

export async function resumeJob(jobName: string, reason?: string): Promise<JobCommandResponse> {
  return postJson<JobCommandResponse, { reason?: string }>(
    `/api/admin/jobs/${encodeURIComponent(jobName)}/resume`,
    { reason },
  );
}

export async function updateJobSchedule(jobName: string, input: UpdateJobScheduleRequest): Promise<JobScheduleResponse> {
  const token = getAdminToken();
  const response = await fetch(`${API_URL}/api/admin/jobs/${encodeURIComponent(jobName)}/schedule`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(input),
  });

  if (response.status === 401) {
    clearAdminToken();
    window.location.href = "/login";
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `HTTP ${response.status}`);
  }

  return response.json() as Promise<JobScheduleResponse>;
}

export async function getMetricsSummary(): Promise<MetricsSummary> {
  return request<MetricsSummary>("/api/admin/metrics/summary");
}

export async function reconcileCreditCards(
  input: AdminTargetUserRequest,
): Promise<AdminCommandResult<FinancialMaintenanceSummary>> {
  return postJson<AdminCommandResult<FinancialMaintenanceSummary>, AdminTargetUserRequest>(
    "/api/admin/reconcile-credit-cards",
    input,
  );
}

export async function recalculateInvoices(
  input: AdminTargetUserRequest,
): Promise<AdminCommandResult<FinancialMaintenanceSummary>> {
  return postJson<AdminCommandResult<FinancialMaintenanceSummary>, AdminTargetUserRequest>(
    "/api/admin/recalculate-invoices",
    input,
  );
}

export async function createMissingOpenInvoices(
  input: AdminTargetUserRequest,
): Promise<AdminCommandResult<FinancialMaintenanceSummary>> {
  return postJson<AdminCommandResult<FinancialMaintenanceSummary>, AdminTargetUserRequest>(
    "/api/admin/create-missing-open-invoices",
    input,
  );
}

export async function migrateCreditCardInvoices(
  input: AdminTargetUserRequest,
): Promise<AdminCommandResult<FinancialMaintenanceSummary>> {
  return postJson<AdminCommandResult<FinancialMaintenanceSummary>, AdminTargetUserRequest>(
    "/api/admin/migrate-credit-card-invoices",
    input,
  );
}

export async function getAuditActions(
  limit = 50,
  targetUserId?: string,
  action?: string,
): Promise<AuditActionItem[]> {
  const params = new URLSearchParams();
  params.set("limit", String(limit));

  if (targetUserId) {
    params.set("targetUserId", targetUserId);
  }

  if (action) {
    params.set("action", action);
  }

  const query = params.toString();
  return request<AuditActionItem[]>(`/api/admin/audit/actions?${query}`);
}

export async function getMonthlyAuditReport(year: number, month: number): Promise<AdminMonthlyAuditReport> {
  const params = new URLSearchParams();
  params.set("year", String(year));
  params.set("month", String(month));

  return request<AdminMonthlyAuditReport>(`/api/admin/audit/report/monthly?${params.toString()}`);
}

export async function login(username: string, password: string): Promise<{ accessToken: string; expiresAtUtc: string }> {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ username, password }),
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || "Invalid credentials");
  }

  return response.json() as Promise<{ accessToken: string; expiresAtUtc: string }>;
}
