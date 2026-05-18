import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

// Rotas públicas que não requerem autenticação
const PUBLIC_PATHS = [
  "/login",
  "/register",
  "/forgot-password",
  "/reset-password",
  "/account-deleted",
];

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Ignorar rotas estáticas e de API do Next.js
  if (
    pathname.startsWith("/_next") ||
    pathname.startsWith("/api/") ||
    pathname.startsWith("/favicon") ||
    pathname.includes(".")
  ) {
    return NextResponse.next();
  }

  const isPublicPath = PUBLIC_PATHS.some((p) => pathname.startsWith(p));
  const hasToken = !!request.cookies.get("mm_access_token")?.value;

  // Redireciona para login se não autenticado em rota protegida
  if (!isPublicPath && !hasToken) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("returnUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // Redireciona para dashboard se já autenticado tentando acessar rota pública
  if (isPublicPath && hasToken && pathname !== "/account-deleted") {
    return NextResponse.redirect(new URL("/", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
