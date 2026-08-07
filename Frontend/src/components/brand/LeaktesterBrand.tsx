import YanmarMark from "./YanmarMark";

type LeaktesterBrandProps = {
  compact?: boolean;
  inverted?: boolean;
  size?: "default" | "large";
  showTitle?: boolean;
};

export default function LeaktesterBrand({
  compact = false,
  inverted = false,
  size = "default",
  showTitle = true,
}: LeaktesterBrandProps) {
  const isLarge = size === "large" && !compact;
  const markSize = compact ? "h-10 w-16" : isLarge ? "h-24 w-36" : "h-16 w-24";
  const gapSize = isLarge ? "gap-5" : "gap-3";
  const titleSize = compact ? "text-sm" : isLarge ? "text-xl" : "text-base";
  const subtitleSize = isLarge ? "text-sm" : "text-xs";

  return (
    <div className={`flex items-center ${gapSize}`}>
      <div className={`flex shrink-0 items-center justify-center ${markSize}`}>
        <YanmarMark className="h-full w-full object-contain" />
      </div>
      {showTitle ? (
        <div className="min-w-0">
          <p className={`truncate font-extrabold leading-tight ${inverted ? "text-white" : "text-brand-600"} ${titleSize}`}>
            Leaktester Work Record
          </p>
          <p className={`truncate font-medium ${inverted ? "text-white/75" : "text-slate-500"} ${subtitleSize}`}>PT. Yanmar Diesel Indonesia</p>
        </div>
      ) : null}
    </div>
  );
}
