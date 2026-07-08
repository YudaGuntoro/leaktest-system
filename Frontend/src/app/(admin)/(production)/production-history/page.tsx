import type { Metadata } from "next";
import ProductionHistoryPage from "@/production/ProductionHistoryPage";
export const metadata: Metadata = { title: "Production Activity | PT YKK AP Indonesia" };
export default function Page() { return <ProductionHistoryPage />; }
