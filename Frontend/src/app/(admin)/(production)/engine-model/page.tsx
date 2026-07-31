import type { Metadata } from "next";
import EngineModelPage from "@/production/EngineModelPage";

export const metadata: Metadata = { title: "Engine Model | PT. Yanmar Diesel Indonesia" };

export default function Page() {
  return <EngineModelPage />;
}
