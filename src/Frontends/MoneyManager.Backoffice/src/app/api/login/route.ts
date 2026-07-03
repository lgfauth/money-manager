import { NextResponse } from "next/server";

export async function POST(request: Request) {
  try {
    const formData = await request.formData();
    const username = String(formData.get("username") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    if (!username || !password) {
      return NextResponse.json({ ok: false, error: "Credenciais obrigatórias" }, { status: 400 });
    }

    const adminApiUrl = process.env.ADMIN_API_URL ?? process.env.NEXT_PUBLIC_ADMIN_API_URL;
    if (!adminApiUrl) {
      return NextResponse.json({ ok: false, error: "Servidor não configurado" }, { status: 500 });
    }

    const response = await fetch(`${adminApiUrl}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
      cache: "no-store",
    });

    if (!response.ok) {
      return NextResponse.json({ ok: false, error: "Credenciais inválidas" }, { status: 401 });
    }

    const data = (await response.json()) as { accessToken?: string };
    if (!data.accessToken) {
      return NextResponse.json({ ok: false, error: "Token não recebido" }, { status: 500 });
    }

    const res = NextResponse.json({ ok: true });
    res.cookies.set("mm_admin_token", data.accessToken, {
      path: "/",
      maxAge: 60 * 60,
      sameSite: "strict",
      secure: process.env.NODE_ENV === "production",
      httpOnly: true,
    });

    return res;
  } catch {
    return NextResponse.json({ ok: false, error: "Erro ao conectar com o servidor" }, { status: 500 });
  }
}
