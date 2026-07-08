type YkkBrandProps = {
  compact?: boolean;
  inverted?: boolean;
  size?: "default" | "large";
  showTitle?: boolean;
};

export default function YkkBrand({
  compact = false,
  inverted = false,
  size = "default",
  showTitle = true,
}: YkkBrandProps) {
  const isLarge = size === "large" && !compact;
  const markSize = compact ? "size-12" : isLarge ? "size-24" : "size-16";
  const gapSize = isLarge ? "gap-5" : "gap-3";
  const titleSize = compact ? "text-sm" : isLarge ? "text-xl" : "text-base";
  const subtitleSize = isLarge ? "text-sm" : "text-xs";

  return (
    <div className={`flex items-center ${gapSize}`}>
      <div
        className={`flex shrink-0 items-center justify-center rounded-2xl ${
          inverted ? "bg-white/15 ring-1 ring-white/25" : "bg-[#0799c9]"
        } ${markSize}`}
        aria-label="YKK AP"
      >
        <svg
          className="h-[78%] w-[78%] translate-y-[1px]"
          viewBox="0 0 64 64"
          role="img"
        >
          <text
            x="32"
            y="24"
            fill="white"
            fontFamily="Arial Black, Arial, sans-serif"
            fontSize="17"
            fontWeight="900"
            letterSpacing="-1.4"
            textAnchor="middle"
          >
            YKK
          </text>
          <text
            x="32"
            y="47"
            fill="white"
            fontFamily="Arial Black, Arial, sans-serif"
            fontSize="24"
            fontWeight="900"
            letterSpacing="-1"
            textAnchor="middle"
          >
            AP
          </text>
        </svg>
      </div>
      {showTitle ? (
        <div className="min-w-0">
          <p className={`truncate font-extrabold leading-tight ${inverted ? "text-white" : "text-[#0799c9]"} ${titleSize}`}>
            Production Control
          </p>
          <p className={`truncate font-medium ${inverted ? "text-white/75" : "text-slate-500"} ${subtitleSize}`}>PT YKK AP Indonesia</p>
        </div>
      ) : null}
    </div>
  );
}
