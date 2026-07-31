import api, { ApiRequestOptions } from "@/utils/api";

export type ApiPagination = {
  page: number;
  limit: number;
  total: number;
  totalPage: number;
};

export type ApiListResponse<T> = {
  success: boolean;
  message: string;
  data: T[];
  pagination: ApiPagination;
};

export type UserRole = {
  id: number;
  role_name: string;
  description?: string | null;
  is_active: boolean;
  created_at: string;
  updated_at: string;
};

export type User = {
  id: number;
  username: string;
  full_name: string;
  email?: string | null;
  phone?: string | null;
  roles_id: number;
  role: string;
  is_active: boolean;
  last_login_at?: string | null;
  created_at: string;
  updated_at?: string;
};

export type UserCreatePayload = {
  username: string;
  full_name: string;
  email?: string | null;
  phone?: string | null;
  roles_id: number;
  password: string;
  is_active: boolean;
};

export type UserUpdatePayload = {
  username: string;
  full_name: string;
  email?: string | null;
  phone?: string | null;
  roles_id: number;
  password?: string;
  is_active: boolean;
};

export type UserQuery = {
  page?: number;
  limit?: number;
  search?: string;
  isActive?: boolean | null;
};

const normalizeQuery = (query: UserQuery) => ({
  page: query.page,
  limit: query.limit,
  search: query.search,
  isActive: query.isActive ?? undefined,
});

const UserService = {
  getUsers: async (query: UserQuery = {}, options?: ApiRequestOptions) => {
    const response = await api.get<ApiListResponse<User>>("/api/users", {
      ...options,
      params: normalizeQuery(query),
    });

    return response.data;
  },

  getUser: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.get<{
      success: boolean;
      message: string;
      data: User;
    }>(`/api/users/${id}`, options);

    return response.data;
  },

  getRoles: async (options?: ApiRequestOptions) => {
    const response = await api.get<{
      success: boolean;
      message: string;
      data: UserRole[];
    }>("/api/users/roles", options);

    return response.data;
  },

  createUser: async (data: UserCreatePayload, options?: ApiRequestOptions) => {
    const response = await api.post<{
      success: boolean;
      message: string;
      data: User;
    }>("/api/users", data, options);

    return response.data;
  },

  updateUser: async (
    id: number,
    data: UserUpdatePayload,
    options?: ApiRequestOptions
  ) => {
    const response = await api.put<{
      success: boolean;
      message: string;
      data: User;
    }>(`/api/users/${id}`, data, options);

    return response.data;
  },

  deleteUser: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.delete<{ success: boolean; message: string }>(
      `/api/users/${id}`,
      options
    );

    return response.data;
  },
};

export default UserService;
