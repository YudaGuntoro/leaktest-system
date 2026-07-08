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

export type User = {
  id: number;
  name: string;
  username: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
};

export type UserCreatePayload = {
  name: string;
  username: string;
  password: string;
  role: string;
  isActive: boolean;
};

export type UserUpdatePayload = {
  name: string;
  username: string;
  role: string;
  isActive: boolean;
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
