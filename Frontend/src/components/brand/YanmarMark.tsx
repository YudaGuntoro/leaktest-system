type YanmarMarkProps = {
  className?: string;
};

export default function YanmarMark({ className = "" }: YanmarMarkProps) {
  return (
    <img alt="Yanmar" className={className} src="/images/logo/yanmar-logo.png" />
  );
}
