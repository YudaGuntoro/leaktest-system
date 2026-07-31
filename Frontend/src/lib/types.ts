export type ApiResponse<T> = {
  success: boolean;
  statusCode: number;
  message: string;
  data: T;
};

export type UserRole = "ADMIN" | "SUPERVISOR" | "OPERATOR" | "VIEWER";

export type UserResponse = {
  id: number;
  username: string;
  full_name: string;
  email?: string | null;
  phone?: string | null;
  roles_id: number;
  role: UserRole;
  is_active: boolean;
  last_login_at?: string | null;
  created_at: string;
  updated_at: string;
};

export type LoginResponse = {
  access_token: string;
  token_type: string;
  expires_at: string;
  user: UserResponse;
};
