export class ApiClientError extends Error {
  statusCode: number;
  messages: string[];
  data?: unknown;

  constructor({
    statusCode,
    message,
    messages,
    data,
  }: {
    statusCode: number;
    message: string;
    messages: string[];
    data?: unknown;
  }) {
    super(message);
    this.name = "ApiClientError";
    this.statusCode = statusCode;
    this.messages = messages;
    this.data = data;
  }
}

function normalizeMessage(message: string): string {
  return message.trim();
}

function collectMessages(data: unknown): string[] {
  if (typeof data === "string") {
    const normalizedMessage = normalizeMessage(data);
    return normalizedMessage ? [normalizedMessage] : [];
  }

  if (Array.isArray(data)) {
    return data.flatMap((entry) => collectMessages(entry));
  }

  if (!data || typeof data !== "object") {
    return [];
  }

  const objectData = data as Record<string, unknown>;
  const directMessages = [
    objectData.message,
    objectData.error,
    objectData.title,
    objectData.detail,
  ].flatMap((entry) => collectMessages(entry));

  const errorMessages = "errors" in objectData
    ? collectMessages(objectData.errors)
    : [];

  const nestedMessages = Object.entries(objectData)
    .filter(([key]) => !["message", "error", "title", "detail", "errors"].includes(key))
    .flatMap(([, value]) => collectMessages(value));

  return [...directMessages, ...errorMessages, ...nestedMessages];
}

export function createApiClientError(
  statusCode: number,
  data: unknown,
  fallbackMessage?: string
) {
  const messages = Array.from(new Set(collectMessages(data)));
  const message = messages[0] || fallbackMessage || `HTTP ${statusCode}`;

  return new ApiClientError({
    statusCode,
    message,
    messages: messages.length > 0 ? messages : [message],
    data,
  });
}

export function getApiErrorMessages(
  error: unknown,
  fallbackMessage?: string
): string[] {
  if (error instanceof ApiClientError) {
    return error.messages;
  }

  if (error instanceof Error) {
    const normalizedMessage = normalizeMessage(error.message);
    if (normalizedMessage) {
      return [normalizedMessage];
    }
  }

  if (fallbackMessage) {
    return [fallbackMessage];
  }

  return [];
}

export function getApiErrorMessage(
  error: unknown,
  fallbackMessage: string
): string {
  return getApiErrorMessages(error, fallbackMessage)[0] ?? fallbackMessage;
}