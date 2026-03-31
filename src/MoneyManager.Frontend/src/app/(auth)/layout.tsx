export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-gradient-to-br from-indigo-600 via-purple-600 to-indigo-800 dark:from-indigo-950 dark:via-purple-950 dark:to-indigo-900">
      {/* Animated background blobs */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute -left-40 -top-40 h-80 w-80 rounded-full bg-white/10 blur-3xl animate-pulse" />
        <div className="absolute -right-40 -bottom-40 h-80 w-80 rounded-full bg-white/10 blur-3xl animate-pulse delay-1000" />
      </div>

      <div className="relative z-10 w-full max-w-md px-4">{children}</div>
    </div>
  );
}
