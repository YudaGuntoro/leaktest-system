import type { Metadata } from "next";
import SettingPage from "@/production/SettingPage";

export const metadata: Metadata = { title: "Setting | PT. Yanmar Diesel Indonesia" };

export default function Page() {
  return <SettingPage />;
}
