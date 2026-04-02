"use client";

import { clearAdminToken } from "@/lib/admin-auth";

export function LogoutButton() {
  return (
    <button
      className="btn"
      type="button"
      onClick={() => {
        clearAdminToken();
        window.location.href = "/login";
      }}
    >
      Logout
    </button>
  );
}
