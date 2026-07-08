import type { Metadata } from 'next';
import './globals.css';
import "flatpickr/dist/flatpickr.css";
import { SidebarProvider } from '@/context/SidebarContext';
import { ThemeProvider } from '@/context/ThemeContext';
import { ToastProvider } from '@/context/ToastContext';

export const metadata: Metadata = {
  title: {
    default: "Production Control Monitoring System",
    template: "%s | Production Control Monitoring System",
  },
  description: "Production Control Monitoring System for PT YKK AP Indonesia",
  icons: {
    apple: "/icon.png?v=ykk-ap",
    icon: "/icon.png?v=ykk-ap",
    shortcut: "/favicon.ico?v=ykk-ap",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body className="font-outfit dark:bg-gray-900">
        <ThemeProvider>
          <ToastProvider>
            <SidebarProvider>{children}</SidebarProvider>
          </ToastProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
