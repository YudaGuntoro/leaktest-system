import type { Metadata } from "next";
import ProductionDashboard from "@/production/ProductionDashboard";

export const metadata: Metadata = {
  title: "Leaktester Work Record | PT. Yanmar Diesel Indonesia",
  description: "Leaktester work record and inspection monitoring dashboard",
};

export default function LeaktesterWorkRecordHome() {
  return <ProductionDashboard />;
}
