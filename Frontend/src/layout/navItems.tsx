import React from "react";
import {
  BoxCubeIcon,
  BoltIcon,
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
    name: "Leaktester Work Record",
    path: "/work-record",
  },
  {
    icon: <BoxCubeIcon />,
    name: "Master Data",
    subItems: [
      { name: "Engine Model", path: "/engine-model" },
      { name: "User", path: "/users" },
    ],
  },
];
