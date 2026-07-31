import type { Metadata } from 'next';
import './globals.css';
import "flatpickr/dist/flatpickr.css";
import { SidebarProvider } from '@/context/SidebarContext';
import { ThemeProvider } from '@/context/ThemeContext';
import { ToastProvider } from '@/context/ToastContext';

export const metadata: Metadata = {
  title: {
    default: "Leaktester Work Record",
    template: "%s | Leaktester Work Record",
  },
  description: "Leaktester Work Record for PT. Yanmar Diesel Indonesia",
  icons: {
    apple: "/yanmar-icon.svg?v=yanmar",
    icon: "/yanmar-icon.svg?v=yanmar",
    shortcut: "/yanmar-icon.svg?v=yanmar",
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
