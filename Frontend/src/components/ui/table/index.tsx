import React, { ReactNode } from "react";

// Props for Table
interface TableProps extends React.TableHTMLAttributes<HTMLTableElement> {
  children: ReactNode; // Table content (thead, tbody, etc.)
}

// Props for TableHeader
interface TableHeaderProps
  extends React.HTMLAttributes<HTMLTableSectionElement> {
  children: ReactNode; // Header row(s)
}

// Props for TableBody
interface TableBodyProps extends React.HTMLAttributes<HTMLTableSectionElement> {
  children: ReactNode; // Body row(s)
}

// Props for TableRow
interface TableRowProps extends React.HTMLAttributes<HTMLTableRowElement> {
  children: ReactNode; // Cells (th or td)
}

// Props for TableCell
interface TableCellProps
  extends React.ThHTMLAttributes<HTMLTableCellElement>,
  React.TdHTMLAttributes<HTMLTableCellElement> {
  children: ReactNode; // Cell content
  isHeader?: boolean; // If true, renders as <th>, otherwise <td>
}

// Table Component
const Table: React.FC<TableProps> = ({ children, className, ...props }) => {
  return (
    <table className={`min-w-full ${className ?? ""}`} {...props}>
      {children}
    </table>
  );
};

// TableHeader Component
const TableHeader: React.FC<TableHeaderProps> = ({
  children,
  className,
  ...props
}) => {
  return (
    <thead
      className={`bg-brand-500 text-white dark:bg-brand-500 dark:text-white ${className ?? ""
        }`}
      {...props}
    >
      {children}
    </thead>
  );
};

// TableBody Component
const TableBody: React.FC<TableBodyProps> = ({
  children,
  className,
  ...props
}) => {
  return (
    <tbody className={className} {...props}>
      {children}
    </tbody>
  );
};

// TableRow Component
const TableRow: React.FC<TableRowProps> = ({
  children,
  className,
  ...props
}) => {
  return (
    <tr className={className} {...props}>
      {children}
    </tr>
  );
};

// TableCell Component
const TableCell: React.FC<TableCellProps> = ({
  children,
  isHeader = false,
  className,
  ...props
}) => {
  const CellTag = isHeader ? "th" : "td";

  return (
    <CellTag className={className} {...props}>
      {children}
    </CellTag>
  );
};

export { Table, TableHeader, TableBody, TableRow, TableCell };
