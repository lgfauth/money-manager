"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import { useAuthStore } from "@/stores/auth-store";
import { useAcceptTerms, useProfile, useRefreshProfile } from "@/hooks/use-profile";
import { Sidebar } from "@/components/layout/sidebar";
import { Header } from "@/components/layout/header";
import { MobileNav } from "@/components/layout/mobile-nav";
import { TermsConsentModal } from "@/components/lgpd/terms-consent-modal";
import {
  LEGAL_PRIVACY_POLICY_URL,
  LEGAL_TERMS_OF_USE_URL,
  LEGAL_TERMS_VERSION,
} from "@/config/legal";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated, token, isHydrated } = useAuthStore();
  const profile = useProfile({ enabled: isHydrated && (isAuthenticated || !!token) });
  const refreshUser = useRefreshProfile();
  const acceptTerms = useAcceptTerms();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (isHydrated && !isAuthenticated && !token) {
      router.replace(`/login?returnUrl=${encodeURIComponent(pathname)}`);
    }
  }, [isHydrated, isAuthenticated, token, router, pathname]);

  const requiresTermsAcceptance =
    !!profile.data && !profile.data.termsAccepted;

  const handleAcceptTerms = async () => {
    try {
      await acceptTerms.mutateAsync({ termsVersion: LEGAL_TERMS_VERSION });
      refreshUser();
      await profile.refetch();
    } catch {
      // Feedback is handled in the mutation hook.
    }
  };

  if (!isHydrated || (!isAuthenticated && !token) || profile.isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="flex h-screen">
      <Sidebar />
      <div className="flex flex-1 flex-col overflow-hidden">
        <Header />
        <main className="flex-1 overflow-auto p-4 pb-36 md:p-6 md:pb-6">
          {children}
        </main>
      </div>
      <MobileNav />
      <TermsConsentModal
        open={requiresTermsAcceptance}
        isSubmitting={acceptTerms.isPending}
        termsVersion={LEGAL_TERMS_VERSION}
        termsOfUseUrl={LEGAL_TERMS_OF_USE_URL}
        privacyPolicyUrl={LEGAL_PRIVACY_POLICY_URL}
        onAccept={handleAcceptTerms}
      />
    </div>
  );
}
