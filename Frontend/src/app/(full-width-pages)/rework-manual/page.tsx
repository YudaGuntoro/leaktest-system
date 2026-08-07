import FormManualPage from "@/production/FormManualPage";
import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Rework Manual | PT. Yanmar Diesel Indonesia",
};

export default function ReworkManualPublicRoute() {
  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top_left,rgba(230,0,40,0.08),transparent_32%),linear-gradient(180deg,#f8fafc_0%,#eef2f7_100%)] px-4 py-8 dark:bg-none dark:bg-slate-950 sm:px-6 lg:px-8">
      <FormManualPage publicAccess />
    </main>
  );
}
