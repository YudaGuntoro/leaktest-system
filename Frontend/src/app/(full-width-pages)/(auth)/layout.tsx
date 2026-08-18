import YanmarMark from "@/components/brand/YanmarMark";
import ThemeTogglerTwo from "@/components/common/ThemeTogglerTwo";

import React from "react";

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="relative z-1 min-h-screen bg-white dark:bg-gray-950">
      <div className="relative flex min-h-screen w-full flex-col overflow-hidden bg-white dark:bg-gray-950 lg:flex-row">
        {children}
        <div className="auth-hero-surface relative hidden min-h-screen w-full items-center overflow-hidden bg-[#080b13] text-white lg:grid lg:w-1/2">
          <div className="absolute inset-0 z-0 bg-[linear-gradient(135deg,#070b14_0%,#121a2c_46%,#243342_70%,#9f101c_100%)]" />
          <div className="auth-hero-grid absolute inset-0" />
          <div className="absolute inset-x-0 bottom-0 z-[2] h-[44%] bg-[linear-gradient(0deg,rgba(230,0,40,0.42),rgba(230,0,40,0))]" />
          <div className="absolute -right-28 bottom-0 z-[2] h-[72%] w-[62%] -skew-x-12 border-l border-white/10 bg-[#e60028]/14" />
          <div className="absolute left-[10%] top-[9%] z-[4] h-px w-44 bg-[linear-gradient(90deg,transparent,rgba(255,255,255,0.34),transparent)]" />
          <div className="absolute bottom-[18%] left-[12%] z-[4] h-px w-96 bg-[linear-gradient(90deg,rgba(230,0,40,0.58),transparent)]" />
          <svg
            aria-hidden="true"
            className="absolute -right-10 bottom-10 z-[3] h-72 w-80 text-white/10"
            fill="none"
            viewBox="0 0 320 256"
          >
            <path d="M42 192H278M72 160H248M96 128H224" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
            <path d="M82 64H238L266 92V190H54V92L82 64Z" stroke="currentColor" strokeWidth="3" />
            <path d="M110 64V190M210 64V190M54 118H266" stroke="currentColor" strokeWidth="2" />
            <circle cx="110" cy="118" r="12" stroke="currentColor" strokeWidth="3" />
            <circle cx="210" cy="118" r="12" stroke="currentColor" strokeWidth="3" />
            <path d="M138 150H182M160 150V174" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
          </svg>

          <div className="relative z-10 flex items-center justify-center px-12">
            <div className="w-full max-w-[500px]">
              <YanmarMark className="mb-14 h-24 w-24 scale-x-125 text-[#e60028]" />

              <div>
                <h2 className="text-5xl font-extrabold leading-tight tracking-normal text-white">
                  Assembly
                  <br />
                  <span className="text-[#df1f26]">System</span>
                </h2>
                <p className="mt-8 max-w-[390px] text-xl leading-8 text-white">
                  Monitor production records, master data, parameters, and operational results in one dashboard.
                </p>
              </div>

              <ul className="mt-9 space-y-5 text-base text-white">
                <li className="flex items-center gap-3"><span className="size-2 rounded-full bg-[#df1f26]" />Production traceability</li>
                <li className="flex items-center gap-3"><span className="size-2 rounded-full bg-[#df1f26]" />Master data and parameter control</li>
                <li className="flex items-center gap-3"><span className="size-2 rounded-full bg-[#df1f26]" />OK/NG result monitoring</li>
              </ul>

              <div className="mt-12 h-px w-full max-w-sm bg-white/15" />
              <p className="mt-9 max-w-sm text-lg italic leading-7 text-white">&quot;Accurate records for every assembly process&quot;</p>
            </div>
          </div>
        </div>
      </div>
      <div className="fixed bottom-6 right-6 z-50">
        <ThemeTogglerTwo />
      </div>
    </div>
  );
}
