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

export type Parameter = {
  id: number;
  code: string;
  name: string;
  description: string;
  dataType: string;
  isActive: boolean;
  createdAt: string;
};

export type ParameterQuery = {
  page?: number;
  limit?: number;
  search?: string;
  isActive?: boolean | null;
};

const normalizeQuery = (query: ParameterQuery) => ({
  page: query.page,
  limit: query.limit,
  search: query.search,
  isActive: query.isActive ?? undefined,
});

const ParameterService = {
  getParameters: async (
    query: ParameterQuery = {},
    options?: ApiRequestOptions
  ) => {
    const response = await api.get<ApiListResponse<Parameter>>(
      "/api/parameters",
      {
        ...options,
        params: normalizeQuery(query),
      }
    );

    return response.data;
  },
  
  createParameter: async (data: Partial<Parameter>, options?: ApiRequestOptions) => {
    const response = await api.post<{ success: boolean; message: string; data: Parameter }>(
      "/api/parameters",
      data,
      options
    );
    return response.data;
  },

  deleteParameter: async (id: number, options?: ApiRequestOptions) => {
    const response = await api.delete<{ success: boolean; message: string }>(
      `/api/parameters/${id}`,
      options
    );
    return response.data;
  },

  updateParameter: async (id: number, data: Partial<Parameter>, options?: ApiRequestOptions) => {
    const response = await api.put<{ success: boolean; message: string; data: Parameter }>(
      `/api/parameters/${id}`,
      data,
      options
    );
    return response.data;
  },
};

export default ParameterService;
