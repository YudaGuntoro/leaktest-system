export type ProductionWorkOrderStatus =
  | "WAITING"
  | "READY"
  | "IN_PROGRESS"
  | "HOLD"
  | "COMPLETED"
  | "CANCELLED";

export type ProductionWorkOrder = {
  id: number;
  wo_number: string;
  cutting_list_id: number;
  cutting_list_no: string;
  product_code: string;
  product_name: string;
  pic_card_id?: number | null;
  pic_name?: string | null;
  employee_no?: string | null;
  operator_shift?: string | null;
  operator_department?: string | null;
  operators: ProductionOperator[];
  line_code: string;
  actual_qty: number;
  reject_qty: number;
  status: ProductionWorkOrderStatus;
  plan_date: string;
  started_at?: string | null;
  completed_at?: string | null;
  updated_at: string;
};

export type ProductionOperator = {
  id: number;
  pic_card_id: number;
  card_uid: string;
  employee_no: string;
  full_name: string;
  department: string;
  shift: string;
  scanned_at: string;
};

export type ProductionDashboardSummary = {
  total_work_orders: number;
  waiting_work_orders: number;
  running_work_orders: number;
  completed_work_orders: number;
  actual_qty: number;
  reject_qty: number;
  work_orders: ProductionWorkOrder[];
};

export type CuttingList = {
  id: number;
  cutting_list_no: string;
  product_code: string;
  product_name: string;
  line_code: string;
  unit: string;
  plan_date: string;
  status: "OPEN" | "RELEASED" | "IN_PROGRESS" | "COMPLETED" | "CANCELLED";
  created_at: string;
};

export type PicCard = {
  id: number;
  card_uid: string;
  employee_no: string;
  full_name: string;
  department: string;
  shift: string;
  is_active: boolean;
  last_scanned_at?: string | null;
  created_at: string;
};

export type ShiftMaster = {
  id: number;
  shift_code: string;
  shift_name: string;
  sort_order: number;
  is_active: boolean;
  created_at: string;
};

export type ProductionActivityLog = {
  id: number;
  production_work_order_id: number;
  wo_number?: string | null;
  pic_name?: string | null;
  activity_type: string;
  remarks?: string | null;
  created_at: string;
};
