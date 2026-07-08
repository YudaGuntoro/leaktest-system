import type { Metadata } from "next";
import ProductionDashboard from "@/production/ProductionDashboard";

export const metadata: Metadata = {
  title: "Production Control Monitoring System | PT YKK AP Indonesia",
  description: "Production control and work order monitoring dashboard",
};

export default function ProductionControlHome() {
  return <ProductionDashboard />;
}
