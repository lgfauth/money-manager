import { NextResponse } from "next/server";

export async function GET(request: Request) {
  const response = NextResponse.redirect(new URL("/login", request.url), 303);

  // Remove o cookie httpOnly server-side (único jeito de excluir cookies httpOnly)
  response.cookies.set("mm_admin_token", "", {
    path: "/",
    maxAge: 0,
    httpOnly: true,
    secure: true,
    sameSite: "strict",
  });

  return response;
}
