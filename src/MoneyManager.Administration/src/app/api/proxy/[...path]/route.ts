import { NextResponse } from "next/server";

type RouteParams = {
  params: Promise<{ path: string[] }>;
};

function getAdminApiBaseUrl(): string {
  const baseUrl = process.env.ADMIN_API_URL ?? process.env.NEXT_PUBLIC_ADMIN_API_URL ?? "";
  return baseUrl.trim().replace(/\/$/, "");
}

async function forward(request: Request, params: RouteParams["params"]) {
  const adminApiBaseUrl = getAdminApiBaseUrl();
  if (!adminApiBaseUrl) {
    return NextResponse.json({ message: "ADMIN_API_URL is not configured" }, { status: 500 });
  }

  const { path } = await params;
  const incomingUrl = new URL(request.url);
  const targetUrl = `${adminApiBaseUrl}/${path.join("/")}${incomingUrl.search}`;

  const headers = new Headers();
  const authorization = request.headers.get("authorization");
  const contentType = request.headers.get("content-type");

  if (authorization) {
    headers.set("authorization", authorization);
  }

  if (contentType) {
    headers.set("content-type", contentType);
  }

  const method = request.method.toUpperCase();
  const hasBody = !["GET", "HEAD"].includes(method);
  const body = hasBody ? await request.text() : undefined;

  const upstream = await fetch(targetUrl, {
    method,
    headers,
    body,
    cache: "no-store",
  });

  const responseHeaders = new Headers();
  const responseContentType = upstream.headers.get("content-type");
  if (responseContentType) {
    responseHeaders.set("content-type", responseContentType);
  }

  return new NextResponse(upstream.body, {
    status: upstream.status,
    headers: responseHeaders,
  });
}

export async function GET(request: Request, context: RouteParams) {
  return forward(request, context.params);
}

export async function POST(request: Request, context: RouteParams) {
  return forward(request, context.params);
}

export async function PUT(request: Request, context: RouteParams) {
  return forward(request, context.params);
}

export async function PATCH(request: Request, context: RouteParams) {
  return forward(request, context.params);
}

export async function DELETE(request: Request, context: RouteParams) {
  return forward(request, context.params);
}
