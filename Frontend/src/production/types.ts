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

export type LeakTestParameter = {
  id: number;
  channel_no: string;
  model_parameter: string;
  item_name: string;
  item_value: string;
  machine_names?: string | null;
  is_deleted?: boolean | null;
  created_at: string;
  updated_at: string;
};

export type LeakTestParameterImportResult = {
  imported: number;
  updated: number;
  skipped: number;
  channels: number;
};

export type LeakTestResult = "OK" | "NG";

export type LeakTestJudgement = {
  id: number;
  judgement_code: number;
  judgement_name: string;
  result: LeakTestResult | "";
  note?: string | null;
  is_deleted?: boolean | null;
  created_at: string;
  updated_at: string;
};

export type LeakTestWorkRecord = {
  id: number;
  engine_model_id: number;
  engine_model: string;
  engine_number: string;
  barcode_scan?: string | null;
  channel_no?: string | null;
  check_date: string;
  check_time: string;
  machine_name: string;
  operator_code?: string | null;
  operator_name?: string | null;
  parameter_pressure: number;
  press_set_up?: number | null;
  press_set_low?: number | null;
  pressure_input: number;
  cycle_time_leak_test_minutes: number;
  judgement_code?: number | null;
  judgement_name?: string | null;
  parameter_channel_no?: string | null;
  parameter_standard?: string | null;
  parameter_min?: string | null;
  parameter_max?: string | null;
  parameter_limit?: string | null;
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
  operator_name?: string | null;
  parameter_pressure: number;
  pressure_input: number;
  parameter_channel_no?: string | null;
  parameter_standard?: string | null;
  parameter_min?: string | null;
  parameter_max?: string | null;
  parameter_limit?: string | null;
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
