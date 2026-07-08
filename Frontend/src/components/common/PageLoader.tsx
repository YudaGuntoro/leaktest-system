type PageLoaderProps = {
  className?: string;
};

export default function PageLoader({ className = "" }: PageLoaderProps) {
  return (
    <div
      aria-label="Loading page"
      className={`fixed inset-0 z-[999999] flex min-h-screen items-center justify-center bg-white ${className}`}
      role="status"
    >
      <div className="size-14 animate-spin rounded-full border-[4px] border-brand-500 border-t-transparent" />
    </div>
  );
}
