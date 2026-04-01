import { NextResponse } from "next/server";

export async function POST(request: Request) {
  try {
    const formData = await request.formData();
    const username = String(formData.get("username") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    if (!username || !password) {
      return NextResponse.redirect(new URL("/login", request.url), 303);
    }

    const adminApiUrl = process.env.ADMIN_API_URL ?? process.env.NEXT_PUBLIC_ADMIN_API_URL;
    if (!adminApiUrl) {
      return NextResponse.redirect(new URL("/login", request.url), 303);
    }

    const response = await fetch(`${adminApiUrl}/api/auth/login`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ username, password }),
      cache: "no-store",
    });

    if (!response.ok) {
      return NextResponse.redirect(new URL("/login", request.url), 303);
    }

    const data = (await response.json()) as { accessToken?: string };
    if (!data.accessToken) {
      return NextResponse.redirect(new URL("/login", request.url), 303);
    }

    const redirect = NextResponse.redirect(new URL("/", request.url), 303);
    redirect.cookies.set("mm_admin_token", data.accessToken, {
      path: "/",
      maxAge: 60 * 60,
      sameSite: "lax",
      secure: true,
      httpOnly: false,
    });

    return redirect;
  } catch {
    return NextResponse.redirect(new URL("/login", request.url), 303);
  }
}
