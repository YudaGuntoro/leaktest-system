import type { Metadata } from "next";
import ProductionControlPage from "@/production/ProductionControlPage";

export const metadata: Metadata = { title: "Production Control | PT YKK AP Indonesia" };

export default function Page() {
  return <ProductionControlPage />;
}
