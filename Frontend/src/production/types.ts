export type EngineModel = {
  id: number;
  engine_model: string;
  description?: string | null;
  note?: string | null;
  is_deleted?: boolean | null;
};

export type Operator = {
  id: number;
  operator_code: string;
  operator_name: string;
  department?: string | null;
  note?: string | null;
  is_deleted?: boolean | null;
  created_at: string;
  updated_at: string;
};

export type LeakTestResult = "OK" | "NG";

export type LeakTestWorkRecord = {
  id: number;
  engine_model_id: number;
  engine_model: string;
  engine_number: string;
  check_date: string;
  check_time: string;
  machine_name: string;
  operator_id?: number | null;
  operator_name?: string | null;
  parameter_pressure: number;
  pressure_input: number;
  cycle_time_leak_test_minutes: number;
  result: LeakTestResult;
  created_at: string;
  updated_at: string;
};

export type ReworkEngineRecord = {
  id: number;
  engine_model_id?: number | null;
  engine_model: string;
  engine_model_text?: string | null;
  engine_number: string;
  barcode_scan: string;
  rework_date: string;
  rework_time: string;
  operator_id?: number | null;
  operator_name?: string | null;
  parameter_pressure: number;
  pressure_input: number;
  result: LeakTestResult;
  note?: string | null;
  created_at: string;
  updated_at: string;
};

export type LeakTestMonthlySummary = {
  year: number;
  month: number;
  month_label: string;
  total_engine_inspect: number;
  ok: number;
  ng: number;
};
