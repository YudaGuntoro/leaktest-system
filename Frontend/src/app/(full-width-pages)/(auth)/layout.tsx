import GridShape from "@/components/common/GridShape";
import ThemeTogglerTwo from "@/components/common/ThemeTogglerTwo";
import YkkBrand from "@/components/brand/YkkBrand";

import { ThemeProvider } from "@/context/ThemeContext";
import Link from "next/link";
import React from "react";

const productName = "Production Control Monitoring System";

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="relative z-1 min-h-screen bg-gray-50 dark:bg-gray-950">
      <ThemeProvider>
        <div className="relative flex min-h-screen w-full flex-col overflow-hidden bg-[linear-gradient(135deg,#ffffff_0%,#f4fcff_45%,#e9f8fd_100%)] dark:bg-[linear-gradient(135deg,#07151b_0%,#0a222c_55%,#08384b_100%)] lg:flex-row">
          <div className="pointer-events-none absolute inset-0 bg-[linear-gradient(rgba(7,153,201,0.06)_1px,transparent_1px),linear-gradient(90deg,rgba(7,153,201,0.05)_1px,transparent_1px)] bg-[size:56px_56px] dark:opacity-30" />
          {children}
          <div className="relative hidden min-h-screen w-full items-center overflow-hidden bg-[#0799c9] lg:grid lg:w-1/2">
            <div className="absolute inset-0 bg-[linear-gradient(145deg,#08a7d9_0%,#0799c9_50%,#087ea4_100%)]" />
            <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,0.09)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,0.07)_1px,transparent_1px)] bg-[size:64px_64px] opacity-40" />
            <div className="absolute -right-24 top-20 h-48 w-72 rotate-12 rounded-lg border border-white/15 bg-white/[0.06]" />
            <div className="absolute bottom-24 -left-16 h-40 w-60 -rotate-12 rounded-lg border border-white/15 bg-white/[0.05]" />

            <div className="relative z-1 flex items-center justify-center px-10">
              <GridShape />
              <div className="flex w-full max-w-xl flex-col items-center text-center">
                <Link href="/signin" className="mb-10 block">
                  <YkkBrand inverted size="large" />
                </Link>

                <div className="mb-8">
                  <p
                    aria-label={productName}
                    className="auth-moving-letters max-w-2xl text-center text-2xl font-semibold leading-snug text-white"
                  >
                    {Array.from(productName).map((letter, index) => (
                      <span
                        aria-hidden="true"
                        className="auth-moving-letter"
                        key={`${letter}-${index}`}
                        style={
                          {
                            "--letter-index": index,
                          } as React.CSSProperties
                        }
                      >
                        {letter === " " ? "\u00a0" : letter}
                      </span>
                    ))}
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div className="fixed bottom-6 right-6 z-50 hidden sm:block">
            <ThemeTogglerTwo />
          </div>
        </div>
      </ThemeProvider>
    </div>
  );
}
