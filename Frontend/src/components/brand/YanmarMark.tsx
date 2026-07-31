type YanmarMarkProps = {
  className?: string;
};

export default function YanmarMark({ className = "" }: YanmarMarkProps) {
  return (
    <svg
      aria-label="Yanmar"
      className={className}
      role="img"
      viewBox="0 0 64 64"
      xmlns="http://www.w3.org/2000/svg"
    >
      <path d="M14 22.5 32 30.25 50 22.5v6.75L32 37 14 29.25z" fill="currentColor" />
      <path d="M14 32.25 32 40 50 32.25V39L32 46.75 14 39z" fill="currentColor" />
    </svg>
  );
}
