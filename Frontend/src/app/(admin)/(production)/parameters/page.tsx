import type { Metadata } from "next";
import ParameterPage from "@/production/ParameterPage";

export const metadata: Metadata = { title: "Parameter | PT. Yanmar Diesel Indonesia" };

export default function ParametersRoute() {
  return <ParameterPage />;
}
