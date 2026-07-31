export type EngineModel = {
  id: number;
  engine_model: string;
  description?: string | null;
  note?: string | null;
  is_deleted?: boolean | null;
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
  parameter_pressure: number;
  pressure_input: number;
  cycle_time_leak_test_minutes: number;
  result: LeakTestResult;
  created_at: string;
  updated_at: string;
};
