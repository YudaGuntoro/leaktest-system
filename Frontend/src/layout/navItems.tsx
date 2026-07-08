import React from "react";
import {
  BoxCubeIcon,
  BoltIcon,
  DocsIcon,
  GridIcon,
} from "../icons/index";

export type NavSubItem = {
  name: string;
  path: string;
  pro?: boolean;
  new?: boolean;
};

export type NavItem = {
  name: string;
  icon: React.ReactNode;
  path?: string;
  subItems?: NavSubItem[];
};

export const navItems: NavItem[] = [
  {
    icon: <GridIcon />,
    name: "Dashboard",
    path: "/",
  },
  {
    icon: <BoltIcon />,
    name: "Production Control",
    path: "/production-control",
  },
  {
    icon: <BoxCubeIcon />,
    name: "Master Data",
    subItems: [
      { name: "Cutting Lists", path: "/cutting-lists" },
      { name: "Shift Master", path: "/shift-master" },
      { name: "Operator Cards", path: "/pic-cards" },
    ],
  },
  {
    icon: <DocsIcon />,
    name: "Production Activity",
    path: "/production-history",
  },
];
